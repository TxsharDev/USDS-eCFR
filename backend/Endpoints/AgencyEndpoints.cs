using EcfrAnalyzer.Data;
using Microsoft.EntityFrameworkCore;

namespace EcfrAnalyzer.Endpoints;

public static class AgencyEndpoints
{
    public static RouteGroupBuilder MapAgencyEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (AppDbContext db, string? sort, int? limit) =>
        {
            var query = sort?.ToLower() switch
            {
                "words" => db.Agencies.OrderByDescending(a => a.WordCount),
                "rbi" => db.Agencies.OrderByDescending(a => a.RbiScore),
                "name" => db.Agencies.OrderBy(a => a.Name),
                _ => db.Agencies.OrderByDescending(a => a.WordCount)
            };
            if (limit is > 0) query = (IOrderedQueryable<EcfrAnalyzer.Models.Agency>)query.Take(limit.Value);
            return Results.Ok(await query.Select(a => new
            {
                a.Slug, a.Name, a.ShortName, a.WordCount, a.Checksum, a.RbiScore, a.Complexity, a.LastUpdated
            }).ToListAsync());
        });

        group.MapGet("/{slug}", async (AppDbContext db, string slug) =>
        {
            var agency = await db.Agencies.FindAsync(slug);
            if (agency == null) return Results.NotFound(new { error = "Agency not found" });
            var cutoff = DateTime.UtcNow.AddMonths(-12).ToString("yyyy-MM-dd");
            var monthlyCounts = await db.Changes
                .Where(c => c.AgencySlug == slug && c.ChangeDate.CompareTo(cutoff) >= 0)
                .GroupBy(c => c.ChangeDate.Substring(0, 7))
                .Select(g => new { month = g.Key, count = g.Count() })
                .OrderBy(g => g.month).ToListAsync();
            return Results.Ok(new
            {
                agency.Slug, agency.Name, agency.ShortName, agency.WordCount,
                agency.Checksum, agency.RbiScore, agency.Complexity, agency.LastUpdated,
                totalChanges = await db.Changes.CountAsync(c => c.AgencySlug == slug),
                monthlyChanges = monthlyCounts
            });
        });

        return group;
    }
}
