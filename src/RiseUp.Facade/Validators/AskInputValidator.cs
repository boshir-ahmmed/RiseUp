using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Pitching;

public sealed class AskInputValidator : AbstractValidator<AskInput>
{
    public AskInputValidator()
    {
        RuleFor(x => x.AmountUsd).GreaterThan(0).WithMessage("Enter the amount you are raising.").LessThanOrEqualTo(1_000_000_000);
        RuleFor(x => x.Instrument).IsInEnum();
        RuleFor(x => x.ValuationUsd).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 50);
        RuleFor(x => x.RunwayMonths).InclusiveBetween(1, 120);
        RuleFor(x => x.MinTicketUsd).GreaterThanOrEqualTo(0).LessThanOrEqualTo(x => x.AmountUsd).WithMessage("Minimum ticket can't exceed the amount raised.");
        RuleFor(x => x.UseOfFunds).NotEmpty().WithMessage("Add at least one use of funds.")
            .Must(l => l.Count <= BusinessRules.UseOfFundsMax).WithMessage($"Up to {BusinessRules.UseOfFundsMax} use-of-funds lines.")
            .Must(l => l.Sum(i => i.Percent) == 100).WithMessage(l => $"Use of funds must total 100% (currently {l.UseOfFunds.Sum(i => i.Percent)}%).");
        RuleForEach(x => x.UseOfFunds).ChildRules(i =>
        {
            i.RuleFor(x => x.Label).NotEmpty().MaximumLength(40);
            i.RuleFor(x => x.Percent).InclusiveBetween(1, 100);
        });
    }
}
