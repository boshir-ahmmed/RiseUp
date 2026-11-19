using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Pitching;

public sealed record PitchRoomDto(int? RequestId, bool IsPreview, bool ViewerIsInvestor, bool ViewerIsFounder, int StartupId, string StartupName, string Tagline,
    string Industry, FundingStage Stage, string Location, int ColorIndex, string? LogoUrl, bool StartupVerified, bool StartupSuspended,
    string? InvestorName, DateTime? SentAtUtc, DateTime? ExpiresAtUtc, RequestStatus? Status, int? SentVersion, int CurrentVersion, string? Note,
    DateTime? InfoRequestedAtUtc, string? InfoRequest, PitchPackageDto Package, IReadOnlyList<RoomQuestionDto> Questions, bool CanAsk, int QuestionsLeft,
    bool CanDecide, bool CanAnswer, RoomStatsDto? Stats, bool Shortlisted, int? ConversationId);
