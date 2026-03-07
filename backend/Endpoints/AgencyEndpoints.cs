using EcfrAnalyzer.Data;
using Microsoft.EntityFrameworkCore;

namespace EcfrAnalyzer.Endpoints;

public static class AgencyEndpoints
{
    public static RouteGroupBuilder MapAgencyEndpoints(this RouteGroupBuilder group)
    {
        // List all agencies with optional sort and limit
        group.MapGet("/", async (AppDbContext db, string? sort, int? limit) =>
        {
            var query = db.Agencies.AsQueryable();

            query = sort?.ToLower() switch
            {
                "words" => query.OrderByDescending(a => a.WordCount),
                "rbi" => query.OrderByDescending(a => a.RbiScore),
                "name" => query.OrderBy(a => a.Name),
                _ => query.OrderByDescending(a => a.WordCount)
            };

            if (limit.HasValue && limit > 0)
                query = query.Take(limit.Value);

            var agencies = await query.Select(a => new
            {
                a.Slug,
                a.Name,
                a.ShortName,
                a.WordCount,
                a.Checksum,
                a.RbiScore,
                a.Complexity,
                a.LastUpdated
            }).ToListAsync();

            return Results.Ok(agencies);
        });

        // Single agency with its change history grouped by month for the timeline chart
        group.MapGet("/{slug}", async (AppDbContext db, string slug) =>
        {
            var agency = await db.Agencies.FindAsync(slug);
            if (agency == null)
                return Results.NotFound(new { error = "Agency not found" });

            // Group changes by YYYY-MM for the last 12 months
            var cutoff = DateTime.UtcNow.AddMonths(-12).ToString("yyyy-MM-dd");
            var monthlyCounts = await db.Changes
                .Where(c => c.AgencySlug == slug && c.ChangeDate.CompareTo(cutoff) >= 0)
                .GroupBy(c => c.ChangeDate.Substring(0, 7)) // group by YYYY-MM
                .Select(g => new { month = g.Key, count = g.Count() })
                .OrderBy(g => g.month)
                .ToListAsync();

            var totalChanges = await db.Changes.CountAsync(c => c.AgencySlug == slug);

            return Results.Ok(new
            {
                agency.Slug,
                agency.Name,
                agency.ShortName,
                agency.WordCount,
                agency.Checksum,
                agency.RbiScore,
                agency.Complexity,
                agency.LastUpdated,
                totalChanges,
                monthlyChanges = monthlyCounts
            });
        });

        return group;
    }
}