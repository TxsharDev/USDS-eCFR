using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using EcfrAnalyzer.Data;
using EcfrAnalyzer.DTOs;
using EcfrAnalyzer.Models;

namespace EcfrAnalyzer.Services;

public class SyncService
{
    private readonly EcfrClient _ecfr;
    private readonly AppDbContext _db;
    private readonly ILogger<SyncService> _log;

    public SyncService(EcfrClient ecfr, AppDbContext db, ILogger<SyncService> log)
    {
        _ecfr = ecfr;
        _db = db;
        _log = log;
    }

    /// Full sync pipeline: agencies -> titles -> versions -> content + RBI.
    /// Each phase is wrapped in try/catch so partial progress is saved if eCFR goes down mid-sync.
    public async Task<SyncLog> RunFullSyncAsync()
    {
        var syncLog = new SyncLog { StartedAt = DateTime.UtcNow };
        _db.SyncLogs.Add(syncLog);
        await _db.SaveChangesAsync();

        try
        {
            var agencyCount = await SyncAgenciesAsync();
            var titleCount = await SyncTitlesAsync();
            await SyncVersionsAsync();
            await SyncContentAsync();

            syncLog.AgenciesSynced = agencyCount;
            syncLog.Status = "completed";
            syncLog.CompletedAt = DateTime.UtcNow;
            _log.LogInformation("Full sync completed: {Agencies} agencies, {Titles} titles",
                agencyCount, titleCount);
        }
        catch (Exception ex)
        {
            syncLog.Status = "failed";
            syncLog.ErrorMessage = ex.Message;
            syncLog.CompletedAt = DateTime.UtcNow;
            _log.LogError(ex, "Full sync failed");
        }

        await _db.SaveChangesAsync();
        return syncLog;
    }

    public async Task<int> SyncAgenciesAsync()
    {
        var dtos = await _ecfr.FetchAgenciesAsync();
        // eCFR returns nested parent/child agencies so flatten it so each gets its own row
        var flat = FlattenAgencies(dtos);
        // Upsert: load existing into memory, update or insert each
        var existing = await _db.Agencies.ToDictionaryAsync(a => a.Slug);
        var count = 0;

        foreach (var dto in flat)
        {
            // Store CFR references as JSON. These are needed later to fetch the right XML chapters
            var cfrJson = JsonSerializer.Serialize(
                dto.CfrReferences?.Select(r => new { r.Title, r.Chapter }) ?? []);

            if (existing.TryGetValue(dto.Slug, out var agency))
            {
                agency.Name = dto.Name;
                agency.ShortName = dto.ShortName;
                agency.CfrTitlesJson = cfrJson;
            }
            else
            {
                _db.Agencies.Add(new Agency
                {
                    Slug = dto.Slug,
                    Name = dto.Name,
                    ShortName = dto.ShortName,
                    CfrTitlesJson = cfrJson
                });
            }
            count++;
        }

        await _db.SaveChangesAsync();
        _log.LogInformation("Synced {Count} agencies", count);
        return count;
    }

    /// Fetch all 50 CFR titles and upsert metadata (name, amendment dates, etc.)
    public async Task<int> SyncTitlesAsync()
    {
        var dtos = await _ecfr.FetchTitlesAsync();
        var existing = await _db.Titles.ToDictionaryAsync(t => t.Number);
        var count = 0;

        foreach (var dto in dtos)
        {
            if (existing.TryGetValue(dto.Number, out var title))
            {
                title.Name = dto.Name;
                title.LatestAmended = dto.LatestAmendedOn;
                title.UpToDateAsOf = dto.UpToDateAsOf;
                title.Reserved = dto.Reserved;
                title.LastFetched = DateTime.UtcNow;
            }
            else
            {
                _db.Titles.Add(new Title
                {
                    Number = dto.Number,
                    Name = dto.Name,
                    LatestAmended = dto.LatestAmendedOn,
                    UpToDateAsOf = dto.UpToDateAsOf,
                    Reserved = dto.Reserved,
                    LastFetched = DateTime.UtcNow
                });
            }
            count++;
        }

        await _db.SaveChangesAsync();
        _log.LogInformation("Synced {Count} titles", count);
        return count;
    }

