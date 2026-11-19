using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Startups;

public sealed class UpdateStartupRequestValidator : AbstractValidator<UpdateStartupRequest>
{
    public UpdateStartupRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Tagline).NotEmpty().MaximumLength(BusinessRules.TaglineMax);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(BusinessRules.DescriptionMax);
        RuleFor(x => x.Industry).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(80);
        RuleFor(x => x.FoundingYear).InclusiveBetween(1950, 2100);
        RuleFor(x => x.FundingGoalUsd).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Website).MaximumLength(200);
    }
}
