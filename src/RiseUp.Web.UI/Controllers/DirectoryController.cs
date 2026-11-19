using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RiseUp.Entities.Common;
using RiseUp.Facade.ConnectionRequests;
using RiseUp.Facade.Investors;
using RiseUp.Facade.Moderation;
using RiseUp.Facade.Pitching;
using RiseUp.Facade.Posts;
using RiseUp.Facade.Startups;
using RiseUp.Framework.Constants;
using RiseUp.Web.UI.Infrastructure;
using RiseUp.Web.UI.ViewModels;

namespace RiseUp.Web.UI.Controllers;

/// <summary>Public directory: startups, investors and posts. Restricted fields are filtered in the Facade.</summary>
public sealed class DirectoryController(IStartupFacade startups, IInvestorFacade investors, IPostFacade posts,
    IConnectionRequestFacade requests, IModerationFacade moderation, IPitchPackageFacade packages) : AppController
{
    [HttpGet("/" + UrlRewrite.Startups)]
    public async Task<IActionResult> Startups([FromQuery] SearchStartupsRequest query, CancellationToken ct) =>
        View(new StartupSearchViewModel(query, await startups.SearchAsync(Actor, query, ct)));

    [HttpGet("/" + UrlRewrite.StartupDetail)]
    public async Task<IActionResult> Startup(int id, CancellationToken ct)
    {
        // FR-120: anonymous views are deduplicated by a random, HttpOnly, 24-hour cookie (no personal data).
        var anon = Request.Cookies["RiseUp.Anon"];
        if (string.IsNullOrEmpty(anon))
        {
            anon = Guid.NewGuid().ToString("N");
            Response.Cookies.Append("RiseUp.Anon", anon, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromHours(24), IsEssential = true });
        }
        var viewer = Actor.IsAuthenticated ? Actor.ViewerKey : $"anon:{anon}";
        var result = await startups.GetDetailAsync(Actor, id, viewer, ct);
        if (result.IsFailure) return NotFound();
        var spotlight = (await packages.ListSpotlightAsync(Actor, 30, ct)).FirstOrDefault(s => s.StartupId == id);
        return View(new StartupDetailViewModel(result.Value!, await posts.ListForStartupAsync(Actor, id, ct), spotlight));
    }

    [HttpGet("/" + UrlRewrite.Investors)]
    public async Task<IActionResult> Investors(CancellationToken ct) => View(await investors.ListAsync(Actor, ct));

    [HttpGet("/" + UrlRewrite.InvestorDetail)]
    public async Task<IActionResult> Investor(int id, CancellationToken ct)
    {
        var result = await investors.GetDetailAsync(Actor, id, ct);
        return result.IsFailure ? NotFound() : View(result.Value);
    }

    [HttpGet("/" + UrlRewrite.PostDetail)]
    public async Task<IActionResult> Post(int id, CancellationToken ct)
    {
        var result = await posts.GetDetailAsync(Actor, id, ct);
        return result.IsFailure ? NotFound() : View(result.Value);
    }

    [Authorize(Policy = Policies.FounderOrInvestor), HttpPost("/posts/{id:int}/like")]
    public async Task<IActionResult> Like(int id, CancellationToken ct) { var r = await posts.ToggleLikeAsync(Actor, id, ct); return Json(r, r.Value); }

    [Authorize(Policy = Policies.FounderOrInvestor), HttpPost("/posts/{id:int}/share")]
    public async Task<IActionResult> Share(int id, CancellationToken ct) { var r = await posts.ShareAsync(Actor, id, ct); return Json(r, r.Value); }

    [Authorize(Policy = Policies.FounderOrInvestor), HttpPost("/posts/{id:int}/comments")]
    public async Task<IActionResult> Comment(int id, string body, int? parentId, CancellationToken ct) =>
        Back(await posts.CommentAsync(Actor, id, parentId, body, ct), nameof(Post), new { id });

    [Authorize(Policy = Policies.FounderOrInvestor), HttpPost("/comments/{id:int}/delete")]
    public async Task<IActionResult> DeleteComment(int id, CancellationToken ct) => Back(await posts.DeleteCommentAsync(Actor, id, ct), nameof(Startups));

    [Authorize(Policy = Policies.Investor), HttpPost("/startups/{id:int}/invite"), EnableRateLimiting("requests")]
    public async Task<IActionResult> Invite(int id, string? note, string idempotencyKey, CancellationToken ct) =>
        Back(await requests.SendInvitationAsync(Actor, id, note, idempotencyKey, ct), nameof(Startup), new { id });

    [Authorize, HttpPost("/reports")]
    public async Task<IActionResult> Report(ReportKind kind, string target, string reason, int? conversationId, int? messageId, CancellationToken ct) =>
        Back(await moderation.ReportAsync(Actor, kind, target, reason, conversationId, messageId, ct), nameof(Startups));
}
