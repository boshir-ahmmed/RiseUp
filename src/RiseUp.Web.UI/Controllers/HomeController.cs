using Microsoft.AspNetCore.Mvc;
using RiseUp.Entities.Common;
using RiseUp.Facade.Discovery;
using RiseUp.Facade.Investors;
using RiseUp.Facade.Payments;
using RiseUp.Facade.Pitching;
using RiseUp.Facade.Posts;
using RiseUp.Facade.Startups;
using RiseUp.Web.UI.Infrastructure;
using RiseUp.Web.UI.ViewModels;

namespace RiseUp.Web.UI.Controllers;

public sealed class HomeController(IPostFacade posts, IStartupFacade startups, IInvestorFacade investors, IMatchingFacade matching, IPaymentFacade payments,
    IPitchPackageFacade packages) : AppController
{
    /// <summary>Instagram-style feed homepage.</summary>
    [HttpGet("/")]
    public async Task<IActionResult> Index(string tab = "foryou", PostCategory? category = null, int take = 10, CancellationToken ct = default)
    {
        var actor = Actor;
        if (tab == "reels") return Redirect("/reels");
        if (tab == "saved" && !(actor.IsFounder || actor.IsInvestor)) tab = "foryou";
        var feed = await posts.GetFeedAsync(actor, tab, category, take, ct);
        var stories = await startups.SearchAsync(actor, new SearchStartupsRequest(null, null, null, null, "views", 1, 12), ct);
        var recommended = actor.IsInvestor ? await matching.RecommendStartupsForInvestorAsync(actor, 5, ct) : null;
        return View(new FeedViewModel(feed, stories.Items.Where(s => s.Status == VerificationStatus.Verified).ToList(),
            (await investors.ListAsync(actor, ct)).Take(4).ToList(), recommended, tab, category, take, await packages.ListSpotlightAsync(actor, 6, ct),
            actor.StartupId, actor.IsFounder && stories.Items.Any(s => s.Id == actor.StartupId)));
    }

    [HttpGet("/about")] public IActionResult About() => View();
    [HttpGet("/how-it-works")] public IActionResult HowItWorks() => View();
    [HttpGet("/faq")] public IActionResult Faq() => View();
    [HttpGet("/pricing")] public async Task<IActionResult> Pricing(CancellationToken ct) => View(await payments.ListPackagesAsync(ct));
    [HttpGet("/legal/{page}")] public IActionResult Legal(string page) => View(model: page);

    [Route("/error/{code:int?}"), IgnoreAntiforgeryToken]
    public IActionResult Error(int? code) => View("Error", code ?? 500);
}
