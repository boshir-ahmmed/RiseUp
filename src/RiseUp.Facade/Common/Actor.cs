using RiseUp.Entities.Common;

namespace RiseUp.Facade.Common;

/// <summary>
/// The authenticated caller, built by Web.UI from server-issued claims. Every use case receives it and
/// performs role AND object-level authorization here, never only in controllers or views (NFR-15).
/// </summary>
public sealed record Actor(
    int? AccountId,
    string Name,
    PrimaryRole? Role,
    AdminRole AdminRole = AdminRole.None,
    int? StartupId = null,
    int? InvestorId = null,
    int? TeamAccountId = null,
    bool MfaSatisfied = false)
{
    public static readonly Actor Anonymous = new(null, "Visitor", null);
    public static readonly Actor System = new(null, "system", PrimaryRole.Admin, AdminRole.SuperAdmin, MfaSatisfied: true);

    public bool IsAuthenticated => AccountId is not null || TeamAccountId is not null;
    public bool IsFounder => Role == PrimaryRole.Founder && AccountId is not null;
    public bool IsInvestor => Role == PrimaryRole.Investor && InvestorId is not null;
    public bool IsTeamMember => Role == PrimaryRole.TeamMember && TeamAccountId is not null;
    public bool IsAdmin(AdminRole role) => Role == PrimaryRole.Admin && MfaSatisfied && AdminRole == role;
    public bool IsAnyAdmin => Role == PrimaryRole.Admin && MfaSatisfied && AdminRole != AdminRole.None;
    public string ViewerKey => AccountId is not null ? $"a:{AccountId}" : TeamAccountId is not null ? $"t:{TeamAccountId}" : "anonymous";
}
