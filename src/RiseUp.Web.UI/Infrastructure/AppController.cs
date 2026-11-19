using Microsoft.AspNetCore.Mvc;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;

namespace RiseUp.Web.UI.Infrastructure;

/// <summary>Base for thin controllers: bind → call Facade → map Result. No business logic lives here.</summary>
public abstract class AppController : Controller
{
    protected Actor Actor => CurrentActor.From(User);

    protected void Flash(Result result)
    {
        TempData["flash"] = result.IsSuccess ? (result.Message ?? "Done.") : result.Error.Message;
        TempData["flashCode"] = result.IsSuccess ? "" : result.Error.Code;
    }

    protected IActionResult Back(Result result, string fallbackAction, object? routeValues = null)
    {
        Flash(result);
        var referer = Request.Headers.Referer.ToString();
        return Uri.TryCreate(referer, UriKind.Absolute, out var uri) && uri.Host == Request.Host.Host
            ? LocalRedirect(uri.PathAndQuery) : RedirectToAction(fallbackAction, routeValues);
    }

    /// <summary>JSON for fetch() callers, with ProblemDetails-style errors and correct status codes.</summary>
    protected IActionResult Json(Result result, object? value = null) => result.IsSuccess
        ? Ok(new { ok = true, message = result.Message, value })
        : StatusCode(ResultHttp.StatusFor(result.Error.Code), new { ok = false, code = result.Error.Code, title = result.Error.Message });
}

public static class ResultHttp
{
    public static int StatusFor(string code) => code switch
    {
        ErrorCodes.Validation or ErrorCodes.ContentRejected => StatusCodes.Status400BadRequest,
        ErrorCodes.Unauthenticated or ErrorCodes.InvalidCredentials => StatusCodes.Status401Unauthorized,
        ErrorCodes.Forbidden or ErrorCodes.ForbiddenDocument or ErrorCodes.NotVerified or ErrorCodes.AccountSuspended => StatusCodes.Status403Forbidden,
        ErrorCodes.NotFound => StatusCodes.Status404NotFound,
        ErrorCodes.InvitationDailyLimit or ErrorCodes.AccountLocked => StatusCodes.Status429TooManyRequests,
        ErrorCodes.AiTemporarilyUnavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status409Conflict,
    };
}
