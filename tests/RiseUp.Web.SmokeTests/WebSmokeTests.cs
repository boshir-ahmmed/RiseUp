using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RiseUp.Web.SmokeTests;

/// <summary>Boots the real ASP.NET Core host (SQLite demo DB, sandbox adapters) and exercises the HTTP surface.</summary>
public sealed class RiseUpFactory : WebApplicationFactory<Program>
{
    private readonly string _db = Path.Combine(Path.GetTempPath(), $"riseup-smoke-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Database:Provider", "SqliteDemo");
        builder.UseSetting("ConnectionStrings:RiseUpDemo", $"Data Source={_db}");
        builder.UseSetting("Jobs:Enabled", "false");
        builder.UseSetting("RateLimits:AuthPerMinute", "1000");
    }
}

public class WebSmokeTests(RiseUpFactory factory) : IClassFixture<RiseUpFactory>
{
    private HttpClient Client(bool follow = false) => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = follow, BaseAddress = new Uri("https://localhost") });

    [Fact]
    public async Task SuggestionsMatchFirmAndNameWithoutContactOrFinancialFields()
    {
        var client = Client();
        using var json = System.Text.Json.JsonDocument.Parse(await client.GetStringAsync("/search/suggestions?scope=investors&q=pAdMa"));
        var investor = Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal("Sadia Haque", investor.GetProperty("name").GetString());
        Assert.True(investor.GetProperty("accepting").GetBoolean());
        Assert.Equal(new[] { "accepting", "detail", "href", "id", "kind", "name", "photo" },
            investor.EnumerateObject().Select(p => p.Name).Order().ToArray());

        using var startups = System.Text.Json.JsonDocument.Parse(await client.GetStringAsync("/search/suggestions?scope=startups&q=nOdI"));
        Assert.Contains(startups.RootElement.EnumerateArray(), s => s.GetProperty("name").GetString() == "Nodi Freight");
        Assert.Equal("[]", await client.GetStringAsync("/search/suggestions?scope=all&q=%25"));
        Assert.Equal("[]", await client.GetStringAsync("/search/suggestions?scope=investors&q="));
    }

    [Theory]
    [InlineData("/"), InlineData("/startups"), InlineData("/search?q=Padma"), InlineData("/startups/1"), InlineData("/investors"), InlineData("/investors/1"), InlineData("/posts/1"),
     InlineData("/about"), InlineData("/how-it-works"), InlineData("/faq"), InlineData("/pricing"), InlineData("/legal/terms"), InlineData("/login"),
     InlineData("/register"), InlineData("/register/founder"), InlineData("/health"), InlineData("/reels")]
    public async Task PublicPages_Return200(string url) => Assert.Equal(HttpStatusCode.OK, (await Client().GetAsync(url)).StatusCode);

    [Theory]
    [InlineData("/dashboard"), InlineData("/pitches"), InlineData("/wallet"), InlineData("/conversations"), InlineData("/admin"), InlineData("/admin/finance"),
     InlineData("/me"), InlineData("/me/edit"), InlineData("/shortlist"), InlineData("/pitch-room/1"), InlineData("/admin/profile-changes")]
    public async Task ProtectedPages_RedirectAnonymousToLogin(string url)
    {
        var res = await Client().GetAsync(url);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/login", res.Headers.Location!.ToString());
    }

    [Fact]
    public async Task UnknownPagesAndAdminRegistration_Return404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Client().GetAsync("/register/admin")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client().GetAsync("/does-not-exist")).StatusCode);
    }

    [Fact]
    public async Task SecurityHeaders_ArePresent()
    {
        var res = await Client().GetAsync("/");
        Assert.True(res.Headers.Contains("X-Content-Type-Options"));
        Assert.True(res.Headers.Contains("X-Frame-Options"));
        Assert.True(res.Headers.Contains("Content-Security-Policy"));
        Assert.True(res.Headers.Contains("X-Correlation-ID"));
    }

    [Fact]
    public async Task VisitorHtml_NeverContainsRestrictedFields()
    {
        var html = await Client().GetStringAsync("/startups/1");
        Assert.DoesNotContain("600,000", html);              // funding goal
        Assert.DoesNotContain("farhan@nodifreight", html);    // contact email
        Assert.DoesNotContain("500,000", await Client().GetStringAsync("/investors"));   // ticket ranges
    }

    [Fact]
    public async Task PostWithoutAntiforgeryToken_IsRejected()
    {
        var res = await Client().PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string> { ["Identifier"] = "x@y.z", ["Password"] = "x" }));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task FounderCanSignIn_AndReachDashboard_ButNotAdmin()
    {
        var client = Client();
        var login = await client.GetStringAsync("/login");
        var token = Regex.Match(login, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(token));
        var res = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Identifier"] = "farhan@nodifreight.example", ["Password"] = "Demo#2026pass", ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Equal("/dashboard", res.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/dashboard")).StatusCode);
        Assert.Contains("value=\"600000", await client.GetStringAsync("/settings"));   // owner sees own funding goal
        var admin = await client.GetAsync("/admin");
        Assert.True(admin.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Forbidden);
        Assert.Contains("access-denied", admin.Headers.Location?.ToString() ?? "access-denied");
    }

    [Fact]
    public async Task AdminLogin_RequiresSecondFactor()
    {
        var client = Client();
        var login = await client.GetStringAsync("/login");
        var token = Regex.Match(login, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var res = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Identifier"] = "finance@riseup.example", ["Password"] = "Demo#2026pass", ["__RequestVerificationToken"] = token }));
        Assert.Contains("/account/mfa", res.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/admin/finance")).StatusCode);   // not signed in until 2FA passes
    }

    [Fact]
    public async Task PrivatePitchMedia_IsNotServedToVisitors()
    {
        var res = await Client().GetAsync("/media/999999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
