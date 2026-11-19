using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Pitching;

public sealed record AskDto(decimal AmountUsd, FundingInstrument Instrument, decimal ValuationUsd, int DiscountPercent, int RunwayMonths, decimal MinTicketUsd, IReadOnlyList<FundsItem> UseOfFunds)
{
    public string InstrumentLabel => Instrument switch { FundingInstrument.Equity => "Equity (priced round)", FundingInstrument.Safe => "SAFE", FundingInstrument.ConvertibleNote => "Convertible note", _ => "Revenue share" };
    public string Terms => Instrument switch
    {
        FundingInstrument.Equity => ValuationUsd > 0 ? $"${ValuationUsd:N0} pre-money" : "valuation on request",
        FundingInstrument.RevenueShare => "revenue share",
        _ => $"{DiscountPercent}% discount{(ValuationUsd > 0 ? $" · ${ValuationUsd:N0} cap" : " · uncapped")}",
    };
}
