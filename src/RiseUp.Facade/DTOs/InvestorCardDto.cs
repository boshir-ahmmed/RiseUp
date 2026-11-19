using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Investors;

public sealed record InvestorCardDto(int Id, string Name, string Affiliation, string Type, IReadOnlyList<string> Industries, IReadOnlyList<FundingStage> Stages,
    string Location, string Bio, bool ReceivePitches, decimal? TicketMinUsd, decimal? TicketMaxUsd, int ColorIndex, string? PhotoUrl = null, string? CoverUrl = null, string? Headline = null);
