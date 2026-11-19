using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RiseUp.DataAccess.Repositories;
using RiseUp.DataAccess.Seed;
using RiseUp.DataAccess.Services;
using RiseUp.Entities.Abstractions;
using RiseUp.Framework.Time;

namespace RiseUp.DataAccess;

public static class DependencyInjection
{
    /// <summary>
    /// Registers EF Core (SQL Server), repositories, Unit of Work and infrastructure adapters.
    /// DbContext registration lives HERE, not in the Facade (fixes the legacy AddCrmFacade coupling).
    /// </summary>
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration,
        bool isProduction, Action<DbContextOptionsBuilder>? overrideDatabase = null)
    {
        services.AddDbContext<RiseUpDbContext>(options =>
        {
            if (overrideDatabase is not null) { overrideDatabase(options); return; }
            if (configuration["Database:Provider"] == "SqliteDemo")
            {
                // DEVELOPMENT-ONLY convenience so the site runs without SQL Server. Refused in Production.
                if (isProduction) throw new InvalidOperationException("The SqliteDemo database provider cannot be used in Production.");
                options.UseSqlite(configuration.GetConnectionString("RiseUpDemo") ?? "Data Source=riseup-demo.db");
                return;
            }
            options.UseSqlServer(configuration.GetConnectionString("RiseUp")
                ?? throw new InvalidOperationException("ConnectionStrings:RiseUp is not configured."),
                sql => sql.CommandTimeout(30));
        });

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IStartupRepository, StartupRepository>();
        services.AddScoped<IInvestorRepository, InvestorRepository>();
        services.AddScoped<IConnectionRepository, ConnectionRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<IPromotionRepository, PromotionRepository>();
        services.AddScoped<ITeamAccountRepository, TeamAccountRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IVerificationRepository, VerificationRepository>();
        services.AddScoped<IGovernanceRepository, GovernanceRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IPitchRepository, PitchRepository>();
        services.AddScoped<IShortlistRepository, ShortlistRepository>();
        services.AddScoped<IMeetingRepository, MeetingRepository>();
        services.AddScoped<ISavedPostRepository, SavedPostRepository>();

        services.Configure<PaymentOptions>(configuration.GetSection("Payments"));
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        services.AddSingleton<RiseUp.Entities.Abstractions.IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<LocalPrivateFileStorage>();
        services.AddSingleton<IFileStorage>(sp => sp.GetRequiredService<LocalPrivateFileStorage>());
        services.AddSingleton<IVerificationUploadStorage>(sp => sp.GetRequiredService<LocalPrivateFileStorage>());
        if (!isProduction && configuration.GetValue("Scanning:DemoSignatures", false) && string.IsNullOrWhiteSpace(configuration["Scanning:Host"]))
            services.AddSingleton<IMalwareScanner, SignatureMalwareScanner>();
        else
            services.AddSingleton<IMalwareScanner>(new ClamAvScanner(configuration["Scanning:Host"], configuration.GetValue("Scanning:Port", 3310)));
        services.Configure<MfaOptions>(configuration.GetSection("Mfa"));
        if (isProduction) services.PostConfigure<MfaOptions>(options => options.AllowDemoCodes = false);
        services.AddSingleton<IMfaVerifier, TotpMfaVerifier>();

        if (configuration.GetValue("Email:Enabled", false))
        {
            var smtp = configuration.GetSection("Email:Smtp").Get<SmtpOptions>() ?? new();
            if (string.IsNullOrWhiteSpace(smtp.Host) || smtp.Port is < 1 or > 65535 ||
                !System.Net.Mail.MailAddress.TryCreate(smtp.From, out _) ||
                (string.IsNullOrEmpty(smtp.Username) != string.IsNullOrEmpty(smtp.Password)))
                throw new InvalidOperationException("Configure Email:Smtp host, port, sender and matching SMTP credentials.");
            services.AddSingleton<IEmailSender>(new SmtpEmailSender(smtp));
        }

        var gateway = configuration["Payments:Gateway"] ?? "Disabled";
        if (gateway == "Sandbox")
        {
            if (isProduction) throw new InvalidOperationException("The sandbox payment gateway cannot be enabled in Production.");
            services.AddSingleton<IPaymentGateway, SandboxPaymentGateway>();
        }
        else if (gateway == "Disabled")
        {
            services.AddSingleton<IPaymentGateway, DisabledPaymentGateway>();
        }
        else if (gateway == "SSLCOMMERZ")
        {
            var options = configuration.GetSection("Payments:SslCommerz").Get<SslCommerzOptions>() ?? new();
            options.PublicOrigin = configuration["Site:PublicOrigin"] ?? "";
            options.Validate(isProduction);
            services.AddSingleton<IPaymentGateway>(_ => new SslCommerzGateway(options, new HttpClient(
                new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) }));
        }
        else
        {
            throw new InvalidOperationException($"Payment gateway '{gateway}' is not implemented. Complete merchant onboarding and add its adapter.");
        }

        services.Configure<SeedOptions>(configuration.GetSection("Seed"));
        services.AddScoped<DemoDataSeeder>();
        return services;
    }
}
