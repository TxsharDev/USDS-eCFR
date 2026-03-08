using Microsoft.EntityFrameworkCore;
using EcfrAnalyzer.Data;
using EcfrAnalyzer.Endpoints;
using EcfrAnalyzer.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=ecfr.db"));
builder.Services.AddHttpClient<EcfrClient>(c => c.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddScoped<SyncService>();
builder.Services.AddHostedService<BackgroundSyncService>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

app.UseCors();

app.MapGet("/api/health", async (AppDbContext db) =>
{
    var lastSync = await db.SyncLogs.OrderByDescending(s => s.Id).Select(s => s.CompletedAt).FirstOrDefaultAsync();
    return Results.Ok(new
    {
        status = "healthy",
        lastSync = lastSync?.ToString("o"),
        agencyCount = await db.Agencies.CountAsync(),
        changeCount = await db.Changes.LongCountAsync()
    });
});

app.MapPost("/api/sync/trigger", async (SyncService sync) =>
{
    var log = await sync.RunFullSyncAsync();
    AnalyticsEndpoints.ClearCache();
    return Results.Ok(new { status = log.Status, agencies = log.AgenciesSynced, error = log.ErrorMessage });
});

app.MapGroup("/api/agencies").MapAgencyEndpoints();
app.MapGroup("/api/analytics").MapAnalyticsEndpoints();
app.Run();
