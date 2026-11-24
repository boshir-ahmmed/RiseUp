using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RiseUp.Entities.Common;
using RiseUp.Facade.Identity;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Web.UI.Infrastructure;
using RiseUp.Web.UI.ViewModels;

namespace RiseUp.Web.UI.Controllers;

[EnableRateLimiting("auth")]
public sealed class AccountController(IAccountFacade accounts, IWebHostEnvironment env, IConfiguration configuration) : AppController
{
    private bool RegistrationOpen => configuration.GetValue("Registration:Enabled", env.IsDevelopment());
    private const string PendingMfaKey = "riseup.pending-mfa";

    [HttpGet("/" + UrlRewrite.Account.Login)]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("/" + UrlRewrite.Account.Login)]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);
        // Team members sign in with a username; founders, investors and admins with email. Passwords are hashed (no plaintext predicate).
        var result = model.Identifier.Contains('@')
            ? await accounts.SignInAsync(model.Identifier, model.Password, ct)
            : await accounts.SignInTeamMemberAsync(model.Identifier, model.Password, ct);
        if (result.IsFailure) { ModelState.AddModelError("", result.Error.Message); return View(model); }
        var user = result.Value!;
        if (user.MfaRequired)
        {
            TempData[PendingMfaKey] = System.Text.Json.JsonSerializer.Serialize(user);
            return RedirectToAction(nameof(Mfa), new { returnUrl = model.ReturnUrl });
        }
        await SignInAsync(user, mfaSatisfied: false, model.RememberMe);
        return LocalRedirect(SafeReturn(model.ReturnUrl, user));
    }

    [HttpGet("/account/mfa")]
    public IActionResult Mfa(string? returnUrl = null)
    {
        if (TempData.Peek(PendingMfaKey) is null) return RedirectToAction(nameof(Login));
        return View(new MfaViewModel { ReturnUrl = returnUrl, DemoHint = env.IsDevelopment() });
    }

    [HttpPost("/account/mfa")]
    public async Task<IActionResult> Mfa(MfaViewModel model, CancellationToken ct)
    {
        if (TempData.Peek(PendingMfaKey) is not string json) return RedirectToAction(nameof(Login));
        var user = System.Text.Json.JsonSerializer.Deserialize<SignedInUser>(json)!;
        var check = await accounts.VerifyMfaAsync(user.AccountId!.Value, model.Code ?? "", ct);
        if (check.IsFailure) { ModelState.AddModelError("", check.Error.Message); model.DemoHint = env.IsDevelopment(); return View(model); }
        TempData.Remove(PendingMfaKey);
        await SignInAsync(user, mfaSatisfied: true, false);
        return LocalRedirect(SafeReturn(model.ReturnUrl, user));
    }

    [HttpGet("/" + UrlRewrite.Account.Register)]
    public IActionResult Register() => View(RegistrationOpen ? "RegisterRole" : "RegistrationClosed");

    [HttpGet("/" + UrlRewrite.Account.RegisterRole)]
    public IActionResult RegisterForm(string role) =>
        !RegistrationOpen && role is ("founder" or "investor") ? View("RegistrationClosed") : role is "founder" or "investor" ? View("Register", new RegisterViewModel { Role = role == "founder" ? PrimaryRole.Founder : PrimaryRole.Investor }) : NotFound();

    [HttpPost("/" + UrlRewrite.Account.Register)]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!RegistrationOpen) return View("RegistrationClosed");
        if (!ModelState.IsValid) return View("Register", model);
        var result = await accounts.RegisterAsync(new RegisterRequest(model.Role, model.FullName, model.Email, model.Phone, model.Password, model.AcceptTerms, model.StartupName, model.Affiliation), ct);
        if (result.IsFailure)
        {
            var field = result.Error.Code switch
            {
                ErrorCodes.EmailTaken => nameof(model.Email),
                ErrorCodes.PhoneTaken => nameof(model.Phone),
                _ => string.Empty
            };
            ModelState.AddModelError(field, result.Error.Message);
            return View("Register", model);
        }
        // In production the token is sent only by email (outbox). Development shows the link to make the flow testable.
        return View("CheckEmail", env.IsDevelopment() ? Url.Action(nameof(VerifyEmail), new { token = result.Value!.EmailVerificationToken }) : null);
    }

    [HttpPost("/account/resend-verification")]
    public async Task<IActionResult> ResendVerification(string email, CancellationToken ct)
    {
        Flash(await accounts.ResendVerificationAsync(email, ct));
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("/" + UrlRewrite.Account.VerifyEmail)]
    public async Task<IActionResult> VerifyEmail(string token, CancellationToken ct)
    {
        Flash(await accounts.VerifyEmailAsync(token, ct));
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("/" + UrlRewrite.Account.ForgotPassword)]
    public IActionResult ForgotPassword() => View();

    [HttpPost("/" + UrlRewrite.Account.ForgotPassword)]
    public async Task<IActionResult> ForgotPassword(string email, CancellationToken ct)
    {
        var result = await accounts.RequestPasswordResetAsync(email, ct);
        if (env.IsDevelopment() && !string.IsNullOrEmpty(result.Value)) TempData["devLink"] = Url.Action(nameof(ResetPassword), new { token = result.Value });
        Flash(Result.Success("If an account exists, a single-use reset link valid for 60 minutes has been sent."));
        return RedirectToAction(nameof(ForgotPassword));
    }

    [HttpGet("/" + UrlRewrite.Account.ResetPassword)]
    public IActionResult ResetPassword(string token) => View(model: token);

    [HttpPost("/" + UrlRewrite.Account.ResetPassword)]
    public async Task<IActionResult> ResetPassword(string token, string password, CancellationToken ct)
    {
        var result = await accounts.ResetPasswordAsync(token, password, ct);
        Flash(result);
        return result.IsSuccess ? RedirectToAction(nameof(Login)) : View(model: token);
    }

    [Authorize, HttpPost("/" + UrlRewrite.Account.Logout)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }

    [Authorize(Policy = Policies.FounderOrInvestor), HttpPost("/account/delete")]
    public async Task<IActionResult> Delete(string confirmation, CancellationToken ct)
    {
        var result = await accounts.DeleteAccountAsync(Actor, confirmation, ct);
        if (result.IsFailure) return Back(result, "Index");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Flash(result);
        return Redirect("/");
    }

    [HttpGet("/" + UrlRewrite.Account.AccessDenied)]
    public IActionResult AccessDenied() => View();

    private Task SignInAsync(SignedInUser user, bool mfaSatisfied, bool persistent) =>
        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            CurrentActor.ToPrincipal(user, mfaSatisfied, CookieAuthenticationDefaults.AuthenticationScheme),
            new AuthenticationProperties { IsPersistent = persistent });

    private string SafeReturn(string? returnUrl, SignedInUser user) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl
        : user.Role == PrimaryRole.Admin ? "/admin" : user.Role == PrimaryRole.TeamMember ? "/conversations" : "/dashboard";
}
