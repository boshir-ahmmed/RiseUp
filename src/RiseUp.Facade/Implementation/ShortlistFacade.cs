using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Pitching;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Pitching;


/// <summary>Investor-private deal pipeline. Founders are never told they were shortlisted.</summary>
public sealed class ShortlistFacade(IShortlistRepository shortlist, IStartupRepository startups, IConnectionRepository connections, IPitchRepository pitches, IUnitOfWork uow) : IShortlistFacade
{
    public async Task<IReadOnlyList<ShortlistItemDto>> ListAsync(Actor actor, CancellationToken ct = default)
    {
        if (!actor.IsInvestor) return [];
        var entries = await shortlist.ListAsync(actor.InvestorId!.Value, ct);
        var ss = (await startups.GetManyAsync(entries.Select(e => e.StartupId).ToList(), ct)).Where(s => !s.IsDeleted && !s.IsSuspended).ToDictionary(s => s.Id);
        var packs = (await pitches.ListPackagesAsync(ss.Keys.ToList(), ct)).ToDictionary(p => p.StartupId);
        var result = new List<ShortlistItemDto>();
        foreach (var e in entries.Where(e => ss.ContainsKey(e.StartupId)))
        {
            var s = ss[e.StartupId];
            var pending = await connections.GetPendingForPairAsync(s.Id, actor.InvestorId!.Value, ct);
            result.Add(new ShortlistItemDto(s.Id, s.Name, s.Industry, s.Stage, s.ColorIndex, s.LogoMediaId is null ? null : UrlRewrite.Media(s.LogoMediaId),
                packs.TryGetValue(s.Id, out var p) && p.AskAmountUsd > 0 ? p.AskAmountUsd : s.FundingGoalUsd, e.Stage, e.Note,
                pending?.Type == RequestType.Pitch ? pending.Id : null, await connections.GetConnectionAsync(s.Id, actor.InvestorId!.Value, ct) is not null,
                pending?.Type == RequestType.Invitation));
        }
        return result;
    }

    public async Task<Result<bool>> ToggleAsync(Actor actor, int startupId, CancellationToken ct = default)
    {
        if (!actor.IsInvestor) return Guard.Forbidden("Only investors have a shortlist.");
        var s = await startups.GetByIdAsync(startupId, ct);
        if (s is null || s.IsDeleted || s.IsSuspended) return Guard.NotFound("Startup");
        var existing = await shortlist.GetAsync(actor.InvestorId!.Value, startupId, ct);
        if (existing is not null) { shortlist.Remove(existing); await uow.SaveChangesAsync(ct); return Result.Success(false, "Removed from your shortlist."); }
        var stage = await connections.GetConnectionAsync(startupId, actor.InvestorId.Value, ct) is not null ? ShortlistStage.InConversation
            : (await connections.GetPendingForPairAsync(startupId, actor.InvestorId.Value, ct))?.Type == RequestType.Pitch ? ShortlistStage.PitchReceived : ShortlistStage.Watching;
        await shortlist.AddAsync(new ShortlistEntry { InvestorId = actor.InvestorId.Value, StartupId = startupId, Stage = stage }, ct);
        try { await uow.SaveChangesAsync(ct); } catch (PersistenceConflictException) { return Result.Success(true); }
        return Result.Success(true, "Added to your private shortlist.");
    }

    public async Task<Result> UpdateAsync(Actor actor, int startupId, ShortlistStage stage, string? note, CancellationToken ct = default)
    {
        if (!actor.IsInvestor) return Result.Failure(Guard.Forbidden());
        if (!Enum.IsDefined(stage)) return Result.Failure(ErrorCodes.Validation, "Unknown stage.");
        note = (note ?? "").Trim();
        if (note.Length > BusinessRules.ShortlistNoteMax) return Result.Failure(ErrorCodes.Validation, $"Notes are limited to {BusinessRules.ShortlistNoteMax} characters.");
        var e = await shortlist.GetAsync(actor.InvestorId!.Value, startupId, ct);
        if (e is null) return Result.Failure(Guard.NotFound("Shortlist entry"));
        e.Stage = stage; e.Note = note;
        await uow.SaveChangesAsync(ct);
        return Result.Success("Saved (private).");
    }
}
