using Microsoft.EntityFrameworkCore;
using EcfrAnalyzer.Data;
using EcfrAnalyzer.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=ecfr.db"));

// HTTP client for eCFR API
builder.Services.AddHttpClient<EcfrClient>(client =>
    client.Timeout = TimeSpan.FromSeconds(60));

// CORS
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Ensure database created and WAL (Write-Ahead Logging) mode enabled to let the
// frontend query analytics data without blocking the background sync writing new records.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

app.UseCors();

// Health endpoint
app.MapGet("/api/health", async (AppDbContext db) =>
{
    var agencyCount = await db.Agencies.CountAsync();
    var changeCount = await db.Changes.LongCountAsync();
    var lastSync = await db.SyncLogs
        .OrderByDescending(s => s.Id)
        .Select(s => s.CompletedAt)
        .FirstOrDefaultAsync();

    return Results.Ok(new
    {
        status = "healthy",
        version = "1.0.0",
        lastSync = lastSync?.ToString("o"),
        agencyCount,
        changeCount
    });
});

app.Run();