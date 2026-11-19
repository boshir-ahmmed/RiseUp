using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Pitching;

public sealed record SpotlightDto(int StartupId, string Name, string Tagline, string Industry, FundingStage Stage, string Location, int ColorIndex, string? LogoUrl,
    VideoDto Teaser, VideoDto? Demo, IReadOnlyList<string> Highlights, AskDto? Ask, IReadOnlyList<(string Name, string Value)>? Metrics,
    int? PendingPitchRequestId, bool InvitationPending, bool Connected, bool Shortlisted, bool ViewerIsVerifiedInvestor);