    /// For each non-reserved title, fetch version history and map changes to agencies
    /// based on their CFR references. This builds the change timeline data.
    public async Task SyncVersionsAsync()
    {
        var titles = await _db.Titles.Where(t => !t.Reserved).ToListAsync();
        // Build a lookup: title number -> list of agency slugs that reference it
        var agencies = await _db.Agencies.ToListAsync();
        var titleToAgencies = new Dictionary<int, List<string>>();

        foreach (var agency in agencies)
        {
            var refs = ParseCfrReferences(agency.CfrTitlesJson);
            foreach (var r in refs)
            {
                if (!titleToAgencies.ContainsKey(r.Title))
                    titleToAgencies[r.Title] = [];
                titleToAgencies[r.Title].Add(agency.Slug);
            }
        }

        // Get existing change dates per agency to avoid duplicates
        var existingKeys = new HashSet<string>(
            await _db.Changes.Select(c => c.AgencySlug + "|" + c.ChangeDate + "|" + c.TitleNumber)
                .ToListAsync());

        var totalChanges = 0;

        foreach (var title in titles)
        {
            if (!titleToAgencies.ContainsKey(title.Number))
                continue;

            try
            {
                var versions = await _ecfr.FetchVersionsAsync(title.Number);
                var slugs = titleToAgencies[title.Number];

                foreach (var v in versions.Where(v => v.Date != null))
                {
                    foreach (var slug in slugs)
                    {
                        var key = slug + "|" + v.Date + "|" + title.Number;
                        if (existingKeys.Contains(key))
                            continue;

                        _db.Changes.Add(new Change
                        {
                            AgencySlug = slug,
                            TitleNumber = title.Number,
                            Part = v.Identifier,
                            SectionName = v.Name,
                            ChangeDate = v.Date!,
                            Substantive = v.Substantive,
                            Removed = v.Removed
                        });
                        existingKeys.Add(key);
                        totalChanges++;
                    }
                }

                // Batch save every title to avoid huge in-memory buildup
                if (totalChanges > 0)
                {
                    await _db.SaveChangesAsync();
                    _log.LogInformation("Title {Title}: added changes, running total {Total}",
                        title.Number, totalChanges);
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to sync versions for title {Title}", title.Number);
            }
        }

        _log.LogInformation("Version sync done: {Total} new changes", totalChanges);
    }

    /// For each agency with CFR references, fetch the XML content, parse word count,
    /// compute average sentence length, MD5 checksum, and the RBI score.
    public async Task SyncContentAsync()
    {
        var agencies = await _db.Agencies.ToListAsync();
        var titles = await _db.Titles.Where(t => !t.Reserved).ToDictionaryAsync(t => t.Number);
        var synced = 0;

        foreach (var agency in agencies)
        {
            var refs = ParseCfrReferences(agency.CfrTitlesJson);
            if (refs.Count == 0) continue;

            var totalWords = 0;
            var totalSentences = 0;
            var totalSentenceWords = 0;
            var allText = new StringBuilder();

            foreach (var cfr in refs)
            {
                if (!titles.TryGetValue(cfr.Title, out var title))
                    continue;

                // Use the title's up_to_date_as_of date for fetching content
                var date = title.UpToDateAsOf ?? title.LatestAmended;
                if (string.IsNullOrEmpty(date)) continue;

                try
                {
                    var xmlBytes = await _ecfr.FetchContentAsync(date, cfr.Title, cfr.Chapter);
                    if (xmlBytes == null || xmlBytes.Length == 0) continue;

                    var text = ExtractTextFromXml(xmlBytes);
                    allText.Append(text);

                    var words = Regex.Matches(text, @"\w+").Count;
                    var sentences = Regex.Matches(text, @"[.!?]+\s").Count;
                    if (sentences == 0) sentences = 1;

                    totalWords += words;
                    totalSentences += sentences;
                    totalSentenceWords += words;
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Content fetch failed for {Slug} title-{Title} ch-{Ch}",
                        agency.Slug, cfr.Title, cfr.Chapter);
                }
            }

            // Compute metrics
            agency.WordCount = totalWords;
            agency.Complexity = totalSentences > 0
                ? Math.Round((double)totalSentenceWords / totalSentences, 1)
                : 0;

            // MD5 checksum of all concatenated text
            if (allText.Length > 0)
            {
                var hash = MD5.HashData(Encoding.UTF8.GetBytes(allText.ToString()));
                agency.Checksum = Convert.ToHexString(hash).ToLowerInvariant();
            }

            // RBI: (words/1000) * (avgSentenceLength/15) * log2(staleness+1) / 100
            agency.RbiScore = ComputeRbi(totalWords, agency.Complexity, refs, titles);
            agency.LastUpdated = DateTime.UtcNow;
            synced++;

            if (synced % 25 == 0)
            {
                await _db.SaveChangesAsync();
                _log.LogInformation("Content sync progress: {Count} agencies processed", synced);
            }
        }

        await _db.SaveChangesAsync();
        _log.LogInformation("Content sync done: {Count} agencies with metrics", synced);
    }

    private static double ComputeRbi(int wordCount, double avgSentenceLength,
        List<CfrRef> refs, Dictionary<int, Title> titles)
    {
        if (wordCount == 0) return 0;

        // Staleness = days since the most recent amendment across all referenced titles
        var maxStaleness = 0.0;
        foreach (var r in refs)
        {
            if (titles.TryGetValue(r.Title, out var t) && t.LatestAmended != null)
            {
                if (DateTime.TryParse(t.LatestAmended, out var amended))
                {
                    var days = (DateTime.UtcNow - amended).TotalDays;
                    if (days > maxStaleness) maxStaleness = days;
                }
            }
        }

        var volume = wordCount / 1000.0;
        var complexity = avgSentenceLength / 15.0;
        var staleness = Math.Log2(maxStaleness + 1);
        return Math.Round(volume * complexity * staleness / 100.0, 2);
    }

    /// Pull all text nodes from the XML content for word counting
    private static string ExtractTextFromXml(byte[] xmlBytes)
    {
        try
        {
            using var stream = new MemoryStream(xmlBytes);
            var doc = XDocument.Load(stream);
            var sb = new StringBuilder();

            foreach (var node in doc.DescendantNodes().OfType<XText>())
            {
                var text = node.Value.Trim();
                if (text.Length > 0)
                    sb.Append(text).Append(' ');
            }

            return sb.ToString();
        }
        catch
        {
            // If XML is malformed, fall back to raw text extraction
            return Encoding.UTF8.GetString(xmlBytes);
        }
    }

    private static List<CfrRef> ParseCfrReferences(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<CfrRef>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }
        catch { return []; }
    }

    /// Recursively flatten the agency tree since eCFR nests child agencies under parents
    private static List<AgencyDto> FlattenAgencies(List<AgencyDto> agencies)
    {
        var result = new List<AgencyDto>();
        foreach (var agency in agencies)
        {
            result.Add(agency);
            if (agency.Children is { Count: > 0 })
                result.AddRange(FlattenAgencies(agency.Children));
        }
        return result;
    }
}

// Simple record for deserializing the stored CFR reference JSON
record CfrRef(int Title, string? Chapter);