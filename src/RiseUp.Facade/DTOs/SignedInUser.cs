using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Identity;


/// <summary>Claims source for the cookie. Roles come from the database, never hard-coded (legacy "manager" bug).</summary>
public sealed record SignedInUser(int? AccountId, int? TeamAccountId, string Name, PrimaryRole Role, AdminRole AdminRole,
    int? StartupId, int? InvestorId, bool MfaRequired, int TeamSessionVersion = 0);
