using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Identity;

public sealed record RegisterRequest(PrimaryRole Role, string FullName, string Email, string Phone, string Password,
    bool AcceptedTerms, string? StartupName = null, string? Affiliation = null);
