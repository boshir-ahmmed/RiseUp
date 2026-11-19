using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Startups;

public sealed record UpdateStartupRequest(string Name, string Tagline, string Description, string Industry, FundingStage Stage, string Location,
    int FoundingYear, string? BusinessModel, string? Website, string? Tags, decimal FundingGoalUsd);
