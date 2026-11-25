using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiseUp.Entities.Abstractions;
using RiseUp.Facade.Payments;
using RiseUp.Entities.Common;
using RiseUp.Framework.Results;
using RiseUp.Framework.Constants;
using RiseUp.Web.UI.Infrastructure;

namespace RiseUp.Web.UI.Controllers;

public sealed class PaymentsController(IPaymentFacade payments, IPaymentGateway gateway) : AppController
{
    /// <summary>Server-to-server confirmation. Browser redirects are never treated as proof of payment (FR-129).</summary>
    [AllowAnonymous, IgnoreAntiforgeryToken, HttpPost("/" + UrlRewrite.PaymentWebhook)]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(ct);
        return Json(await payments.HandleWebhookAsync(payload, Request.Headers["X-Signature"].ToString(), ct));
    }

    [AllowAnonymous, IgnoreAntiforgeryToken, RequestSizeLimit(32_768), HttpPost("/payments/sslcommerz/ipn")]
    public async Task<IActionResult> SslCommerzNotification([FromForm(Name = "tran_id")] string? order,
        [FromForm(Name = "val_id")] string? validationId, CancellationToken ct)
    {
        if (gateway is not IHostedPaymentGateway) return NotFound();
        if (string.IsNullOrWhiteSpace(order)) return BadRequest();
        var result = await payments.ConfirmHostedAsync(order, string.IsNullOrWhiteSpace(validationId) ? null : validationId, ct);
        return result.IsSuccess ? Ok() : StatusCode(ResultHttp.StatusFor(result.Error.Code));
    }

    [AllowAnonymous, IgnoreAntiforgeryToken, HttpGet("/payments/return"), HttpPost("/payments/return")]
    public IActionResult HostedReturn([ModelBinder(Name = "tran_id")] string? orderNumber) => ReturnToWallet(orderNumber);

    private RedirectToActionResult ReturnToWallet(string? order) => RedirectToAction("Wallet", "Workspace", new { order }, "orders");

    /// <summary>SANDBOX ONLY: a fake hosted checkout page. Unavailable when a real gateway is configured.</summary>
    [Authorize(Policy = Policies.Founder), HttpGet("/wallet/sandbox-checkout/{orderNumber}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> SandboxCheckout(string orderNumber, CancellationToken ct)
    {
        var order = await payments.GetSandboxOrderAsync(Actor, orderNumber, ct);
        if (order is null) return NotFound();
        return order.Status == PaymentStatus.Pending && !order.Credited ? View(order) : ReturnToWallet(orderNumber);
    }

    [Authorize(Policy = Policies.Founder), HttpPost("/wallet/sandbox-checkout/{orderNumber}")]
    public async Task<IActionResult> SandboxComplete(string orderNumber, string? method, string? outcome, CancellationToken ct)
    {
        var result = await payments.CompleteSandboxAsync(Actor, orderNumber, method, outcome, ct);
        if (result.Error.Code == ErrorCodes.NotFound) return NotFound();
        Flash(result);
        return ReturnToWallet(orderNumber);
    }
}
