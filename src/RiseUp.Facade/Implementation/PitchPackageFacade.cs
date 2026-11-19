using FluentValidation;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Media;
using RiseUp.Entities.Pitching;
using RiseUp.Entities.Startups;
using RiseUp.Facade.Common;
using RiseUp.Facade.Discovery;
using RiseUp.Facade.Media;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Pitching;


/// <summary>
/// Pitch Studio. Every change bumps the package version. Videos/decks/documents are validated, malware-scanned and
/// never sent to AI. The founder's written note on each pitch remains the only text checked by Model 1.
/// </summary>
public sealed class PitchPackageFacade(IPitchRepository pitches, IStartupRepository startups, IInvestorRepository investors, IConnectionRepository connections,
    IShortlistRepository shortlist, IMediaRepository media, MediaIntake intake, IUnitOfWork uow, IClock clock, IValidator<AskInput> askValidator) : IPitchPackageFacade
{
    private async Task<(PitchPackage? Package, Startup? Startup, Error? Error)> OwnAsync(Actor actor, bool create, CancellationToken ct)
    {
        if (!actor.IsFounder || actor.StartupId is null) return (null, null, Guard.Forbidden("Only the Main Startup Owner can edit the pitch package."));
        var s = await startups.GetByIdAsync(actor.StartupId.Value, ct);
        if (s is null || s.IsDeleted) return (null, null, Guard.NotFound("Startup"));
        if (s.IsSuspended) return (null, s, new Error(ErrorCodes.AccountSuspended, "Your startup account is suspended."));
        var p = await pitches.GetPackageAsync(s.Id, ct);
        if (p is null && create)
        {
            p = new PitchPackage { StartupId = s.Id, AskAmountUsd = s.FundingGoalUsd, RunwayMonths = 12, UseOfFunds = [new UseOfFundsItem { Label = "Product & growth", Percent = 100 }] };
            await pitches.AddPackageAsync(p, ct);
        }
        return (p, s, null);
    }

    private static void Touch(PitchPackage p) => p.PackageVersion++;

    public async Task<Result<PitchPackageDto>> GetOwnAsync(Actor actor, CancellationToken ct = default)
    {
        var (p, s, e) = await OwnAsync(actor, true, ct);
        if (e is not null) return e;
        if (p!.Id == 0) await uow.SaveChangesAsync(ct);
        return Result.Success(await ToDtoAsync(p, s!, null, ct));
    }

    internal async Task<PitchPackageDto> ToDtoAsync(PitchPackage p, Startup s, IReadOnlySet<int>? onlyDocuments, CancellationToken ct)
    {
        var ids = new int?[] { p.PitchVideoId, p.DemoVideoId, p.TeaserVideoId, p.PitchPosterId, p.DemoPosterId, p.TeaserPosterId, p.DeckMediaId }
            .Concat(p.Documents.Select(d => (int?)d.MediaId)).Where(i => i is not null).Select(i => i!.Value).Distinct().ToList();
        var assets = (await media.GetManyAsync(ids, ct)).ToDictionary(a => a.Id);
        VideoDto? Video(int? id, int? poster) => id is int v && assets.TryGetValue(v, out var a)
            ? new VideoDto(v, UrlRewrite.Media(v), poster is int pp && assets.ContainsKey(pp) ? UrlRewrite.Media(pp) : null, a.DurationSeconds ?? 0, a.ScanStatus) : null;
        var deck = p.DeckMediaId is int dk && assets.TryGetValue(dk, out var da) ? new FileDto(dk, da.FileName, da.SizeBytes, da.ScanStatus, UrlRewrite.Media(dk)) : null;
        var docs = p.Documents.Where(d => assets.ContainsKey(d.MediaId) && (onlyDocuments is null || onlyDocuments.Contains(d.Id))).OrderBy(d => d.Id)
            .Select(d => { var a = assets[d.MediaId]; return new PitchDocumentDto(d.Id, d.MediaId, a.FileName, d.Label, a.SizeBytes, a.ScanStatus, d.AllowDownload, UrlRewrite.Media(d.MediaId)); }).ToList();
        var ask = new AskDto(p.AskAmountUsd, p.Instrument, p.ValuationUsd, p.DiscountPercent, p.RunwayMonths, p.MinTicketUsd, p.UseOfFunds.OrderBy(u => u.Id).Select(u => new FundsItem(u.Label, u.Percent)).ToList());
        var slides = p.Slides.OrderBy(x => x.Position).Select(x => new SlideDto(x.Title, x.Body)).ToList();
        var checklist = new List<(string, string, bool)>
        {
            ("A 30-second hook", "Pitch video: open with the problem in one sentence and who has it.", p.PitchVideoId is not null),
            ("Show the product, don't describe it", "Product demo: a screen recording or real footage beats slides.", p.DemoVideoId is not null),
            ("A public teaser", "60 seconds, no funding figures — shown in Reels when you enable it.", p.TeaserVideoId is not null),
            ("A deck investors can skim", "At least 5 slides: problem, solution, market, traction, ask.", slides.Count >= 5),
            ("Numbers investors can verify", "Two or more highlights with time periods.", p.HighlightLines.Count() >= 2),
            ("A clear ask", "Amount, instrument, valuation or cap, use of funds, runway.", p.AskAmountUsd > 0 && ask.UseOfFunds.Sum(u => u.Percent) == 100),
            ("Why you", "Team members with relevant experience.", p.Team.Count >= 1),
            ("Anticipate questions", "Three or more FAQs: competition, regulation, unit economics, exit.", p.Faq.Count >= 3),
            ("Keep documents light", "A one-pager or financial model; the data room comes after connection.", docs.Count >= 1),
        };
        return new PitchPackageDto(s.Id, s.Name, p.PackageVersion, Video(p.PitchVideoId, p.PitchPosterId), Video(p.DemoVideoId, p.DemoPosterId), Video(p.TeaserVideoId, p.TeaserPosterId),
            p.PublicTeaser, p.PublicDemo, p.CaptionsAvailable, deck, slides, docs, ask, p.HighlightLines.ToList(),
            p.Team.OrderBy(t => t.Id).Select(t => new TeamDto(t.Name, t.Role)).ToList(), p.Faq.OrderBy(f => f.Id).Select(f => new FaqDto(f.Question, f.Answer)).ToList(),
            (int)Math.Round(checklist.Count(c => c.Item3) * 100.0 / checklist.Count), checklist);
    }

    // ------------------------------------------------------------------ videos
    public async Task<Result> SetVideoAsync(Actor actor, PitchVideoSlot slot, UploadedFile video, UploadedFile? poster, CancellationToken ct = default)
    {
        var (p, s, e) = await OwnAsync(actor, true, ct);
        if (e is not null) return Result.Failure(e);
        var kind = slot switch { PitchVideoSlot.Pitch => MediaKind.PitchVideo, PitchVideoSlot.Demo => MediaKind.DemoVideo, _ => MediaKind.TeaserVideo };
        var accepted = await intake.AcceptAsync(kind, video, s!.Id, actor.AccountId, null, ct);
        if (accepted.IsFailure) return accepted;
        Result<MediaAsset>? posterAccepted = poster is null ? null : await intake.AcceptAsync(MediaKind.VideoPoster, poster, s.Id, actor.AccountId, null, ct);
        await uow.SaveChangesAsync(ct);
        var now = clock.UtcNow;
        int? newPoster = posterAccepted is { IsSuccess: true } ? posterAccepted.Value!.Id : null;
        switch (slot)
        {
            case PitchVideoSlot.Pitch: await intake.DiscardAsync(p!.PitchVideoId, now, ct); await intake.DiscardAsync(p.PitchPosterId, now, ct); p.PitchVideoId = accepted.Value!.Id; p.PitchPosterId = newPoster; break;
            case PitchVideoSlot.Demo: await intake.DiscardAsync(p!.DemoVideoId, now, ct); await intake.DiscardAsync(p.DemoPosterId, now, ct); p.DemoVideoId = accepted.Value!.Id; p.DemoPosterId = newPoster; break;
            default: await intake.DiscardAsync(p!.TeaserVideoId, now, ct); await intake.DiscardAsync(p.TeaserPosterId, now, ct); p.TeaserVideoId = accepted.Value!.Id; p.TeaserPosterId = newPoster; break;
        }
        Touch(p);
        await uow.SaveChangesAsync(ct);
        return Result.Success(accepted.Value!.ScanStatus == ScanStatus.Clean ? $"{slot} video uploaded and scanned." : $"{slot} video uploaded. It becomes visible to investors once the security scan finishes.");
    }

    public async Task<Result> RemoveVideoAsync(Actor actor, PitchVideoSlot slot, CancellationToken ct = default)
    {
        var (p, _, e) = await OwnAsync(actor, false, ct);
        if (e is not null) return Result.Failure(e);
        if (p is null) return Result.Success("Nothing to remove.");
        var now = clock.UtcNow;
        switch (slot)
        {
            case PitchVideoSlot.Pitch: await intake.DiscardAsync(p.PitchVideoId, now, ct); await intake.DiscardAsync(p.PitchPosterId, now, ct); p.PitchVideoId = p.PitchPosterId = null; break;
            case PitchVideoSlot.Demo: await intake.DiscardAsync(p.DemoVideoId, now, ct); await intake.DiscardAsync(p.DemoPosterId, now, ct); p.DemoVideoId = p.DemoPosterId = null; p.PublicDemo = false; break;
            default: await intake.DiscardAsync(p.TeaserVideoId, now, ct); await intake.DiscardAsync(p.TeaserPosterId, now, ct); p.TeaserVideoId = p.TeaserPosterId = null; p.PublicTeaser = false; break;
        }
        Touch(p);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Video removed.");
    }

    public async Task<Result> SetVisibilityAsync(Actor actor, bool publicTeaser, bool publicDemo, bool captions, CancellationToken ct = default)
    {
        var (p, s, e) = await OwnAsync(actor, true, ct);
        if (e is not null) return Result.Failure(e);
        if (publicTeaser && p!.TeaserVideoId is null) return Result.Failure(ErrorCodes.Validation, "Upload a teaser before making it public.");
        if (publicDemo && p!.DemoVideoId is null) return Result.Failure(ErrorCodes.Validation, "Upload a product demo before making it public.");
        if ((publicTeaser || publicDemo) && !s!.IsActiveVerified) return Result.Failure(ErrorCodes.NotVerified, "Public videos are available once your startup is verified.");
        p!.PublicTeaser = publicTeaser; p.PublicDemo = publicDemo; p.CaptionsAvailable = captions;
        await uow.SaveChangesAsync(ct);
        return Result.Success(publicTeaser ? "Saved. Your teaser is public in Reels." : "Saved.");
    }

    // ------------------------------------------------------------------ deck + slides
    public async Task<Result> SetDeckAsync(Actor actor, UploadedFile deck, CancellationToken ct = default)
    {
        var (p, s, e) = await OwnAsync(actor, true, ct);
        if (e is not null) return Result.Failure(e);
        var accepted = await intake.AcceptAsync(MediaKind.PitchDeck, deck, s!.Id, actor.AccountId, null, ct);
        if (accepted.IsFailure) return accepted;
        await uow.SaveChangesAsync(ct);
        await intake.DiscardAsync(p!.DeckMediaId, clock.UtcNow, ct);
        p.DeckMediaId = accepted.Value!.Id;
        if (p.Slides.Count == 0)
            p.Slides.AddRange(new[] { ("{0}", s.Tagline), ("The problem", ""), ("Our solution", ""), ("Market", ""), ("Traction", ""), ("Business model", s.BusinessModel ?? ""), ("Team", ""), ("The ask", "") }
                .Select((x, i) => new PitchSlide { Position = i, Title = string.Format(x.Item1, s.Name), Body = x.Item2 }));
        Touch(p);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Deck uploaded. Investors read the slides in the watermarked in-app viewer; download stays off.");
    }

    public async Task<Result> RemoveDeckAsync(Actor actor, CancellationToken ct = default)
    {
        var (p, _, e) = await OwnAsync(actor, false, ct);
        if (e is not null) return Result.Failure(e);
        if (p?.DeckMediaId is null) return Result.Success("No deck file.");
        await intake.DiscardAsync(p.DeckMediaId, clock.UtcNow, ct);
        p.DeckMediaId = null; Touch(p);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Deck file removed. Your slide text is kept.");
    }

    public async Task<Result> SaveSlidesAsync(Actor actor, IReadOnlyList<SlideDto> slides, CancellationToken ct = default)
    {
        var (p, _, e) = await OwnAsync(actor, true, ct);
        if (e is not null) return Result.Failure(e);
        var clean = slides.Where(x => !string.IsNullOrWhiteSpace(x.Title)).Select(x => new SlideDto(x.Title.Trim(), (x.Body ?? "").Trim())).ToList();
        if (clean.Count > BusinessRules.DeckSlidesMax) return Result.Failure(ErrorCodes.Validation, $"Up to {BusinessRules.DeckSlidesMax} slides.");
        if (clean.Any(x => x.Title.Length > 80 || x.Body.Length > 240)) return Result.Failure(ErrorCodes.Validation, "Slide titles are limited to 80 characters and slide text to 240.");
        p!.Slides.Clear();
        p.Slides.AddRange(clean.Select((x, i) => new PitchSlide { Position = i, Title = x.Title, Body = x.Body }));
        Touch(p);
        await uow.SaveChangesAsync(ct);
        return Result.Success($"{clean.Count} slides saved.");
    }

    // ------------------------------------------------------------------ documents
    public async Task<Result> AddDocumentAsync(Actor actor, UploadedFile file, string label, CancellationToken ct = default)
    {
        var (p, s, e) = await OwnAsync(actor, true, ct);
        if (e is not null) return Result.Failure(e);
        if (p!.Documents.Count >= BusinessRules.PitchDocumentsMax) return Result.Failure(ErrorCodes.LimitReached, $"Up to {BusinessRules.PitchDocumentsMax} pitch documents. Remove one first.");
        label = string.IsNullOrWhiteSpace(label) ? "Document" : label.Trim();
        if (label.Length > 60) return Result.Failure(ErrorCodes.Validation, "Document labels are limited to 60 characters.");
        var accepted = await intake.AcceptAsync(MediaKind.PitchDocument, file, s!.Id, actor.AccountId, null, ct);
        if (accepted.IsFailure) return accepted;
        await uow.SaveChangesAsync(ct);
        p.Documents.Add(new PitchDocument { MediaId = accepted.Value!.Id, Label = label, AllowDownload = false });
        Touch(p);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Document added. It is view-only until you allow download.");
    }

    public async Task<Result> RemoveDocumentAsync(Actor actor, int documentId, CancellationToken ct = default)
    {
        var (p, _, e) = await OwnAsync(actor, false, ct);
        if (e is not null) return Result.Failure(e);
        var d = p?.Documents.FirstOrDefault(x => x.Id == documentId);
        if (d is null) return Result.Failure(Guard.NotFound("Document"));
        p!.Documents.Remove(d);
        await intake.DiscardAsync(d.MediaId, clock.UtcNow, ct);
        Touch(p);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Document removed. Investors who already received it lose access.");
    }

    public async Task<Result> SetDocumentDownloadAsync(Actor actor, int documentId, bool allow, CancellationToken ct = default)
    {
        var (p, _, e) = await OwnAsync(actor, false, ct);
        if (e is not null) return Result.Failure(e);
        var d = p?.Documents.FirstOrDefault(x => x.Id == documentId);
        if (d is null) return Result.Failure(Guard.NotFound("Document"));
        d.AllowDownload = allow;
        await uow.SaveChangesAsync(ct);
        return Result.Success(allow ? "Investors may download this file. Downloads are logged." : "View-only. Investors read it in the in-app viewer.");
    }

    // ------------------------------------------------------------------ ask + story
    public async Task<Result> SaveAskAsync(Actor actor, AskInput ask, CancellationToken ct = default)
    {
        if (await Guard.ValidateAsync(askValidator, ask, ct) is { } invalid) return Result.Failure(invalid);
        var (p, _, e) = await OwnAsync(actor, true, ct);
        if (e is not null) return Result.Failure(e);
        p!.AskAmountUsd = ask.AmountUsd; p.Instrument = ask.Instrument; p.ValuationUsd = ask.ValuationUsd; p.DiscountPercent = ask.DiscountPercent;
        p.RunwayMonths = ask.RunwayMonths; p.MinTicketUsd = ask.MinTicketUsd;
        p.UseOfFunds.Clear();
        p.UseOfFunds.AddRange(ask.UseOfFunds.Select(u => new UseOfFundsItem { Label = u.Label.Trim(), Percent = u.Percent }));
        Touch(p);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Ask saved. Pitches already sent show that the package was updated.");
    }

    public async Task<Result> SaveStoryAsync(Actor actor, IReadOnlyList<string> highlights, IReadOnlyList<TeamDto> team, IReadOnlyList<FaqDto> faq, CancellationToken ct = default)
    {
        var h = highlights.Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        var t = team.Where(x => !string.IsNullOrWhiteSpace(x.Name)).Select(x => new TeamDto(x.Name.Trim(), string.IsNullOrWhiteSpace(x.Role) ? "Team" : x.Role.Trim())).ToList();
        var f = faq.Where(x => !string.IsNullOrWhiteSpace(x.Question)).Select(x => new FaqDto(x.Question.Trim(), (x.Answer ?? "").Trim())).ToList();
        if (h.Count > BusinessRules.HighlightsMax || h.Any(x => x.Length > 200)) return Result.Failure(ErrorCodes.Validation, $"Up to {BusinessRules.HighlightsMax} highlights, 200 characters each.");
        if (t.Count > BusinessRules.PitchTeamMax || t.Any(x => x.Name.Length > 80 || x.Role.Length > 120)) return Result.Failure(ErrorCodes.Validation, $"Up to {BusinessRules.PitchTeamMax} team members.");
        if (f.Count > BusinessRules.PitchFaqMax || f.Any(x => x.Question.Length > 200 || x.Answer.Length > 600)) return Result.Failure(ErrorCodes.Validation, $"Up to {BusinessRules.PitchFaqMax} FAQs.");
        var (p, _, e) = await OwnAsync(actor, true, ct);
        if (e is not null) return Result.Failure(e);
        p!.Highlights = string.Join('\n', h);
        p.Team.Clear(); p.Team.AddRange(t.Select(x => new PitchTeamMember { Name = x.Name, Role = x.Role }));
        p.Faq.Clear(); p.Faq.AddRange(f.Select(x => new PitchFaq { Question = x.Question, Answer = x.Answer }));
        Touch(p);
        await uow.SaveChangesAsync(ct);
        return Result.Success("Saved.");
    }

    // ------------------------------------------------------------------ spotlight / reels
    public async Task<IReadOnlyList<SpotlightDto>> ListSpotlightAsync(Actor actor, int take, CancellationToken ct = default)
    {
        var verified = await startups.ListActiveVerifiedAsync(200, ct);
        var packages = (await pitches.ListPackagesAsync(verified.Select(s => s.Id).ToList(), ct)).Where(p => p.PublicTeaser && p.TeaserVideoId is not null).ToDictionary(p => p.StartupId);
        var pool = verified.Where(s => packages.ContainsKey(s.Id) && !(actor.IsFounder && actor.StartupId == s.Id)).ToList();
        var me = actor.IsInvestor ? await investors.GetByIdAsync(actor.InvestorId!.Value, ct) : null;
        var verifiedInvestor = me is { IsActiveVerified: true };
        IEnumerable<Startup> ordered = me is not null
            ? RuleBasedMatcher.Rank(MatchingFacade.ToProfile(me, true), pool.Select(MatchingFacade.ToProfile).ToList()).Select(m => pool.First(s => s.Id == m.CandidateId))
            : pool.OrderByDescending(s => s.ViewCount);
        var chosen = ordered.Take(Math.Clamp(take, 1, 30)).ToList();
        var mediaIds = chosen.SelectMany(s => new int?[] { packages[s.Id].TeaserVideoId, packages[s.Id].TeaserPosterId, packages[s.Id].DemoVideoId, packages[s.Id].DemoPosterId })
            .Where(x => x is not null).Select(x => x!.Value).ToList();
        var assets = (await media.GetManyAsync(mediaIds, ct)).Where(a => a.ScanStatus == ScanStatus.Clean).ToDictionary(a => a.Id);
        var result = new List<SpotlightDto>();
        foreach (var s in chosen)
        {
            var p = packages[s.Id];
            if (!assets.TryGetValue(p.TeaserVideoId!.Value, out var teaser)) continue;
            var teaserDto = new VideoDto(teaser.Id, UrlRewrite.Media(teaser.Id), p.TeaserPosterId is int tp && assets.ContainsKey(tp) ? UrlRewrite.Media(tp) : null, teaser.DurationSeconds ?? 0, teaser.ScanStatus);
            VideoDto? demo = p.DemoVideoId is int dv && assets.TryGetValue(dv, out var da) && (p.PublicDemo || verifiedInvestor)
                ? new VideoDto(dv, UrlRewrite.Media(dv), p.DemoPosterId is int dp && assets.ContainsKey(dp) ? UrlRewrite.Media(dp) : null, da.DurationSeconds ?? 0, da.ScanStatus) : null;
            // A private demo is only streamable inside a pitch room, so the spotlight links it only when it is public.
            if (demo is not null && !p.PublicDemo) demo = null;
            int? pendingPitch = null; var invitationPending = false; var connected = false; var shortlisted = false;
            IReadOnlyList<(string, string)>? metrics = null;
            if (me is not null)
            {
                var pending = await connections.GetPendingForPairAsync(s.Id, me.Id, ct);
                pendingPitch = pending?.Type == RequestType.Pitch ? pending.Id : null;
                invitationPending = pending?.Type == RequestType.Invitation;
                connected = await connections.GetConnectionAsync(s.Id, me.Id, ct) is not null;
                shortlisted = await shortlist.GetAsync(me.Id, s.Id, ct) is not null;
                if (verifiedInvestor) metrics = (await startups.GetWithMetricsAsync(s.Id, ct))?.Metrics.Take(3).Select(m => (m.Name, m.Value)).ToList();
            }
            var ask = verifiedInvestor ? new AskDto(p.AskAmountUsd, p.Instrument, p.ValuationUsd, p.DiscountPercent, p.RunwayMonths, p.MinTicketUsd, []) : null;
            result.Add(new SpotlightDto(s.Id, s.Name, s.Tagline, s.Industry, s.Stage, s.Location, s.ColorIndex, s.LogoMediaId is null ? null : UrlRewrite.Media(s.LogoMediaId),
                teaserDto, demo, p.HighlightLines.Take(2).ToList(), ask, metrics, pendingPitch, invitationPending, connected, shortlisted, verifiedInvestor));
        }
        return result;
    }
}
