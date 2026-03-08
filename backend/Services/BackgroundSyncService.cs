using EcfrAnalyzer.Endpoints;

namespace EcfrAnalyzer.Services;

public class BackgroundSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundSyncService> _log;

    public BackgroundSyncService(IServiceScopeFactory scopeFactory, ILogger<BackgroundSyncService> log)
    { _scopeFactory = scopeFactory; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sync = scope.ServiceProvider.GetRequiredService<SyncService>();
                await sync.RunFullSyncAsync();
                AnalyticsEndpoints.ClearCache();
            }
            catch (Exception ex) { _log.LogError(ex, "Background sync error"); }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
