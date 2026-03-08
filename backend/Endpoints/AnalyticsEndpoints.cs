using EcfrAnalyzer.Data;
using Microsoft.EntityFrameworkCore;

namespace EcfrAnalyzer.Endpoints;

public static class AnalyticsEndpoints
{
    private static object? _heatmapCache;
    private static DateTime _heatmapCacheTime = DateTime.MinValue;

    public static RouteGroupBuilder MapAnalyticsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/word-counts", async (AppDbContext db, int? limit) =>
            Results.Ok(await db.Agencies.Where(a => a.WordCount > 0).OrderByDescending(a => a.WordCount)
                .Take(limit ?? 15).Select(a => new { a.Slug, a.Name, a.ShortName, a.WordCount }).ToListAsync()));

        group.MapGet("/burden-index", async (AppDbContext db, int? limit) =>
            Results.Ok(await db.Agencies.Where(a => a.RbiScore > 0).OrderByDescending(a => a.RbiScore)
                .Take(limit ?? 15).Select(a => new { a.Slug, a.Name, a.ShortName, a.RbiScore, a.WordCount, a.Complexity }).ToListAsync()));

        group.MapGet("/heatmap", async (AppDbContext db, int? months) =>
        {
            if (_heatmapCache != null && DateTime.UtcNow - _heatmapCacheTime < TimeSpan.FromMinutes(5))
                return Results.Ok(_heatmapCache);
            var cutoff = DateTime.UtcNow.AddMonths(-(months ?? 12)).ToString("yyyy-MM-dd");
            var topSlugs = await db.Changes.Where(c => c.ChangeDate.CompareTo(cutoff) >= 0)
                .GroupBy(c => c.AgencySlug).OrderByDescending(g => g.Count()).Take(15)
                .Select(g => g.Key).ToListAsync();
            var data = await db.Changes
                .Where(c => topSlugs.Contains(c.AgencySlug) && c.ChangeDate.CompareTo(cutoff) >= 0)
                .GroupBy(c => new { c.AgencySlug, Month = c.ChangeDate.Substring(0, 7) })
                .Select(g => new { g.Key.AgencySlug, g.Key.Month, Count = g.Count() }).ToListAsync();
            var names = await db.Agencies.Where(a => topSlugs.Contains(a.Slug))
                .ToDictionaryAsync(a => a.Slug, a => a.ShortName ?? a.Name);
            var result = new
            {
                agencies = topSlugs.Select(slug => new
                {
                    slug, name = names.GetValueOrDefault(slug, slug),
                    months = data.Where(d => d.AgencySlug == slug).OrderBy(d => d.Month)
                        .Select(d => new { d.Month, d.Count }).ToList()
                }).ToList()
            };
            _heatmapCache = result;
            _heatmapCacheTime = DateTime.UtcNow;
            return Results.Ok(result);
        });

        group.MapGet("/checksums", async (AppDbContext db) =>
            Results.Ok(await db.Agencies.Where(a => a.Checksum != "").OrderBy(a => a.Name)
                .Select(a => new { a.Slug, a.Name, a.ShortName, a.Checksum }).ToListAsync()));

        return group;
    }

    public static void ClearCache() { _heatmapCache = null; _heatmapCacheTime = DateTime.MinValue; }
}
