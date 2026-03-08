using Microsoft.EntityFrameworkCore;
using EcfrAnalyzer.Models;

namespace EcfrAnalyzer.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<Change> Changes => Set<Change>();
    public DbSet<Title> Titles => Set<Title>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Agency>(e => { e.HasKey(a => a.Slug); e.Property(a => a.Slug).HasMaxLength(200); e.Property(a => a.Name).IsRequired(); });
        m.Entity<Change>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.AgencySlug);
            e.HasIndex(c => c.ChangeDate);
            e.HasIndex(c => new { c.AgencySlug, c.ChangeDate }).HasDatabaseName("IX_Changes_Slug_Date");
        });
        m.Entity<Title>(e => e.HasKey(t => t.Number));
        m.Entity<SyncLog>(e => e.HasKey(s => s.Id));
    }
}
