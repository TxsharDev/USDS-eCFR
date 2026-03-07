using System.Text.Json;
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

    /// Runs agencies + titles sync pipeline. Wraps each phase in try/catch
    /// so partial progress is saved if eCFR goes down mid-sync.
    public async Task<SyncLog> RunMetadataSyncAsync()
    {
        var syncLog = new SyncLog { StartedAt = DateTime.UtcNow };
        _db.SyncLogs.Add(syncLog);
        await _db.SaveChangesAsync();

        try
        {
            var agencyCount = await SyncAgenciesAsync();
            var titleCount = await SyncTitlesAsync();

            syncLog.AgenciesSynced = agencyCount;
            syncLog.Status = "completed";
            syncLog.CompletedAt = DateTime.UtcNow;
            _log.LogInformation("Metadata sync completed: {Agencies} agencies, {Titles} titles",
                agencyCount, titleCount);
        }
        catch (Exception ex)
        {
            syncLog.Status = "failed";
            syncLog.ErrorMessage = ex.Message;
            syncLog.CompletedAt = DateTime.UtcNow;
            _log.LogError(ex, "Metadata sync failed");
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