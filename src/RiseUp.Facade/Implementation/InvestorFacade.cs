using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Investors;

public sealed class InvestorFacade(IInvestorRepository investors, IStartupRepository startups, IUserAccountRepository accounts,
    IConnectionRepository connections, IUnitOfWork uow, IProfileRepository profiles) : IInvestorFacade
{
    /// <summary>FR-039/040: ticket ranges only for verified startups (or the investor themself).</summary>
    private async Task<bool> CanSeeRangeAsync(Actor actor, int investorId, CancellationToken ct)
    {
        if (actor.IsInvestor && actor.InvestorId == investorId) return true;
        if (!actor.IsFounder || actor.StartupId is null) return false;
        return (await startups.GetByIdAsync(actor.StartupId.Value, ct))?.IsActiveVerified == true;
    }

    public async Task<IReadOnlyList<InvestorCardDto>> ListAsync(Actor actor, CancellationToken ct = default)
    {
        var list = await investors.ListActiveVerifiedAsync(200, ct);
        var verifiedStartup = actor.IsFounder && actor.StartupId is int sid && (await startups.GetByIdAsync(sid, ct))?.IsActiveVerified == true;
        var photos = await profiles.PhotoIdsForAccountsAsync(list.Select(i => i.AccountId).ToList(), ct);
        return list.Select(i => ToCard(i, verifiedStartup || (actor.IsInvestor && actor.InvestorId == i.Id)) with { PhotoUrl = photos.TryGetValue(i.AccountId, out var ph) ? UrlRewrite.Media(ph) : null }).ToList();
    }

    public async Task<Result<InvestorDetailDto>> GetDetailAsync(Actor actor, int id, CancellationToken ct = default)
    {
        var i = await investors.GetWithClaimsAsync(id, ct);
        if (i is null || !i.IsActiveVerified) return Guard.NotFound("Investor");
        var self = actor.IsInvestor && actor.InvestorId == id;
        var connected = actor.IsFounder && actor.StartupId is int sid && await connections.GetConnectionAsync(sid, id, ct) is not null;
        var account = connected || self ? await accounts.GetByIdAsync(i.AccountId, ct) : null;
        var canPitch = actor.IsFounder && actor.StartupId is int s2 && !connected && i.ReceivePitches && (await startups.GetByIdAsync(s2, ct))?.IsActiveVerified == true
                       && await connections.GetPendingForPairAsync(s2, id, ct) is null;
        var prof = await profiles.GetForAccountAsync(i.AccountId, ct);
        var card = ToCard(i, await CanSeeRangeAsync(actor, id, ct)) with { PhotoUrl = prof?.PhotoMediaId is null ? null : UrlRewrite.Media(prof.PhotoMediaId), CoverUrl = prof?.CoverMediaId is null ? null : UrlRewrite.Media(prof.CoverMediaId), Headline = string.IsNullOrWhiteSpace(prof?.Headline) ? null : prof!.Headline };
        return Result.Success(new InvestorDetailDto(card,
            i.Claims.Select(c => (c.StartupName, c.Year, c.Round, c.IsConfirmed)).ToList(),
            account?.Email, account?.Phone, self, connected, canPitch));
    }

    /// <summary>FR-068/069: OFF blocks new pitches only; pending pitches and chats are unaffected.</summary>
    public async Task<Result> SetReceivePitchesAsync(Actor actor, bool receive, CancellationToken ct = default)
    {
        if (!actor.IsInvestor) return Result.Failure(Guard.Forbidden());
        var i = await investors.GetByIdAsync(actor.InvestorId!.Value, ct);
        if (i is null) return Result.Failure(Guard.NotFound("Investor"));
        i.ReceivePitches = receive;
        await uow.SaveChangesAsync(ct);
        return Result.Success($"Receive Pitches turned {(receive ? "ON" : "OFF")}. Pending pitches and chats are unaffected.");
    }

    private static InvestorCardDto ToCard(Entities.Investors.InvestorProfile i, bool showRange) =>
        new(i.Id, i.DisplayName, i.Affiliation, i.InvestorType, i.Industries.ToList(), i.Stages.ToList(), i.Location, i.Bio, i.ReceivePitches,
            showRange ? i.TicketMinUsd : null, showRange ? i.TicketMaxUsd : null, i.ColorIndex);
}
