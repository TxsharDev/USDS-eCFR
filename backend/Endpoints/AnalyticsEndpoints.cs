using EcfrAnalyzer.Data;
using Microsoft.EntityFrameworkCore;

namespace EcfrAnalyzer.Endpoints;

public static class AnalyticsEndpoints
{
    // Heatmap aggregates 155K+ change rows so we cache it for 5 minutes.
    // Invalidated on sync trigger so fresh data shows up after a re-sync.
    private static object? _heatmapCache;

    private static DateTime _heatmapCacheTime = DateTime.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public static RouteGroupBuilder MapAnalyticsEndpoints(this RouteGroupBuilder group)
    {
        // Top agencies by word count
        group.MapGet("/word-counts", async (AppDbContext db, int? limit) =>
        {
            var top = limit ?? 15;
            var results = await db.Agencies
                .Where(a => a.WordCount > 0)
                .OrderByDescending(a => a.WordCount)
                .Take(top)
                .Select(a => new { a.Slug, a.Name, a.ShortName, a.WordCount })
                .ToListAsync();

            return Results.Ok(results);
        });

        // Top agencies by RBI score
        group.MapGet("/burden-index", async (AppDbContext db, int? limit) =>
        {
            var top = limit ?? 15;
            var results = await db.Agencies
                .Where(a => a.RbiScore > 0)
                .OrderByDescending(a => a.RbiScore)
                .Take(top)
                .Select(a => new
                {
                    a.Slug,
                    a.Name,
                    a.ShortName,
                    a.RbiScore,
                    a.WordCount,
                    a.Complexity
                })
                .ToListAsync();

            return Results.Ok(results);
        });

        // Heatmap: top 15 most active agencies x months. Used by the frontend grid.
        group.MapGet("/heatmap", async (AppDbContext db, int? months) =>
        {
            if (_heatmapCache != null && DateTime.UtcNow - _heatmapCacheTime < CacheTtl)
                return Results.Ok(_heatmapCache);

            var monthCount = months ?? 12;
            var cutoff = DateTime.UtcNow.AddMonths(-monthCount).ToString("yyyy-MM-dd");

            // Get top 15 agencies by total changes in the period
            var topSlugs = await db.Changes
                .Where(c => c.ChangeDate.CompareTo(cutoff) >= 0)
                .GroupBy(c => c.AgencySlug)
                .OrderByDescending(g => g.Count())
                .Take(15)
                .Select(g => g.Key)
                .ToListAsync();

            // Get monthly counts for those agencies
            var data = await db.Changes
                .Where(c => topSlugs.Contains(c.AgencySlug) && c.ChangeDate.CompareTo(cutoff) >= 0)
                .GroupBy(c => new { c.AgencySlug, Month = c.ChangeDate.Substring(0, 7) })
                .Select(g => new { g.Key.AgencySlug, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            // Grab short names for the heatmap labels
            var names = await db.Agencies
                .Where(a => topSlugs.Contains(a.Slug))
                .ToDictionaryAsync(a => a.Slug, a => a.ShortName ?? a.Name);

            var result = new
            {
                agencies = topSlugs.Select(slug => new
                {
                    slug,
                    name = names.GetValueOrDefault(slug, slug),
                    months = data
                        .Where(d => d.AgencySlug == slug)
                        .OrderBy(d => d.Month)
                        .Select(d => new { d.Month, d.Count })
                        .ToList()
                }).ToList()
            };

            _heatmapCache = result;
            _heatmapCacheTime = DateTime.UtcNow;
            return Results.Ok(result);
        });

        // All agency checksums
        group.MapGet("/checksums", async (AppDbContext db) =>
        {
            var results = await db.Agencies
                .Where(a => a.Checksum != "")
                .OrderBy(a => a.Name)
                .Select(a => new { a.Slug, a.Name, a.ShortName, a.Checksum })
                .ToListAsync();

            return Results.Ok(results);
        });

        return group;
    }

    // Called after sync to invalidate the heatmap cache
    public static void ClearCache()
    {
        _heatmapCache = null;
        _heatmapCacheTime = DateTime.MinValue;
    }
}