using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RiseUp.DataAccess;
using RiseUp.DataAccess.Seed;
using RiseUp.Facade;
using RiseUp.Framework.Constants;
using RiseUp.Web.UI.Hubs;
using RiseUp.Web.UI.Infrastructure;
using RiseUp.Web.UI.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAntiforgery(o => { o.HeaderName = "X-CSRF-TOKEN"; o.Cookie.Name = "RiseUp.Csrf"; o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; });

// Layering: Web.UI -> Facade -> Entities abstractions <- DataAccess. DataAccess is referenced ONLY here, to register it.
builder.Services.AddDataAccess(builder.Configuration, builder.Environment.IsProduction());
builder.Services.AddFacade();
builder.Services.PostConfigure<RiseUp.DataAccess.Seed.SeedOptions>(o => o.DemoMediaPath = Path.Combine(builder.Environment.ContentRootPath, o.DemoMediaPath));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/" + UrlRewrite.Account.Login;
        o.AccessDeniedPath = "/" + UrlRewrite.Account.AccessDenied;
        o.Cookie.Name = "RiseUp.Auth";
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(Policies.Founder, p => p.RequireRole(Roles.Founder));
    o.AddPolicy(Policies.Investor, p => p.RequireRole(Roles.Investor));
    o.AddPolicy(Policies.FounderOrInvestor, p => p.RequireRole(Roles.Founder, Roles.Investor));
    o.AddPolicy(Policies.ChatParticipant, p => p.RequireRole(Roles.Founder, Roles.Investor, Roles.TeamMember));
    // Every admin policy also requires a completed second factor (FR-009, SR-09).
    o.AddPolicy(Policies.AnyAdmin, p => p.RequireRole(Roles.Admins).RequireClaim(ClaimNames.MfaSatisfied, "true"));
    o.AddPolicy(Policies.SuperAdmin, p => p.RequireRole(Roles.SuperAdmin).RequireClaim(ClaimNames.MfaSatisfied, "true"));
    o.AddPolicy(Policies.VerificationAdmin, p => p.RequireRole(Roles.VerificationAdmin, Roles.SuperAdmin).RequireClaim(ClaimNames.MfaSatisfied, "true"));
    o.AddPolicy(Policies.Moderator, p => p.RequireRole(Roles.Moderator).RequireClaim(ClaimNames.MfaSatisfied, "true"));
    o.AddPolicy(Policies.FinanceAdmin, p => p.RequireRole(Roles.FinanceAdmin).RequireClaim(ClaimNames.MfaSatisfied, "true"));
});

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = builder.Configuration.GetValue("RateLimits:AuthPerMinute", 20), Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy("requests", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.User.Identity?.Name ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = builder.Configuration.GetValue("RateLimits:RequestsPerMinute", 30), Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddSignalR();
builder.Services.AddHealthChecks().AddDbContextCheck<RiseUpDbContext>("database");
if (builder.Configuration.GetValue("Jobs:Enabled", true)) builder.Services.AddHostedService<ScheduledJobs>();

var app = builder.Build();

if (app.Configuration.GetValue("Database:InitializeOnStartup", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RiseUpDbContext>();
    if (db.Database.IsSqlServer()) await db.Database.MigrateAsync(); else await db.Database.EnsureCreatedAsync();
    if (!app.Environment.IsProduction()) await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHub<ChatHub>("/hubs/chat");
app.MapControllerRoute("areas", "{area:exists}/{controller=Overview}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();

/// <summary>Exposed for WebApplicationFactory smoke tests.</summary>
public partial class Program;
