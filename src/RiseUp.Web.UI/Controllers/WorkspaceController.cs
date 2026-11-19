using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Facade.ConfidentialDocuments;
using RiseUp.Facade.ConnectionRequests;
using RiseUp.Facade.Conversations;
using RiseUp.Facade.Discovery;
using RiseUp.Facade.Investors;
using RiseUp.Facade.Notifications;
using RiseUp.Facade.Payments;
using RiseUp.Facade.Posts;
using RiseUp.Facade.Promotions;
using RiseUp.Facade.Startups;
using RiseUp.Facade.TeamAccounts;
using RiseUp.Facade.Verification;
using RiseUp.Facade.Wallets;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Web.UI.Infrastructure;
using RiseUp.Web.UI.ViewModels;

namespace RiseUp.Web.UI.Controllers;

/// <summary>Signed-in workspace for founders, investors and team members. Every action is policy-protected AND re-authorised in the Facade.</summary>
[Authorize]
public sealed class WorkspaceController(IConnectionRequestFacade requests, IMatchingFacade matching, IConversationFacade conversations,
    ITeamAccountFacade team, IDocumentFacade documents, IPostFacade posts, IPromotionFacade promotions, IWalletFacade wallets,
    IPaymentFacade payments, IStartupFacade startups, IInvestorFacade investors, IVerificationFacade verification,
    INotificationFacade notifications, IFileStorage storage, RiseUp.Facade.Pitching.IMeetingFacade meetings) : AppController
{
    [HttpGet("/" + UrlRewrite.Dashboard)]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var a = Actor;
        if (a.IsTeamMember) return Redirect("/conversations");
        if (a.IsAnyAdmin) return Redirect("/admin");
        var vm = new DashboardViewModel
        {
            Requests = await requests.ListForActorAsync(a, ct),
            Conversations = await conversations.ListAsync(a, ct),
            Matches = a.IsInvestor ? await matching.RecommendStartupsForInvestorAsync(a, 5, ct) : await matching.RecommendInvestorsForStartupAsync(a, 5, ct),
        };
        if (a.IsFounder)
        {
            vm.Wallet = (await wallets.GetAsync(a, ct)).Value;
            vm.Promotions = await promotions.ListForStartupAsync(a, ct);
            vm.Team = await team.ListAsync(a, ct);
            vm.Startup = (await startups.GetDetailAsync(a, a.StartupId!.Value, a.ViewerKey, ct)).Value;
        }
        if (a.IsInvestor)
        {
            vm.Investor = (await investors.GetDetailAsync(a, a.InvestorId!.Value, ct)).Value;
            vm.InvitationsUsedToday = await requests.InvitationsUsedTodayAsync(a.InvestorId!.Value, ct);
        }
        return View(vm);
    }

    // ------------------------------------------------------------ discovery
    [Authorize(Policy = Policies.FounderOrInvestor), HttpGet("/discover")]
    public async Task<IActionResult> Discover(CancellationToken ct) =>
        View(Actor.IsInvestor ? await matching.RecommendStartupsForInvestorAsync(Actor, 50, ct) : await matching.RecommendInvestorsForStartupAsync(Actor, 50, ct));

    // Pitch Studio (package, send, sent, checklist) lives in PitchStudioController.

    // ------------------------------------------------------------ requests
    [Authorize(Policy = Policies.FounderOrInvestor), HttpGet("/" + UrlRewrite.Requests)]
    public async Task<IActionResult> Requests(CancellationToken ct) => View(new RequestsViewModel
    {
        Items = await requests.ListForActorAsync(Actor, ct),
        InvitationsUsedToday = Actor.IsInvestor ? await requests.InvitationsUsedTodayAsync(Actor.InvestorId!.Value, ct) : 0,
        ReceivePitches = Actor.IsInvestor && (await investors.GetDetailAsync(Actor, Actor.InvestorId!.Value, ct)).Value?.Card.ReceivePitches == true,
    });

    [Authorize(Policy = Policies.FounderOrInvestor), HttpPost("/requests/{id:int}/respond")]
    public async Task<IActionResult> Respond(int id, bool accept, string? note, CancellationToken ct) =>
        Back(await requests.RespondAsync(Actor, id, accept, note, ct), nameof(Requests));

    // ------------------------------------------------------------ conversations
    [Authorize(Policy = Policies.ChatParticipant), HttpGet("/" + UrlRewrite.Conversations + "/{id:int?}")]
    public async Task<IActionResult> Conversations(int? id, CancellationToken ct)
    {
        var list = await conversations.ListAsync(Actor, ct);
        var selected = id ?? list.FirstOrDefault()?.Id;
        var detail = selected is int sid ? (await conversations.GetAsync(Actor, sid, 1, ct)) : null;
        if (detail is { IsFailure: true }) return Forbid();
        return View(new ConversationsViewModel(list, detail?.Value, Actor.IsFounder ? await team.ListAsync(Actor, ct) : [],
            selected is int msid ? await meetings.ListAsync(Actor, msid, ct) : []));
    }

    [Authorize(Policy = Policies.ChatParticipant), HttpPost("/conversations/{id:int}/messages")]
    public async Task<IActionResult> SendMessage(int id, string body, CancellationToken ct) { Flash(await conversations.SendAsync(Actor, id, body ?? "", ct) is { IsFailure: true } f ? f : Result.Success()); return Redirect($"/conversations/{id}"); }

    [Authorize(Policy = Policies.ChatParticipant), HttpPost("/messages/{id:int}/edit")]
    public async Task<IActionResult> EditMessage(int id, string body, int conversationId, CancellationToken ct) { Flash(await conversations.EditAsync(Actor, id, body, ct)); return Redirect($"/conversations/{conversationId}"); }

    [Authorize(Policy = Policies.ChatParticipant), HttpPost("/messages/{id:int}/delete")]
    public async Task<IActionResult> DeleteMessage(int id, int conversationId, CancellationToken ct) { Flash(await conversations.DeleteAsync(Actor, id, ct)); return Redirect($"/conversations/{conversationId}"); }

    [Authorize(Policy = Policies.Founder), HttpPost("/conversations/{id:int}/members")]
    public async Task<IActionResult> Members(int id, int[] teamIds, CancellationToken ct) { Flash(await conversations.SetTeamMembersAsync(Actor, id, teamIds, ct)); return Redirect($"/conversations/{id}"); }

    [Authorize(Policy = Policies.FounderOrInvestor), HttpPost("/conversations/{id:int}/block")]
    public async Task<IActionResult> Block(int id, bool blocked, CancellationToken ct) { Flash(await conversations.SetBlockedAsync(Actor, id, blocked, ct)); return Redirect($"/conversations/{id}"); }

    [Authorize(Policy = Policies.ChatParticipant), HttpPost("/conversations/{id:int}/mute")]
    public async Task<IActionResult> Mute(int id, bool muted, CancellationToken ct) { Flash(await conversations.SetMutedAsync(Actor, id, muted, ct)); return Redirect($"/conversations/{id}"); }

    // ------------------------------------------------------------ team accounts (owner)
    [Authorize(Policy = Policies.Founder), HttpGet("/" + UrlRewrite.Team)]
    public async Task<IActionResult> Team(CancellationToken ct) => View(await team.ListAsync(Actor, ct));

    [Authorize(Policy = Policies.Founder), HttpPost("/team")]
    public async Task<IActionResult> CreateTeam(CreateTeamAccountRequest request, CancellationToken ct)
    {
        var result = await team.CreateAsync(Actor, request, ct);
        Flash(result);
        if (result.IsSuccess) TempData["oneTime"] = $"{result.Value!.Username}|{result.Value.TemporaryPassword}";
        return RedirectToAction(nameof(Team));
    }

    [Authorize(Policy = Policies.Founder), HttpPost("/team/{id:int}/{op}")]
    public async Task<IActionResult> TeamOp(int id, string op, bool restore, CancellationToken ct)
    {
        if (op == "reset")
        {
            var r = await team.ResetPasswordAsync(Actor, id, ct);
            Flash(r);
            if (r.IsSuccess) TempData["oneTime"] = $"{r.Value!.Username}|{r.Value.TemporaryPassword}";
            return RedirectToAction(nameof(Team));
        }
        Result result = op switch
        {
            "deactivate" => await team.DeactivateAsync(Actor, id, ct),
            "reactivate" => await team.ReactivateAsync(Actor, id, restore, ct),
            "delete" => await team.DeleteAsync(Actor, id, ct),
            _ => Result.Failure(ErrorCodes.NotFound, "Unknown operation."),
        };
        Flash(result);
        return RedirectToAction(nameof(Team));
    }

    // ------------------------------------------------------------ confidential library
    [Authorize(Policy = Policies.FounderOrInvestor), HttpGet("/" + UrlRewrite.Library)]
    public async Task<IActionResult> Library(CancellationToken ct) => View(new LibraryViewModel(
        Actor.IsFounder ? await documents.ListOwnAsync(Actor, ct) : await documents.ListSharedWithMeAsync(Actor, ct),
        Actor.IsFounder ? (await conversations.ListAsync(Actor, ct)).Select(c => (c.InvestorId, c.Title)).ToList() : []));

    [Authorize(Policy = Policies.Founder), HttpPost("/library/upload"), RequestSizeLimit(21 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, DocumentKind kind, CancellationToken ct)
    {
        if (file is null || file.Length == 0) { Flash(Result.Failure(ErrorCodes.Validation, "Choose a file.")); return RedirectToAction(nameof(Library)); }
        if (!await FileSignatures.LooksLikeAsync(file, [".pdf", ".pptx"]))
        { Flash(Result.Failure(ErrorCodes.Validation, "The file content does not match an allowed PDF/PPTX signature.")); return RedirectToAction(nameof(Library)); }
        await using var stream = file.OpenReadStream();
        var key = await storage.SaveAsync(stream, file.FileName, ct);
        var result = await documents.RegisterUploadAsync(Actor, file.FileName, kind, file.Length, key, ct);
        if (result.IsFailure) await storage.DeleteAsync(key, ct);
        Flash(result);
        return RedirectToAction(nameof(Library));
    }

    [Authorize(Policy = Policies.Founder), HttpPost("/library/{id:int}/grant")]
    public async Task<IActionResult> Grant(int id, int investorId, bool grant, CancellationToken ct)
    {
        Flash(grant ? await documents.GrantAsync(Actor, id, investorId, ct) : await documents.RevokeAsync(Actor, id, investorId, ct));
        return RedirectToAction(nameof(Library));
    }

    [Authorize(Policy = Policies.FounderOrInvestor), HttpGet("/library/{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        var result = await documents.AuthorizeDownloadAsync(Actor, id, ct);
        if (result.IsFailure) return result.Error.Code == ErrorCodes.NotFound ? NotFound() : Forbid();
        var stream = await storage.OpenAsync(result.Value.StorageKey, ct);
        return stream is null ? NotFound() : File(stream, "application/octet-stream", result.Value.FileName);
    }

    // ------------------------------------------------------------ posts & promotions (owner)
    [Authorize(Policy = Policies.Founder), HttpGet("/my-posts")]
    public async Task<IActionResult> MyPosts(int? edit, CancellationToken ct) =>
        View(new MyPostsViewModel(await posts.ListOwnAsync(Actor, ct), await promotions.ListForStartupAsync(Actor, ct), (await wallets.GetAsync(Actor, ct)).Value, edit));

    [Authorize(Policy = Policies.Founder), HttpPost("/my-posts")]
    public async Task<IActionResult> SubmitPost(int? postId, PostCategory category, string title, string body, CancellationToken ct)
    {
        var result = await posts.SubmitAsync(Actor, new SubmitPostRequest(postId, category, title ?? "", body ?? ""), ct);
        Flash(result);
        return RedirectToAction(nameof(MyPosts), result.IsFailure ? new { edit = postId ?? 0 } : null);
    }

    /// <summary>Adds a ≤90 s video to a post (AJAX upload from the post editor; poster frame captured in the browser).</summary>
    [Authorize(Policy = Policies.Founder), HttpPost("/my-posts/{id:int}/video"), RequestSizeLimit(110 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 110 * 1024 * 1024)]
    public async Task<IActionResult> PostVideo(int id, IFormFile? video, IFormFile? poster, double? clientDuration, CancellationToken ct)
    {
        var (file, error) = await UploadSaver.SaveAsync(video, UploadSaver.VideoExtensions, storage, clientDuration, ct);
        if (file is null) return Json(Result.Failure(ErrorCodes.InvalidMedia, error!));
        var (posterFile, _) = poster is null ? (null, null) : await UploadSaver.SaveAsync(poster, UploadSaver.ImageExtensions, storage, null, ct);
        return Json(await posts.AttachVideoAsync(Actor, id, file, posterFile, ct));
    }

    [Authorize(Policy = Policies.Founder), HttpPost("/my-posts/{id:int}/promote")]
    public async Task<IActionResult> Promote(int id, int days, CancellationToken ct) { Flash(await promotions.PurchaseAsync(Actor, id, days, ct)); return RedirectToAction(nameof(MyPosts)); }

    // ------------------------------------------------------------ wallet & payments (owner)
    [Authorize(Policy = Policies.Founder), HttpGet("/" + UrlRewrite.Wallet)]
    public async Task<IActionResult> Wallet(CancellationToken ct)
    {
        var w = await wallets.GetAsync(Actor, ct);
        return View(new WalletViewModel(w.Value!, await payments.ListPackagesAsync(ct), await payments.ListOrdersAsync(Actor, ct)));
    }

    [Authorize(Policy = Policies.Founder), HttpPost("/wallet/buy")]
    public async Task<IActionResult> Buy(int packageId, CancellationToken ct)
    {
        var result = await payments.CreateOrderAsync(Actor, packageId, ct);
        if (result.IsFailure) { Flash(result); return RedirectToAction(nameof(Wallet)); }
        return LocalRedirect(result.Value!.RedirectUrl);
    }

    // ------------------------------------------------------------ profile, verification, settings, notifications
    [Authorize(Policy = Policies.FounderOrInvestor), HttpGet("/settings")]
    public async Task<IActionResult> Settings(CancellationToken ct) => View(new SettingsViewModel
    {
        Startup = Actor.IsFounder ? (await startups.GetDetailAsync(Actor, Actor.StartupId!.Value, Actor.ViewerKey, ct)).Value : null,
        Investor = Actor.IsInvestor ? (await investors.GetDetailAsync(Actor, Actor.InvestorId!.Value, ct)).Value : null,
    });

    [Authorize(Policy = Policies.Founder), HttpPost("/settings/startup")]
    public async Task<IActionResult> UpdateStartup(UpdateStartupRequest request, CancellationToken ct) { Flash(await startups.UpdateProfileAsync(Actor, request, ct)); return RedirectToAction(nameof(Settings)); }

    [Authorize(Policy = Policies.Investor), HttpPost("/settings/receive-pitches")]
    public async Task<IActionResult> ReceivePitches(bool receive, CancellationToken ct) => Back(await investors.SetReceivePitchesAsync(Actor, receive, ct), nameof(Settings));

    [Authorize(Policy = Policies.FounderOrInvestor), HttpPost("/verification")]
    public async Task<IActionResult> SubmitVerification(LicenceHolderType? licenceHolderType, string? licenceHolderName, List<IFormFile> files, CancellationToken ct)
    {
        var docs = new List<(string, string)>();
        foreach (var f in files.Where(f => f.Length > 0).Take(6))
        {
            if (!await FileSignatures.LooksLikeAsync(f, [".pdf", ".jpg", ".jpeg", ".png"])) { Flash(Result.Failure(ErrorCodes.Validation, $"{f.FileName} is not an allowed PDF or image.")); return RedirectToAction(nameof(Settings)); }
            await using var s = f.OpenReadStream();
            docs.Add((Path.GetExtension(f.FileName).TrimStart('.').ToUpperInvariant(), await storage.SaveAsync(s, f.FileName, ct)));
        }
        Flash(await verification.SubmitAsync(Actor, new SubmitVerificationRequest(licenceHolderType, licenceHolderName, docs), ct));
        return RedirectToAction(nameof(Settings));
    }

    [Authorize(Policy = Policies.Founder), HttpPost("/settings/ownership-transfer")]
    public async Task<IActionResult> Transfer(string incomingName, string incomingEmail, CancellationToken ct) =>
        Back(await verification.RequestOwnershipTransferAsync(Actor, incomingName, incomingEmail, ct), nameof(Settings));

    [HttpGet("/notifications")]
    public async Task<IActionResult> Notifications(CancellationToken ct)
    {
        var list = await notifications.ListAsync(Actor, ct);
        await notifications.MarkAllReadAsync(Actor, ct);
        return View(list);
    }
}
