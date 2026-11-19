using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Pitching;

public sealed record ShortlistItemDto(int StartupId, string Name, string Industry, FundingStage Stage, int ColorIndex, string? LogoUrl, decimal? AskUsd,
    ShortlistStage Stage2, string Note, int? PendingPitchRequestId, bool Connected, bool InvitationPending);
