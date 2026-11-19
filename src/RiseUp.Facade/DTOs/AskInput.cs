using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Pitching;

public sealed record AskInput(decimal AmountUsd, FundingInstrument Instrument, decimal ValuationUsd, int DiscountPercent, int RunwayMonths, decimal MinTicketUsd, IReadOnlyList<FundsItem> UseOfFunds);
