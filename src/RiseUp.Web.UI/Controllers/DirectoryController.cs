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
    public async Task<IActionResult> Startup(int id, string? tab, CancellationToken ct)
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
        return View(new StartupDetailViewModel(result.Value!, new ProfilePublications(await posts.ListForStartupAsync(Actor, id, ct),
            await packages.GetPublicTeaserAsync(id, ct), tab == "reels", $"/startups/{id}"), spotlight));
    }

    [HttpGet("/" + UrlRewrite.Investors)]
    public async Task<IActionResult> Investors(string? q, CancellationToken ct) => View(await investors.SearchAsync(Actor, q, 200, ct));

    [HttpGet("/search")]
    public async Task<IActionResult> Search(string? q, CancellationToken ct) =>
        View(new ProfileSearchViewModel(q?.Trim() ?? "", await FindProfiles(q, "all", null, null, 20, ct)));

    [HttpGet("/search/suggestions")]
    public async Task<IActionResult> Suggestions(string? q, string scope, string? industry, FundingStage? stage, CancellationToken ct)
    {
        if (scope is not ("all" or "startups" or "investors")) return BadRequest();
        var results = await FindProfiles(q, scope, industry, stage, scope == "all" ? 4 : 8, ct);
        return Ok(results);
    }

    private async Task<IReadOnlyList<ProfileSuggestion>> FindProfiles(string? q, string scope, string? industry, FundingStage? stage, int limit, CancellationToken ct)
    {
        var query = q?.Trim();
        if (string.IsNullOrEmpty(query) || query.Length > 100) return [];
        var results = new List<ProfileSuggestion>();
        if (scope != "investors")
        {
            var matches = await startups.SearchAsync(Actor, new(query, industry, stage, null, "relevance", PageSize: limit), ct);
            results.AddRange(matches.Items.Select(s => new ProfileSuggestion(s.Id, "startups", s.Name, s.Industry, s.LogoUrl, null)));
        }
        if (scope != "startups")
        {
            var matches = await investors.SearchAsync(Actor, query, limit, ct);
            results.AddRange(matches.Select(i => new ProfileSuggestion(i.Id, "investors", i.Name, i.Affiliation, i.PhotoUrl, i.ReceivePitches)));
        }
        return results.OrderByDescending(r => r.Name.Equals(query, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(r => r.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase)).ThenBy(r => r.Name).ToList();
    }

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
