using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiseUp.Entities.Common;
using RiseUp.Facade.Moderation;
using RiseUp.Facade.Payments;
using RiseUp.Facade.Profiles;
using RiseUp.Facade.Startups;
using RiseUp.Facade.Verification;
using RiseUp.Facade.Wallets;
using RiseUp.Framework.Constants;
using RiseUp.Web.UI.Infrastructure;
using RiseUp.Web.UI.ViewModels;

namespace RiseUp.Web.UI.Areas.Admin.Controllers;

/// <summary>Least privilege: each controller is locked to one admin role; all require a satisfied second factor.</summary>
[Area("Admin"), Authorize(Policy = Policies.AnyAdmin)]
public sealed class OverviewController(IVerificationFacade verification, IModerationFacade moderation, IPaymentFacade payments) : AppController
{
    [HttpGet("/admin")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(new AdminOverviewViewModel(
        Actor.AdminRole,
        (await verification.ListQueueAsync(Actor, ct)).Count(a => a.Status == VerificationStatus.Pending),
        (await verification.ListTransfersAsync(Actor, ct)).Count(t => t.Status is TransferStatus.AwaitingIncomingConsent or TransferStatus.AwaitingReview),
        (await moderation.ListReportsAsync(Actor, ct)).Count(r => r.Status == ReportStatus.Open),
        (await payments.ListOrdersAsync(Actor, ct)).Count(o => o.Status != PaymentStatus.Paid),
        (await moderation.ListAuditAsync(Actor, ct)).Count));
}

[Area("Admin"), Authorize(Policy = Policies.VerificationAdmin)]
public sealed class VerificationController(IVerificationFacade verification) : AppController
{
    [HttpGet("/admin/verification")]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(new VerificationQueueViewModel(await verification.ListQueueAsync(Actor, ct), await verification.ListTransfersAsync(Actor, ct)));

    [HttpPost("/admin/verification/{id:int}/evidence/{documentId:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Evidence(int id, int documentId, CancellationToken ct)
    {
        var result = await verification.OpenEvidenceAsync(Actor, id, documentId, ct);
        if (result.IsFailure) return Back(result, nameof(Index));
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(result.Value.Content, "application/octet-stream", result.Value.FileName);
    }

    [Authorize(Policy = Policies.SuperAdmin), HttpPost("/admin/verification/{id:int}/evidence/{documentId:int}/hold")]
    public async Task<IActionResult> Hold(int id, int documentId, bool hold, string reason, CancellationToken ct) =>
        Back(await verification.SetEvidenceHoldAsync(Actor, id, documentId, hold, reason, ct), nameof(Index));

    [HttpPost("/admin/verification/{id:int}/decide")]
    public async Task<IActionResult> Decide(int id, bool approve, string note, DateOnly? earliestEvidenceExpiry, CancellationToken ct) =>
        Back(approve && !ModelState.IsValid ? RiseUp.Framework.Results.Result.Failure(RiseUp.Framework.Results.ErrorCodes.Validation, "Check the decision note and evidence expiry date.") :
            approve ? await verification.ApproveAsync(Actor, id, note ?? "", earliestEvidenceExpiry, ct) : await verification.RejectAsync(Actor, id, note ?? "", ct), nameof(Index));

    [HttpPost("/admin/verification/expire")]
    public async Task<IActionResult> Expire(VerificationSubject subject, int subjectId, string reason, CancellationToken ct) =>
        Back(await verification.ExpireAsync(Actor, subject, subjectId, reason ?? "evidence expired", ct), nameof(Index));

    [HttpPost("/admin/transfers/{id:int}/decide")]
    public async Task<IActionResult> DecideTransfer(int id, bool approve, string note, [FromServices] IWebHostEnvironment environment, CancellationToken ct) =>
        !environment.IsDevelopment() ? NotFound() : Back(await verification.DecideTransferAsync(Actor, id, approve, note ?? "", ct), nameof(Index));
}

/// <summary>Display-name changes on verified accounts are checked against the ID on file before they go live.</summary>
[Area("Admin"), Authorize(Policy = Policies.VerificationAdmin)]
public sealed class ProfileChangesController(IProfileFacade profiles) : AppController
{
    [HttpGet("/admin/profile-changes")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(new NameChangesViewModel(await profiles.ListNameChangesAsync(Actor, ct)));

    [HttpPost("/admin/profile-changes/{id:int}/decide")]
    public async Task<IActionResult> Decide(int id, bool approve, string? reason, CancellationToken ct) =>
        Back(await profiles.DecideNameChangeAsync(Actor, id, approve, reason, ct), nameof(Index));
}

[Area("Admin"), Authorize(Policy = Policies.Moderator)]
public sealed class ReportsController(IModerationFacade moderation, IStartupFacade startups) : AppController
{
    [HttpGet("/admin/reports")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await moderation.ListReportsAsync(Actor, ct));

    [HttpPost("/admin/reports/{id:int}/inspect")]
    public async Task<IActionResult> Inspect(int id, string reason, CancellationToken ct)
    {
        var result = await moderation.InspectReportedMessageAsync(Actor, id, reason ?? "", ct);
        if (result.IsSuccess) TempData[$"inspected:{id}"] = result.Value;
        return Back(result, nameof(Index));
    }

    [HttpPost("/admin/reports/{id:int}/resolve")]
    public async Task<IActionResult> Resolve(int id, bool actioned, string note, CancellationToken ct) =>
        Back(await moderation.ResolveAsync(Actor, id, actioned, note ?? "", ct), nameof(Index));

    [HttpPost("/admin/startups/{id:int}/suspend")]
    public async Task<IActionResult> Suspend(int id, bool suspend, string reason, CancellationToken ct) =>
        Back(suspend ? await startups.SuspendAsync(Actor, id, reason ?? "", ct) : await startups.LiftSuspensionAsync(Actor, id, reason ?? "", ct), nameof(Index));
}

[Area("Admin"), Authorize(Policy = Policies.FinanceAdmin)]
public sealed class FinanceController(IPaymentFacade payments, IWalletFacade wallets) : AppController
{
    [HttpGet("/admin/finance")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await payments.ListOrdersAsync(Actor, ct));

    [HttpPost("/admin/finance/reconcile")]
    public async Task<IActionResult> Reconcile(string orderNumber, string providerTransactionId, string reason, CancellationToken ct) =>
        Back(await payments.ReconcileAsync(Actor, orderNumber, providerTransactionId, reason ?? "", ct), nameof(Index));

    [HttpPost("/admin/finance/adjust")]
    public async Task<IActionResult> Adjust(int startupId, TokenBucket bucket, int amount, string reason, CancellationToken ct) =>
        Back(await wallets.AdjustAsync(Actor, startupId, bucket, amount, reason ?? "", ct), nameof(Index));
}

[Area("Admin"), Authorize(Policy = Policies.SuperAdmin)]
public sealed class AuditController(IModerationFacade moderation) : AppController
{
    [HttpGet("/admin/audit")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await moderation.ListAuditAsync(Actor, ct));
}
