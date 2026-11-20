using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiseUp.Entities.Abstractions;
using RiseUp.Facade.Payments;
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
    public IActionResult SandboxCheckout(string orderNumber) => gateway.Mode == PaymentMode.Simulator ? View(model: orderNumber) : NotFound();

    /// <summary>SANDBOX ONLY: simulates the gateway calling our webhook with a signed payload.</summary>
    [Authorize(Policy = Policies.Founder), HttpPost("/wallet/sandbox-checkout/{orderNumber}")]
    public async Task<IActionResult> SandboxComplete(string orderNumber, string outcome, CancellationToken ct)
    {
        if (gateway.Mode != PaymentMode.Simulator) return NotFound();
        if (outcome == "redirect-only")
        {
            Flash(RiseUp.Framework.Results.Result.Failure("payment_unconfirmed", "A success redirect alone credits nothing. The order stays pending for reconciliation."));
            return ReturnToWallet(orderNumber);
        }
        var payload = $"{orderNumber}|SBX-{Random.Shared.Next(10000, 99999)}|{(outcome == "fail" ? "FAILED" : "PAID")}";
        var signature = outcome == "tampered" ? "forged" : gateway.CreateSandboxSignature(payload) ?? "";
        Flash(await payments.HandleWebhookAsync(payload, signature, ct));
        return ReturnToWallet(orderNumber);
    }
}
