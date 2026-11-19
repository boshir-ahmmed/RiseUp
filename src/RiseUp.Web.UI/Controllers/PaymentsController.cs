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

    /// <summary>SANDBOX ONLY: a fake hosted checkout page. Unavailable when a real gateway is configured.</summary>
    [Authorize(Policy = Policies.Founder), HttpGet("/wallet/sandbox-checkout/{orderNumber}")]
    public IActionResult SandboxCheckout(string orderNumber) => gateway.IsSandbox ? View(model: orderNumber) : NotFound();

    /// <summary>SANDBOX ONLY: simulates the gateway calling our webhook with a signed payload.</summary>
    [Authorize(Policy = Policies.Founder), HttpPost("/wallet/sandbox-checkout/{orderNumber}")]
    public async Task<IActionResult> SandboxComplete(string orderNumber, string outcome, CancellationToken ct)
    {
        if (!gateway.IsSandbox) return NotFound();
        if (outcome == "redirect-only")
        {
            Flash(RiseUp.Framework.Results.Result.Failure("payment_unconfirmed", "A success redirect alone credits nothing. The order stays pending for reconciliation."));
            return Redirect("/wallet");
        }
        var payload = $"{orderNumber}|SBX-{Random.Shared.Next(10000, 99999)}|{(outcome == "fail" ? "FAILED" : "PAID")}";
        var signature = outcome == "tampered" ? "forged" : gateway.CreateSandboxSignature(payload) ?? "";
        Flash(await payments.HandleWebhookAsync(payload, signature, ct));
        return Redirect("/wallet");
    }
}
