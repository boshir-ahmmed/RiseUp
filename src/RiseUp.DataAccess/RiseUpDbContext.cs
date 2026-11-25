using Microsoft.EntityFrameworkCore;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Governance;
using RiseUp.Entities.Identity;
using RiseUp.Entities.Investors;
using RiseUp.Entities.Media;
using RiseUp.Entities.Pitching;
using RiseUp.Entities.Profiles;
using RiseUp.Entities.Payments;
using RiseUp.Entities.Promotions;
using RiseUp.Entities.Social;
using RiseUp.Entities.Startups;
using RiseUp.Entities.Verification;
using RiseUp.Entities.Wallets;
using RiseUp.Framework.Time;

namespace RiseUp.DataAccess;

public class RiseUpDbContext(DbContextOptions<RiseUpDbContext> options, IClock clock) : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<AccountToken> AccountTokens => Set<AccountToken>();
    public DbSet<Startup> Startups => Set<Startup>();
    public DbSet<StartupMetric> StartupMetrics => Set<StartupMetric>();
    public DbSet<ProfileViewEvent> ProfileViewEvents => Set<ProfileViewEvent>();
    public DbSet<TeamAccount> TeamAccounts => Set<TeamAccount>();
    public DbSet<PrivateDocument> PrivateDocuments => Set<PrivateDocument>();
    public DbSet<DocumentAccessGrant> DocumentAccessGrants => Set<DocumentAccessGrant>();
    public DbSet<InvestorProfile> InvestorProfiles => Set<InvestorProfile>();
    public DbSet<InvestorClaim> InvestorClaims => Set<InvestorClaim>();
    public DbSet<VerificationApplication> VerificationApplications => Set<VerificationApplication>();
    public DbSet<VerificationDocument> VerificationDocuments => Set<VerificationDocument>();
    public DbSet<OwnershipTransferCase> OwnershipTransferCases => Set<OwnershipTransferCase>();
    public DbSet<ConnectionRequest> ConnectionRequests => Set<ConnectionRequest>();
    public DbSet<StartupInvestorConnection> Connections => Set<StartupInvestorConnection>();
    public DbSet<BlockRelationship> Blocks => Set<BlockRelationship>();
    public DbSet<InvitationDailyCounter> InvitationDailyCounters => Set<InvitationDailyCounter>();
    public DbSet<PitchDraft> PitchDrafts => Set<PitchDraft>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<StartupPost> Posts => Set<StartupPost>();
    public DbSet<PostReaction> PostReactions => Set<PostReaction>();
    public DbSet<PostComment> PostComments => Set<PostComment>();
    public DbSet<StartupWallet> Wallets => Set<StartupWallet>();
    public DbSet<TokenLedgerEntry> TokenLedger => Set<TokenLedgerEntry>();
    public DbSet<TokenPackage> TokenPackages => Set<TokenPackage>();
    public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();
    public DbSet<PaymentProviderEvent> PaymentProviderEvents => Set<PaymentProviderEvent>();
    public DbSet<PromotionPackage> PromotionPackages => Set<PromotionPackage>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<UserReport> Reports => Set<UserReport>();
    public DbSet<AdminActionAudit> AdminAudit => Set<AdminActionAudit>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OutboxEvent> Outbox => Set<OutboxEvent>();
    // v3/v4
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<MemberProfile> MemberProfiles => Set<MemberProfile>();
    public DbSet<NameChangeRequest> NameChangeRequests => Set<NameChangeRequest>();
    public DbSet<PitchPackage> PitchPackages => Set<PitchPackage>();
    public DbSet<PitchSlide> PitchSlides => Set<PitchSlide>();
    public DbSet<UseOfFundsItem> UseOfFunds => Set<UseOfFundsItem>();
    public DbSet<PitchTeamMember> PitchTeamMembers => Set<PitchTeamMember>();
    public DbSet<PitchFaq> PitchFaqs => Set<PitchFaq>();
    public DbSet<PitchDocument> PitchDocuments => Set<PitchDocument>();
    public DbSet<PitchInfoItem> PitchInfoItems => Set<PitchInfoItem>();
    public DbSet<PitchQuestion> PitchQuestions => Set<PitchQuestion>();
    public DbSet<PitchEngagement> PitchEngagements => Set<PitchEngagement>();
    public DbSet<ShortlistEntry> Shortlist => Set<ShortlistEntry>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<SavedPost> SavedPosts => Set<SavedPost>();

    // The development SQLite store uses EnsureCreated, while SQL Server uses versioned migrations.
    public async Task InitializeDemoDatabaseAsync()
    {
        await Database.EnsureCreatedAsync();
        await using var transaction = await Database.BeginTransactionAsync();
        var columns = await Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('VerificationDocuments')").ToListAsync();
        if (!columns.Contains("Retention"))
            await Database.ExecuteSqlRawAsync("ALTER TABLE VerificationDocuments ADD COLUMN Retention INTEGER NOT NULL DEFAULT 0");
        var profileColumns = await Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('MemberProfiles')").ToListAsync();
        if (!profileColumns.Contains("LogoMediaId"))
            await Database.ExecuteSqlRawAsync("ALTER TABLE MemberProfiles ADD COLUMN LogoMediaId INTEGER NULL");
        var teamColumns = await Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('TeamAccounts')").ToListAsync();
        if (!teamColumns.Contains("GroupMode"))
            await Database.ExecuteSqlRawAsync("ALTER TABLE TeamAccounts ADD COLUMN GroupMode INTEGER NOT NULL DEFAULT 0");
        if (!teamColumns.Contains("SessionVersion"))
            await Database.ExecuteSqlRawAsync("ALTER TABLE TeamAccounts ADD COLUMN SessionVersion INTEGER NOT NULL DEFAULT 0");
        var hasContacts = await Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = 'AccountContacts'").SingleAsync();
        if (hasContacts == 0)
            foreach (var sql in Migrations.AccountContactSchemaV1.Install(sqlite: true))
                await Database.ExecuteSqlRawAsync(sql);
        var hasInfoItems = await Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = 'PitchInfoItems'").SingleAsync();
        if (hasInfoItems == 0)
        {
            await Database.ExecuteSqlRawAsync("""
                CREATE TABLE PitchInfoItems (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    RequestId INTEGER NOT NULL REFERENCES ConnectionRequests(Id) ON DELETE CASCADE,
                    Prompt TEXT NOT NULL, ReplyText TEXT NULL, AnsweredAtUtc TEXT NULL,
                    ReplyMediaId INTEGER NULL REFERENCES MediaAssets(Id) ON DELETE RESTRICT,
                    CreatedAtUtc TEXT NOT NULL, UpdatedAtUtc TEXT NULL);
                CREATE INDEX IX_PitchInfoItems_RequestId ON PitchInfoItems(RequestId);
                CREATE UNIQUE INDEX IX_PitchInfoItems_ReplyMediaId ON PitchInfoItems(ReplyMediaId);
                """);
            await Database.ExecuteSqlRawAsync(Migrations.PitchInformationSchemaV1.ImportRequests);
        }
        await transaction.CommitAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RiseUpDbContext).Assembly);

        var isSqlite = Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
        foreach (var type in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IConcurrencyAware).IsAssignableFrom(type.ClrType))
                modelBuilder.Entity(type.ClrType).Property(nameof(IConcurrencyAware.Version)).IsConcurrencyToken();
            // SQLite (demo/tests only) stores decimal as TEXT, which breaks numeric CHECK constraints and ordering.
            if (isSqlite)
                foreach (var p in type.GetProperties().Where(p => p.ClrType == typeof(decimal)))
                    modelBuilder.Entity(type.ClrType).Property(p.Name).HasConversion<double>();
        }
    }

    /// <summary>Stamps audit times and rotates concurrency tokens; the Unit of Work is the only caller.</summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAtUtc == default) entry.Entity.CreatedAtUtc = now;
            if (entry.State == EntityState.Modified) entry.Entity.UpdatedAtUtc = now;
        }
        foreach (var entry in ChangeTracker.Entries<IConcurrencyAware>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified) entry.Entity.Version = Guid.NewGuid();
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
