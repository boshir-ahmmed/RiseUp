using Microsoft.AspNetCore.Mvc;
using RiseUp.Facade.Common;
using RiseUp.Facade.Profiles;
using RiseUp.Web.UI.Infrastructure;

namespace RiseUp.Web.UI.Components;

public sealed record NavItem(string Group, string Href, string Label, int Badge = 0);
public sealed record TopItem(string Href, string Label, string Icon, int Badge = 0);
public sealed record ShellModel(Actor Actor, ShellDto Shell, string Path, IReadOnlyList<TopItem> Top, IReadOnlyList<NavItem> Nav)
{
    public bool IsOn(string href) => href == "/" ? Path == "/" : href == "/admin" ? Path == "/admin"
        : Path.StartsWith("/pitch-room", StringComparison.OrdinalIgnoreCase) ? href == (Actor.IsInvestor ? "/requests" : "/pitches")
        : Path.StartsWith(href, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Signed-in shell: LinkedIn-style top bar (Home on every page) and the left sidebar (profile card + navigation).
/// The shell data is loaded once per request and shared by both parts.
/// </summary>
public sealed class ShellViewComponent(IProfileFacade profiles) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string part)
    {
        var actor = CurrentActor.From(HttpContext.User);
        if (HttpContext.Items["shell"] is not ShellDto shell)
        {
            shell = await profiles.GetShellAsync(actor, HttpContext.RequestAborted);
            HttpContext.Items["shell"] = shell;
        }
        var path = HttpContext.Request.Path.Value ?? "/";
        return View(part, new ShellModel(actor, shell, path, TopItems(actor, shell), NavItems(actor, shell)));
    }

    private static List<TopItem> TopItems(Actor a, ShellDto s)
    {
        var list = new List<TopItem> { new("/", "Home", "home") };
        if (a.IsFounder) list.AddRange([new("/discover", "Investors", "compass"), new("/pitches", "Pitch Studio", "pitch"), new("/requests", "Requests", "inbox", s.PendingBadge),
            new("/conversations", "Messages", "msg"), new("/notifications", "Alerts", "bell", s.UnreadNotifications)]);
        else if (a.IsInvestor) list.AddRange([new("/discover", "Startups", "compass"), new("/requests", "Pitches", "pitch", s.PendingBadge), new("/shortlist", "Shortlist", "star"),
            new("/conversations", "Messages", "msg"), new("/notifications", "Alerts", "bell", s.UnreadNotifications)]);
        else if (a.IsTeamMember) list.AddRange([new("/conversations", "Messages", "msg"), new("/notifications", "Alerts", "bell", s.UnreadNotifications)]);
        else if (a.IsAnyAdmin) list.Add(new("/admin", "Admin", "shield"));
        return list;
    }

    private static List<NavItem> NavItems(Actor a, ShellDto s)
    {
        var n = new List<NavItem>();
        if (a.IsFounder)
            n.AddRange([new("Workspace", "/", "Home feed"), new("Workspace", "/dashboard", "Dashboard"), new("Workspace", "/discover", "Discover investors"), new("Workspace", "/pitches", "Pitch Studio"),
                new("Workspace", "/requests", "Requests", s.PendingBadge), new("Workspace", "/conversations", "Conversations"),
                new("Startup", "/my-posts", "Post & Reels"), new("Startup", "/team", "Team accounts"), new("Startup", "/library", "Confidential library"), new("Startup", "/settings", "Startup profile"), new("Startup", "/verification", "Verification"),
                new("Account", "/me", "My profile"), new("Account", "/me/edit", "Edit profile"), new("Account", "/wallet", "Wallet"), new("Account", "/notifications", "Notifications", s.UnreadNotifications)]);
        else if (a.IsInvestor)
            n.AddRange([new("Workspace", "/", "Home feed"), new("Workspace", "/dashboard", "Dashboard"), new("Workspace", "/discover", "Discover startups"), new("Workspace", "/requests", "Pitches & invitations", s.PendingBadge),
                new("Workspace", "/shortlist", "Shortlist"), new("Workspace", "/conversations", "Conversations"), new("Workspace", "/library", "Shared documents"),
                new("Account", "/me", "My profile"), new("Account", "/me/edit", "Edit profile"), new("Account", "/settings", "Preferences"), new("Account", "/verification", "Verification"), new("Account", "/notifications", "Notifications", s.UnreadNotifications)]);
        else if (a.IsTeamMember)
            n.AddRange([new("Workspace", "/", "Home feed"), new("Workspace", "/conversations", "Conversations"), new("Workspace", "/notifications", "Notifications", s.UnreadNotifications),
                new("Account", "/me", "My profile"), new("Account", "/me/edit", "Edit profile photo")]);
        else if (a.IsAnyAdmin)
        {
            n.Add(new("Administration", "/admin", "Overview"));
            if (a.AdminRole is Entities.Common.AdminRole.VerificationAdmin or Entities.Common.AdminRole.SuperAdmin)
            { n.Add(new("Administration", "/admin/verification", "Verification queue")); n.Add(new("Administration", "/admin/profile-changes", "Profile name changes")); }
            if (a.AdminRole == Entities.Common.AdminRole.Moderator) n.Add(new("Administration", "/admin/reports", "Reports"));
            if (a.AdminRole == Entities.Common.AdminRole.FinanceAdmin) n.Add(new("Administration", "/admin/finance", "Payments & ledger"));
            if (a.AdminRole == Entities.Common.AdminRole.SuperAdmin) n.Add(new("Administration", "/admin/audit", "Audit log"));
            n.AddRange([new("Account", "/", "Home feed"), new("Account", "/me", "My profile"), new("Account", "/me/edit", "Edit profile")]);
        }
        return n;
    }
}
