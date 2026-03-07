using EcfrAnalyzer.Endpoints;

namespace EcfrAnalyzer.Services;

// Runs a full eCFR sync on startup and then every 24 hours.
// Uses IServiceScopeFactory since BackgroundService is a singleton
// but SyncService/DbContext are scoped (one per request).
public class BackgroundSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundSyncService> _log;
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(24);

    public BackgroundSyncService(IServiceScopeFactory scopeFactory, ILogger<BackgroundSyncService> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small delay on startup so the rest of the app can finish initializing
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _log.LogInformation("Background sync starting");

                using var scope = _scopeFactory.CreateScope();
                var sync = scope.ServiceProvider.GetRequiredService<SyncService>();
                var result = await sync.RunFullSyncAsync();

                // Warm the heatmap cache after sync so first page load is fast
                AnalyticsEndpoints.ClearCache();

                _log.LogInformation("Background sync finished: {Status}, {Count} agencies",
                    result.Status, result.AgenciesSynced);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Background sync error");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }
}
