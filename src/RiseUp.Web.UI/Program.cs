using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RiseUp.AI;
using RiseUp.DataAccess;
using RiseUp.DataAccess.Seed;
using RiseUp.Facade;
using RiseUp.Framework.Constants;
using RiseUp.Web.UI.Hubs;
using RiseUp.Web.UI.Infrastructure;
using RiseUp.Web.UI.Jobs;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    var config = builder.Configuration;
    var hasSqlConnection = !string.IsNullOrWhiteSpace(config.GetConnectionString("RiseUp"));
    if (config["Database:Provider"] == "Auto")
        config["Database:Provider"] = hasSqlConnection ? "SqlServer" : "SqliteDemo";
    if (!hasSqlConnection && config["Database:Provider"] == "SqlServer")
        config["ConnectionStrings:RiseUp"] = DesignTimeDbContextFactory.LocalDevelopmentConnection;
}

builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAntiforgery(o => { o.HeaderName = "X-CSRF-TOKEN"; o.Cookie.Name = "RiseUp.Csrf"; o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; });

// Compose the infrastructure adapters and Facade at the MVC entry point.
builder.Services.AddDataAccess(builder.Configuration, builder.Environment.IsProduction());
builder.Services.AddAi(builder.Configuration);
builder.Services.AddFacade();
builder.Services.PostConfigure<RiseUp.DataAccess.Seed.SeedOptions>(o => o.DemoMediaPath = Path.Combine(builder.Environment.ContentRootPath, o.DemoMediaPath));

builder.Services.PostConfigure<RiseUp.DataAccess.Services.StorageOptions>(o =>
    o.RootPath = Path.GetFullPath(o.RootPath, builder.Environment.ContentRootPath));

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

var emailEnabled = builder.Configuration.GetValue("Email:Enabled", false);
var registrationOpen = builder.Configuration.GetValue("Registration:Enabled", builder.Environment.IsDevelopment());
if (!builder.Environment.IsDevelopment() && registrationOpen && !emailEnabled)
    throw new InvalidOperationException("Enable email delivery before opening public registration.");
if (emailEnabled && (!Uri.TryCreate(builder.Configuration["Site:PublicOrigin"], UriKind.Absolute, out var origin) ||
    origin.Scheme != "https" || origin.UserInfo.Length != 0 || origin.PathAndQuery != "/" || origin.Fragment.Length != 0))
    throw new InvalidOperationException("Set Site:PublicOrigin to the public HTTPS origin for email links.");
if (emailEnabled || builder.Configuration["Payments:Gateway"] == "SSLCOMMERZ")
    builder.Services.AddHostedService<CommerceJobs>();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks().AddDbContextCheck<RiseUpDbContext>("database");
if (builder.Configuration.GetValue("Jobs:Enabled", true)) builder.Services.AddHostedService<ScheduledJobs>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var diagnosticScope = app.Services.CreateScope();
    var context = diagnosticScope.ServiceProvider.GetRequiredService<RiseUpDbContext>();
    var target = context.Database.GetDbConnection();
    if (context.Database.IsSqlServer())
        app.Logger.LogInformation("RiseUp registration database: SQL Server {Server}, database {Database}", target.DataSource, target.Database);
    else
        app.Logger.LogInformation("RiseUp registration database: SQLite {File}", Path.GetFullPath(target.DataSource));
}

if (app.Configuration.GetValue("Database:InitializeOnStartup", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RiseUpDbContext>();
    if (db.Database.IsSqlServer()) await db.Database.MigrateAsync(); else await db.InitializeDemoDatabaseAsync();
    if (app.Environment.IsDevelopment()) await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync();
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
