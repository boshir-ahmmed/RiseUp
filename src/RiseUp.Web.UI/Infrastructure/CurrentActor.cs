using System.Security.Claims;
using RiseUp.Entities.Common;
using RiseUp.Facade.Common;
using RiseUp.Facade.Identity;
using RiseUp.Framework.Constants;

namespace RiseUp.Web.UI.Infrastructure;

/// <summary>Builds the Facade <see cref="Actor"/> from server-issued claims and issues claims at sign-in.</summary>
public static class CurrentActor
{
    public static Actor From(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true) return Actor.Anonymous;
        static int? Int(ClaimsPrincipal u, string type) => int.TryParse(u.FindFirstValue(type), out var v) ? v : null;
        var role = user.FindFirstValue(ClaimTypes.Role) switch
        {
            Roles.Founder => PrimaryRole.Founder,
            Roles.Investor => PrimaryRole.Investor,
            Roles.TeamMember => PrimaryRole.TeamMember,
            var r when Roles.Admins.Contains(r) => PrimaryRole.Admin,
            _ => (PrimaryRole?)null,
        };
        var admin = user.FindFirstValue(ClaimTypes.Role) switch
        {
            Roles.SuperAdmin => AdminRole.SuperAdmin,
            Roles.VerificationAdmin => AdminRole.VerificationAdmin,
            Roles.Moderator => AdminRole.Moderator,
            Roles.FinanceAdmin => AdminRole.FinanceAdmin,
            _ => AdminRole.None,
        };
        return new Actor(Int(user, ClaimNames.AccountId), user.Identity.Name ?? "", role, admin, Int(user, ClaimNames.StartupId),
            Int(user, ClaimNames.InvestorId), Int(user, ClaimNames.TeamAccountId), user.HasClaim(ClaimNames.MfaSatisfied, "true"));
    }

    public static ClaimsPrincipal ToPrincipal(SignedInUser u, bool mfaSatisfied, string scheme)
    {
        var roleName = u.Role switch
        {
            PrimaryRole.Founder => Roles.Founder,
            PrimaryRole.Investor => Roles.Investor,
            PrimaryRole.TeamMember => Roles.TeamMember,
            _ => u.AdminRole switch
            {
                AdminRole.SuperAdmin => Roles.SuperAdmin, AdminRole.VerificationAdmin => Roles.VerificationAdmin,
                AdminRole.Moderator => Roles.Moderator, AdminRole.FinanceAdmin => Roles.FinanceAdmin, _ => "none",
            },
        };
        var claims = new List<Claim> { new(ClaimTypes.Name, u.Name), new(ClaimTypes.Role, roleName), new(ClaimNames.MfaSatisfied, mfaSatisfied ? "true" : "false") };
        if (u.AccountId is int a) claims.Add(new(ClaimNames.AccountId, a.ToString()));
        if (u.TeamAccountId is int t)
        {
            claims.Add(new(ClaimNames.TeamAccountId, t.ToString()));
            claims.Add(new(ClaimNames.TeamSessionVersion, u.TeamSessionVersion.ToString()));
        }
        if (u.StartupId is int s) claims.Add(new(ClaimNames.StartupId, s.ToString()));
        if (u.InvestorId is int i) claims.Add(new(ClaimNames.InvestorId, i.ToString()));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, scheme));
    }
}
