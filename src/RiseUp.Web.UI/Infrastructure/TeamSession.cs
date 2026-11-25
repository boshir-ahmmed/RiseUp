using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using RiseUp.Facade.Identity;
using RiseUp.Framework.Constants;

namespace RiseUp.Web.UI.Infrastructure;

internal static class TeamSession
{
    public static async Task ValidateCookieAsync(CookieValidatePrincipalContext context)
    {
        if (context.Principal?.IsInRole(Roles.TeamMember) != true) return;
        var accounts = context.HttpContext.RequestServices.GetRequiredService<IAccountFacade>();
        if (await IsCurrentAsync(context.Principal, accounts, context.HttpContext.RequestAborted)) return;
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(context.Scheme.Name);
    }

    public static Task<bool> IsCurrentAsync(ClaimsPrincipal user, IAccountFacade accounts, CancellationToken ct)
    {
        if (!user.IsInRole(Roles.TeamMember)) return Task.FromResult(true);
        // Team cookies issued before this migration correspond to version zero.
        if (!int.TryParse(user.FindFirstValue(ClaimNames.TeamAccountId), out var id) ||
            !int.TryParse(user.FindFirstValue(ClaimNames.TeamSessionVersion) ?? "0", out var version))
            return Task.FromResult(false);
        return accounts.IsTeamSessionCurrentAsync(id, version, ct);
    }
}
