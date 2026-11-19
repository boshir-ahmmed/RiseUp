using RiseUp.Facade.ConfidentialDocuments;
using RiseUp.Facade.ConnectionRequests;
using RiseUp.Facade.Promotions;

namespace RiseUp.Web.UI.Jobs;

/// <summary>
/// Idempotent, re-runnable background jobs (request expiry, promotion clocks). For multi-instance production,
/// move these to a persistent scheduler (e.g. Quartz with a SQL job store) — the Facade calls stay the same.
/// </summary>
public sealed class ScheduledJobs(IServiceScopeFactory scopes, ILogger<ScheduledJobs> logger, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(configuration.GetValue("Jobs:IntervalSeconds", 60));
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var expired = await scope.ServiceProvider.GetRequiredService<IConnectionRequestFacade>().ExpireDueAsync(stoppingToken);
                var promos = await scope.ServiceProvider.GetRequiredService<IPromotionFacade>().RunClockAsync(stoppingToken);
                await scope.ServiceProvider.GetRequiredService<IDocumentFacade>().ProcessScanQueueAsync(stoppingToken);
                await scope.ServiceProvider.GetRequiredService<RiseUp.Facade.Media.IMediaFacade>().ProcessScanQueueAsync(stoppingToken);
                if (expired > 0) logger.LogInformation("Expired {Count} pending requests.", expired);
                logger.LogDebug("Promotion clock advanced for {Count} promotions.", promos);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Scheduled job run failed; it will retry on the next tick.");
            }
        }
    }
}
