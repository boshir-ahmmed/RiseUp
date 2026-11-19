using FluentValidation;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Startups;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.TeamAccounts;

public sealed class CreateTeamAccountRequestValidator : AbstractValidator<CreateTeamAccountRequest>
{
    public CreateTeamAccountRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Designation).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Username).NotEmpty().Matches("^[a-z0-9._]{3,30}$").WithMessage("Username: 3–30 lowercase letters, digits, dots or underscores.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.LinkedInUrl).MaximumLength(200);
    }
}
