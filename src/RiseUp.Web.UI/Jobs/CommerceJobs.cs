using RiseUp.Entities.Abstractions;
using RiseUp.Facade.Notifications;
using RiseUp.Facade.Payments;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Web.UI.Jobs;

public sealed class CommerceJobs(IServiceScopeFactory scopes, IConfiguration config, ILogger<CommerceJobs> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            // A transport failure in one queue must not interrupt the other queue.
            await AttemptAsync(DeliverEmailsAsync, "email queue", ct);
            await AttemptAsync(ReconcileAsync, "payment queue", ct);
        } while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task DeliverEmailsAsync(CancellationToken ct)
    {
        if (!config.GetValue("Email:Enabled", false)) return;
        var origin = new Uri(config["Site:PublicOrigin"]!);
        using var scope = scopes.CreateScope();
        var now = scope.ServiceProvider.GetRequiredService<IClock>().UtcNow;
        var ids = await scope.ServiceProvider.GetRequiredService<IGovernanceRepository>().DueEmailsAsync(now, ct);
        foreach (var id in ids)
            await AttemptAsync(async token =>
            {
                using var delivery = scopes.CreateScope();
                await delivery.ServiceProvider.GetRequiredService<EmailDelivery>().DeliverAsync(id, origin, delivery.ServiceProvider.GetRequiredService<IEmailSender>(), token);
            }, $"email record {id}", ct);
    }

    private async Task ReconcileAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var due = await scope.ServiceProvider.GetRequiredService<IPaymentFacade>().DueForReconciliationAsync(ct);
        foreach (var order in due)
            await AttemptAsync(async token =>
            {
                using var check = scopes.CreateScope();
                var result = await check.ServiceProvider.GetRequiredService<IPaymentFacade>().ConfirmHostedAsync(order, null, token);
                if (result.Error.Code == ErrorCodes.PaymentUnavailable) logger.LogWarning("Provider check deferred for order {Order}.", order);
            }, $"payment order {order}", ct);
    }

    private async Task AttemptAsync(Func<CancellationToken, Task> action, string item, CancellationToken ct)
    {
        try { await action(ct); }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Delivery or reconciliation of {Item} will retry ({Failure}).", item, ex.GetType().Name);
        }
    }
}
