using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Startups;

public sealed record SearchStartupsRequest(string? Text, string? Industry, FundingStage? Stage, string? Location, string Sort = "newest", int Page = 1, int PageSize = 12);
