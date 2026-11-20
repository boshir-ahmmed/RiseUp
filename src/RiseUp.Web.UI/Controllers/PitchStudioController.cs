using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Facade.ConnectionRequests;
using RiseUp.Facade.Investors;
using RiseUp.Facade.Pitching;
using RiseUp.Facade.Wallets;
using RiseUp.Facade.Startups;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Web.UI.Infrastructure;
using RiseUp.Web.UI.ViewModels;

namespace RiseUp.Web.UI.Controllers;

/// <summary>Founder Pitch Studio: package editor, send a pitch, sent-pitch analytics and checklist.</summary>
[Authorize(Policy = Policies.Founder)]
public sealed class PitchStudioController(IPitchPackageFacade package, IPitchRoomFacade rooms, IConnectionRequestFacade requests, IInvestorFacade investors,
    IWalletFacade wallets, IStartupFacade startups, IFileStorage storage) : AppController
{
    private const long VideoLimit = 210L * 1024 * 1024;

    [HttpGet("/" + UrlRewrite.Pitches)]
    public async Task<IActionResult> Index(string tab = "package", int? to = null, int? draft = null, CancellationToken ct = default) =>
        await Studio(tab, to, draft, ct);

    private async Task<IActionResult> Studio(string tab, int? to, int? draft, CancellationToken ct,
        string? note = null, int[]? selectedDocuments = null, string? sendKey = null)
    {
        var p = await package.GetOwnAsync(Actor, ct);
        if (p.IsFailure) { Flash(p); return Redirect("/dashboard"); }
        var detail = (await startups.GetDetailAsync(Actor, Actor.StartupId!.Value, Actor.ViewerKey, ct)).Value;
        var drafts = await requests.ListDraftsAsync(Actor, ct);
        var recipients = await investors.ListAsync(Actor, ct);
        var selected = to ?? drafts.FirstOrDefault(d => d.Id == draft)?.InvestorId;
        if (selected is int id && !recipients.Any(i => i.Id == id))
        {
            var recipient = await investors.GetDetailAsync(Actor, id, ct);
            if (recipient.IsSuccess) recipients = [.. recipients, recipient.Value!.Card];
        }
        return View("Index", new PitchStudioViewModel
        {
            Tab = tab is "send" or "sent" or "guide" ? tab : to is not null || draft is not null ? "send" : "package",
            Package = p.Value!, Investors = recipients, Drafts = drafts,
            Sent = await rooms.ListSentAsync(Actor, ct), Wallet = (await wallets.GetAsync(Actor, ct)).Value, StartupVerified = detail?.Status == VerificationStatus.Verified,
            ToInvestorId = to, EditDraftId = draft, SubmittedNote = note, SelectedDocuments = selectedDocuments, IdempotencyKey = sendKey ?? Guid.NewGuid().ToString("N"),
        });
    }

    // ------------------------------------------------------------ send (note → AI Model 1 only; 1 credit on delivery)
    [HttpPost("/pitches/send"), EnableRateLimiting("requests")]
    public async Task<IActionResult> Send(int investorId, string text, string idempotencyKey, int? draftId, int[]? documentIds, bool documentsChosen, CancellationToken ct)
    {
        var result = await requests.SendPitchAsync(Actor, new SendPitchRequest(investorId, text ?? "", idempotencyKey, draftId, documentsChosen ? documentIds ?? [] : null), ct);
        Flash(result);
        if (result.IsSuccess) return Redirect("/pitches?tab=sent");
        return await Studio("send", investorId, draftId, ct, text ?? "", documentsChosen ? documentIds ?? [] : null, idempotencyKey);
    }

    [HttpPost("/pitches/draft")]
    public async Task<IActionResult> SaveDraft(int? investorId, string text, int? draftId, CancellationToken ct)
    {
        var result = await requests.SaveDraftAsync(Actor, investorId, text ?? "", draftId, ct);
        Flash(result);
        return Redirect($"/pitches?tab=send&draft={result.Value}");
    }

    // ------------------------------------------------------------ videos (AJAX from studio.js)
    [HttpPost("/pitches/package/video/{slot}"), RequestSizeLimit(VideoLimit), RequestFormLimits(MultipartBodyLengthLimit = VideoLimit)]
    public async Task<IActionResult> Video(PitchVideoSlot slot, IFormFile? video, IFormFile? poster, double? clientDuration, CancellationToken ct)
    {
        if (slot is not (PitchVideoSlot.Pitch or PitchVideoSlot.Demo))
            return Json(Result.Failure(ErrorCodes.Validation, "Create reels in Post & Reels."));
        var (file, error) = await UploadSaver.SaveAsync(video, UploadSaver.VideoExtensions, storage, clientDuration, ct);
        if (file is null) return Json(Result.Failure(ErrorCodes.InvalidMedia, error!));
        var (posterFile, _) = poster is null ? (null, null) : await UploadSaver.SaveAsync(poster, UploadSaver.ImageExtensions, storage, null, ct);
        var result = await package.SetVideoAsync(Actor, slot, file, posterFile, ct);
        if (result.IsSuccess) Flash(result);
        return Json(result);
    }

    [HttpPost("/pitches/package/video/{slot}/remove")]
    public async Task<IActionResult> RemoveVideo(PitchVideoSlot slot, CancellationToken ct) { Flash(await package.RemoveVideoAsync(Actor, slot, ct)); return Redirect("/pitches"); }

    [HttpPost("/pitches/package/visibility")]
    public async Task<IActionResult> Visibility(bool publicDemo, bool captions, CancellationToken ct) { Flash(await package.SetVisibilityAsync(Actor, null, publicDemo, captions, ct)); return Redirect("/pitches"); }

    // ------------------------------------------------------------ deck
    [HttpPost("/pitches/package/deck"), RequestSizeLimit(21 * 1024 * 1024)]
    public async Task<IActionResult> Deck(IFormFile? file, CancellationToken ct)
    {
        var (saved, error) = await UploadSaver.SaveAsync(file, UploadSaver.DeckExtensions, storage, null, ct);
        Flash(saved is null ? Result.Failure(ErrorCodes.InvalidMedia, error!) : await package.SetDeckAsync(Actor, saved, ct));
        return Redirect("/pitches#deck");
    }

    [HttpPost("/pitches/package/deck/remove")]
    public async Task<IActionResult> RemoveDeck(CancellationToken ct) { Flash(await package.RemoveDeckAsync(Actor, ct)); return Redirect("/pitches#deck"); }

    [HttpPost("/pitches/package/slides")]
    public async Task<IActionResult> Slides(string[]? titles, string[]? bodies, CancellationToken ct)
    {
        titles ??= []; bodies ??= [];
        var slides = titles.Select((t, i) => new SlideDto(t ?? "", i < bodies.Length ? bodies[i] ?? "" : "")).ToList();
        Flash(await package.SaveSlidesAsync(Actor, slides, ct));
        return Redirect("/pitches#deck");
    }

    // ------------------------------------------------------------ documents
    [HttpPost("/pitches/package/documents"), RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> AddDocument(IFormFile? file, string? label, CancellationToken ct)
    {
        var (saved, error) = await UploadSaver.SaveAsync(file, UploadSaver.DocumentExtensions, storage, null, ct);
        Flash(saved is null ? Result.Failure(ErrorCodes.InvalidMedia, error!) : await package.AddDocumentAsync(Actor, saved, label ?? "", ct));
        return Redirect("/pitches#documents");
    }

    [HttpPost("/pitches/package/documents/{id:int}/remove")]
    public async Task<IActionResult> RemoveDocument(int id, CancellationToken ct) { Flash(await package.RemoveDocumentAsync(Actor, id, ct)); return Redirect("/pitches#documents"); }

    [HttpPost("/pitches/package/documents/{id:int}/download")]
    public async Task<IActionResult> DocumentDownload(int id, bool allow, CancellationToken ct) { Flash(await package.SetDocumentDownloadAsync(Actor, id, allow, ct)); return Redirect("/pitches#documents"); }

    // ------------------------------------------------------------ ask + story
    [HttpPost("/pitches/package/ask")]
    public async Task<IActionResult> Ask(decimal amountUsd, FundingInstrument instrument, decimal valuationUsd, int discountPercent, int runwayMonths, decimal minTicketUsd,
        string[]? fundLabels, int[]? fundPercents, CancellationToken ct)
    {
        fundLabels ??= []; fundPercents ??= [];
        var funds = fundLabels.Select((l, i) => (l, p: i < fundPercents.Length ? fundPercents[i] : 0)).Where(x => !string.IsNullOrWhiteSpace(x.l)).Select(x => new FundsItem(x.l.Trim(), x.p)).ToList();
        Flash(await package.SaveAskAsync(Actor, new AskInput(amountUsd, instrument, valuationUsd, discountPercent, runwayMonths, minTicketUsd, funds), ct));
        return Redirect("/pitches#ask");
    }

    /// <summary>One line per item. Team: "Name — Role". FAQ: "Question? Answer".</summary>
    [HttpPost("/pitches/package/story")]
    public async Task<IActionResult> Story(string? highlights, string? team, string? faq, CancellationToken ct)
    {
        static IEnumerable<string> Lines(string? s) => (s ?? "").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0);
        var t = Lines(team).Select(l => { var parts = l.Split(['—', '–'], 2); return parts.Length == 2 ? new TeamDto(parts[0].Trim(), parts[1].Trim()) : new TeamDto(l, "Team"); }).ToList();
        var f = Lines(faq).Select(l => { var i = l.IndexOf('?'); return i > 0 ? new FaqDto(l[..(i + 1)].Trim(), l[(i + 1)..].Trim()) : new FaqDto(l, ""); }).ToList();
        Flash(await package.SaveStoryAsync(Actor, Lines(highlights).ToList(), t, f, ct));
        return Redirect("/pitches#story");
    }
}
