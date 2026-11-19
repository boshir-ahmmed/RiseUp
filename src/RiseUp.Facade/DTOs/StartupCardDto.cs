using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Startups;

public sealed record StartupCardDto(int Id, string Name, string Tagline, string Description, string Industry, FundingStage Stage, string Location,
    VerificationStatus Status, int Views, int ColorIndex, string? LogoUrl = null);
