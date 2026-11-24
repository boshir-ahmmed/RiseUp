using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Identity;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Role).Must(r => r is PrimaryRole.Founder or PrimaryRole.Investor).WithMessage("Choose Founder or Investor.");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Password).Must(PasswordPolicy.IsValid).WithMessage(PasswordPolicy.Requirements);
        RuleFor(x => x.AcceptedTerms).Equal(true).WithMessage($"You must accept the Terms v{BusinessRules.TermsVersion} and Privacy Policy v{BusinessRules.PrivacyVersion}.");
        RuleFor(x => x.StartupName).NotEmpty().MaximumLength(100).When(x => x.Role == PrimaryRole.Founder);
        RuleFor(x => x.Phone).Matches(@"^\s*\+?[0-9০-৯ ().\-]*[0-9০-৯][0-9০-৯ ().\-]*\s*$")
            .WithMessage("Enter a contact number using digits and an optional country code.");
    }
}
