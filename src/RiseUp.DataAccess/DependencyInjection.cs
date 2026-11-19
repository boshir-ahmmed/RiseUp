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
                sql => { sql.CommandTimeout(30); sql.EnableRetryOnFailure(3); });
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

        services.Configure<AiOptions>(configuration.GetSection("Ai"));
        services.Configure<PaymentOptions>(configuration.GetSection("Payments"));
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        services.AddSingleton<RiseUp.Entities.Abstractions.IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<IContentSafetyModel, SandboxContentSafetyModel>();
        services.AddSingleton<IAiMatchingService, SandboxAiMatchingService>();
        services.AddSingleton<IFileStorage, LocalPrivateFileStorage>();
        services.AddSingleton<IMalwareScanner, SignatureMalwareScanner>();
        services.Configure<MfaOptions>(configuration.GetSection("Mfa"));
        services.AddSingleton<IMfaVerifier, TotpMfaVerifier>();

        var gateway = configuration["Payments:Gateway"] ?? "Sandbox";
        if (gateway == "Sandbox")
        {
            if (isProduction) throw new InvalidOperationException("The sandbox payment gateway cannot be enabled in Production.");
            services.AddSingleton<IPaymentGateway, SandboxPaymentGateway>();
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
