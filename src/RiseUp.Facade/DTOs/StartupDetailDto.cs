using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Startups;


/// <summary>Restricted fields are null unless the caller may see them — they are never rendered and hidden with CSS.</summary>
public sealed record StartupDetailDto(int Id, string Name, string Tagline, string Description, string Industry, FundingStage Stage, string Location,
    int FoundingYear, string? BusinessModel, string? Website, IReadOnlyList<string> Tags, VerificationStatus Status, DateTime? VerifiedUntilUtc, int Views, int ColorIndex,
    decimal? FundingGoalUsd, IReadOnlyList<(string Name, string Value)>? Metrics, string? OwnerName, string? ContactEmail, string? ContactPhone,
    bool ViewerIsOwner, bool ViewerIsConnected, bool ViewerCanInvite, string? LogoUrl = null, string? CoverUrl = null,
    string? OwnerPhotoUrl = null, string? OwnerLogoUrl = null);
