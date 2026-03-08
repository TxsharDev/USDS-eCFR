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

    public async Task<SyncLog> RunFullSyncAsync()
    {
        var syncLog = new SyncLog { StartedAt = DateTime.UtcNow };
        _db.SyncLogs.Add(syncLog);
        await _db.SaveChangesAsync();
        try
        {
            var agencyCount = await SyncAgenciesAsync();
            await SyncTitlesAsync();
            await SyncVersionsAsync();
            await SyncContentAsync();
            syncLog.AgenciesSynced = agencyCount;
            syncLog.Status = "completed";
        }
        catch (Exception ex)
        {
            syncLog.Status = "failed";
            syncLog.ErrorMessage = ex.Message;
            _log.LogError(ex, "Full sync failed");
        }
        syncLog.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return syncLog;
    }

    public async Task<int> SyncAgenciesAsync()
    {
        var dtos = await _ecfr.FetchAgenciesAsync();
        var flat = FlattenAgencies(dtos);
        var existing = await _db.Agencies.ToDictionaryAsync(a => a.Slug);
        foreach (var dto in flat)
        {
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
                    Slug = dto.Slug, Name = dto.Name,
                    ShortName = dto.ShortName, CfrTitlesJson = cfrJson
                });
            }
        }
        await _db.SaveChangesAsync();
        return flat.Count;
    }

    public async Task SyncTitlesAsync()
    {
        var dtos = await _ecfr.FetchTitlesAsync();
        var existing = await _db.Titles.ToDictionaryAsync(t => t.Number);
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
                    Number = dto.Number, Name = dto.Name,
                    LatestAmended = dto.LatestAmendedOn, UpToDateAsOf = dto.UpToDateAsOf,
                    Reserved = dto.Reserved, LastFetched = DateTime.UtcNow
                });
            }
        }
        await _db.SaveChangesAsync();
    }

    public async Task SyncVersionsAsync()
    {
        var titles = await _db.Titles.Where(t => !t.Reserved).ToListAsync();
        var agencies = await _db.Agencies.ToListAsync();
        var titleToAgencies = new Dictionary<int, List<string>>();
        foreach (var agency in agencies)
        {
            foreach (var r in ParseCfrReferences(agency.CfrTitlesJson))
            {
                if (!titleToAgencies.ContainsKey(r.Title))
                    titleToAgencies[r.Title] = [];
                titleToAgencies[r.Title].Add(agency.Slug);
            }
        }
        var existingKeys = new HashSet<string>(
            await _db.Changes.Select(c => c.AgencySlug + "|" + c.ChangeDate + "|" + c.TitleNumber)
                .ToListAsync());
        var totalChanges = 0;
        foreach (var title in titles)
        {
            if (!titleToAgencies.ContainsKey(title.Number)) continue;
            try
            {
                var versions = await _ecfr.FetchVersionsAsync(title.Number);
                var slugs = titleToAgencies[title.Number];
                foreach (var v in versions.Where(v => v.Date != null))
                {
                    foreach (var slug in slugs)
                    {
                        var key = slug + "|" + v.Date + "|" + title.Number;
                        if (existingKeys.Contains(key)) continue;
                        _db.Changes.Add(new Change
                        {
                            AgencySlug = slug, TitleNumber = title.Number,
                            Part = v.Identifier, SectionName = v.Name,
                            ChangeDate = v.Date!, Substantive = v.Substantive, Removed = v.Removed
                        });
                        existingKeys.Add(key);
                        totalChanges++;
                    }
                }
                if (totalChanges > 0) await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to sync versions for title {Title}", title.Number);
            }
        }
    }

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
            var allText = new StringBuilder();
            foreach (var cfr in refs)
            {
                if (!titles.TryGetValue(cfr.Title, out var title)) continue;
                var date = title.UpToDateAsOf ?? title.LatestAmended;
                if (string.IsNullOrEmpty(date)) continue;
                try
                {
                    var xmlBytes = await _ecfr.FetchContentAsync(date, cfr.Title, cfr.Chapter);
                    if (xmlBytes == null || xmlBytes.Length == 0) continue;
                    var text = ExtractTextFromXml(xmlBytes);
                    allText.Append(text);
                    var words = Regex.Matches(text, @"\w+").Count;
                    var sentences = Math.Max(Regex.Matches(text, @"[.!?]+\s").Count, 1);
                    totalWords += words;
                    totalSentences += sentences;
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Content fetch failed for {Slug} title-{Title}", agency.Slug, cfr.Title);
                }
            }
            agency.WordCount = totalWords;
            agency.Complexity = totalSentences > 0 ? Math.Round((double)totalWords / totalSentences, 1) : 0;
            if (allText.Length > 0)
            {
                var hash = MD5.HashData(Encoding.UTF8.GetBytes(allText.ToString()));
                agency.Checksum = Convert.ToHexString(hash).ToLowerInvariant();
            }
            agency.RbiScore = ComputeRbi(totalWords, agency.Complexity, refs, titles);
            agency.LastUpdated = DateTime.UtcNow;
            if (++synced % 25 == 0) await _db.SaveChangesAsync();
        }
        await _db.SaveChangesAsync();
    }

    private static double ComputeRbi(int wordCount, double avgSL, List<CfrRef> refs, Dictionary<int, Title> titles)
    {
        if (wordCount == 0) return 0;
        var maxStaleness = 0.0;
        foreach (var r in refs)
        {
            if (titles.TryGetValue(r.Title, out var t) && t.LatestAmended != null
                && DateTime.TryParse(t.LatestAmended, out var amended))
            {
                var days = (DateTime.UtcNow - amended).TotalDays;
                if (days > maxStaleness) maxStaleness = days;
            }
        }
        return Math.Round(wordCount / 1000.0 * (avgSL / 15.0) * Math.Log2(maxStaleness + 1) / 100.0, 2);
    }

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
                if (text.Length > 0) sb.Append(text).Append(' ');
            }
            return sb.ToString();
        }
        catch { return Encoding.UTF8.GetString(xmlBytes); }
    }

    private static List<CfrRef> ParseCfrReferences(string json)
    {
        try { return JsonSerializer.Deserialize<List<CfrRef>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? []; }
        catch { return []; }
    }

    private static List<AgencyDto> FlattenAgencies(List<AgencyDto> agencies)
    {
        var result = new List<AgencyDto>();
        foreach (var a in agencies)
        {
            result.Add(a);
            if (a.Children is { Count: > 0 }) result.AddRange(FlattenAgencies(a.Children));
        }
        return result;
    }
}

record CfrRef(int Title, string? Chapter);
