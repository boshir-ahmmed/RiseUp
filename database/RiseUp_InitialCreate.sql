IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [AdminAudit] (
        [Id] int NOT NULL IDENTITY,
        [ActorAccountId] int NULL,
        [ActorRole] nvarchar(60) NOT NULL,
        [Action] nvarchar(200) NOT NULL,
        [Target] nvarchar(200) NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [CorrelationId] nvarchar(64) NULL,
        [IsRestrictedContentAccess] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_AdminAudit] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [Blocks] (
        [Id] int NOT NULL IDENTITY,
        [StartupId] int NOT NULL,
        [InvestorId] int NOT NULL,
        [BlockedBy] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Blocks] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [Connections] (
        [Id] int NOT NULL IDENTITY,
        [StartupId] int NOT NULL,
        [InvestorId] int NOT NULL,
        [ConversationId] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Connections] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [Conversations] (
        [Id] int NOT NULL IDENTITY,
        [StartupId] int NOT NULL,
        [InvestorId] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Conversations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [InvitationDailyCounters] (
        [Id] int NOT NULL IDENTITY,
        [InvestorId] int NOT NULL,
        [DhakaDate] date NOT NULL,
        [Count] int NOT NULL,
        [Version] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_InvitationDailyCounters] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_InvitationCounter_Range] CHECK ([Count] >= 0 AND [Count] <= 10)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] int NOT NULL IDENTITY,
        [RecipientAccountId] int NULL,
        [RecipientTeamAccountId] int NULL,
        [Text] nvarchar(300) NOT NULL,
        [Link] nvarchar(200) NULL,
        [IsRead] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [Outbox] (
        [Id] int NOT NULL IDENTITY,
        [Type] nvarchar(100) NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [ProcessedAtUtc] datetime2 NULL,
        [Attempts] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Outbox] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [PaymentOrders] (
        [Id] int NOT NULL IDENTITY,
        [OrderNumber] nvarchar(30) NOT NULL,
        [StartupId] int NOT NULL,
        [PackageId] int NOT NULL,
        [TokensSnapshot] int NOT NULL,
        [PriceBdtSnapshot] decimal(18,2) NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [Status] int NOT NULL,
        [ProviderTransactionId] nvarchar(100) NULL,
        [CreditedAtUtc] datetime2 NULL,
        [Method] nvarchar(40) NULL,
        [Version] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PaymentOrders] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [PaymentProviderEvents] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NULL,
        [ProviderTransactionId] nvarchar(100) NOT NULL,
        [SignatureValid] bit NOT NULL,
        [PayloadHash] nvarchar(128) NOT NULL,
        [Result] nvarchar(200) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PaymentProviderEvents] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [PitchDrafts] (
        [Id] int NOT NULL IDENTITY,
        [StartupId] int NOT NULL,
        [InvestorId] int NULL,
        [Text] nvarchar(3000) NOT NULL,
        [State] int NOT NULL,
        [ReasonCode] nvarchar(60) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PitchDrafts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [PromotionPackages] (
        [Id] int NOT NULL IDENTITY,
        [Days] int NOT NULL,
        [CostPurchasedTokens] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PromotionPackages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [Promotions] (
        [Id] int NOT NULL IDENTITY,
        [PostId] int NOT NULL,
        [StartupId] int NOT NULL,
        [Days] int NOT NULL,
        [CostTokens] int NOT NULL,
        [RemainingSeconds] bigint NOT NULL,
        [Status] int NOT NULL,
        [StartedAtUtc] datetime2 NOT NULL,
        [LastTickUtc] datetime2 NOT NULL,
        [PauseReason] nvarchar(200) NULL,
        [Impressions] bigint NOT NULL,
        [Clicks] bigint NOT NULL,
        [Engagements] bigint NOT NULL,
        [FallbackImpressions] bigint NOT NULL,
        [Version] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Promotions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [Reports] (
        [Id] int NOT NULL IDENTITY,
        [Kind] int NOT NULL,
        [TargetReference] nvarchar(100) NOT NULL,
        [ConversationId] int NULL,
        [MessageId] int NULL,
        [Reason] nvarchar(500) NOT NULL,
        [ReporterAccountId] int NOT NULL,
        [Status] int NOT NULL,
        [ResolutionNote] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Reports] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [TokenLedger] (
        [Id] int NOT NULL IDENTITY,
        [StartupId] int NOT NULL,
        [Description] nvarchar(200) NOT NULL,
        [Bucket] int NOT NULL,
        [Amount] int NOT NULL,
        [Reference] nvarchar(100) NOT NULL,
        [IdempotencyKey] nvarchar(120) NOT NULL,
        [ActorAccountId] int NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_TokenLedger] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [TokenPackages] (
        [Id] int NOT NULL IDENTITY,
        [Tokens] int NOT NULL,
        [PriceBdt] decimal(18,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_TokenPackages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [UserAccounts] (
        [Id] int NOT NULL IDENTITY,
        [Email] nvarchar(256) NOT NULL,
        [NormalizedEmail] nvarchar(256) NOT NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Phone] nvarchar(30) NULL,
        [PrimaryRole] int NOT NULL,
        [AdminRole] int NOT NULL,
        [EmailVerified] bit NOT NULL,
        [FailedLoginCount] int NOT NULL,
        [LockoutEndUtc] datetime2 NULL,
        [MfaEnabled] bit NOT NULL,
        [MfaSecret] nvarchar(128) NULL,
        [AcceptedTermsVersion] nvarchar(16) NOT NULL,
        [AcceptedPrivacyVersion] nvarchar(16) NOT NULL,
        [PolicyAcceptedAtUtc] datetime2 NULL,
        [IsSuspended] bit NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        [Version] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_UserAccounts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [VerificationApplications] (
        [Id] int NOT NULL IDENTITY,
        [Subject] int NOT NULL,
        [StartupId] int NULL,
        [InvestorId] int NULL,
        [Status] int NOT NULL,
        [LicenceHolderType] int NULL,
        [LicenceHolderName] nvarchar(150) NULL,
        [SubmittedAtUtc] datetime2 NOT NULL,
        [DecidedAtUtc] datetime2 NULL,
        [ReviewerAccountId] int NULL,
        [DecisionReason] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_VerificationApplications] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [ConversationMembers] (
        [Id] int NOT NULL IDENTITY,
        [ConversationId] int NOT NULL,
        [Kind] int NOT NULL,
        [AccountId] int NULL,
        [TeamAccountId] int NULL,
        [AddedAtUtc] datetime2 NOT NULL,
        [RemovedAtUtc] datetime2 NULL,
        [Muted] bit NOT NULL,
        [Archived] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ConversationMembers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ConversationMembers_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [Messages] (
        [Id] int NOT NULL IDENTITY,
        [ConversationId] int NOT NULL,
        [AuthorKind] int NOT NULL,
        [AuthorAccountId] int NULL,
        [AuthorTeamAccountId] int NULL,
        [AuthorName] nvarchar(150) NOT NULL,
        [Body] nvarchar(2000) NOT NULL,
        [IsSystem] bit NOT NULL,
        [SentAtUtc] datetime2 NOT NULL,
        [EditedAtUtc] datetime2 NULL,
        [DeletedAtUtc] datetime2 NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Messages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [AccountTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserAccountId] int NOT NULL,
        [Purpose] nvarchar(32) NOT NULL,
        [TokenHash] nvarchar(128) NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [UsedAtUtc] datetime2 NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_AccountTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccountTokens_UserAccounts_UserAccountId] FOREIGN KEY ([UserAccountId]) REFERENCES [UserAccounts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [InvestorProfiles] (
        [Id] int NOT NULL IDENTITY,
        [AccountId] int NOT NULL,
        [DisplayName] nvarchar(150) NOT NULL,
        [Affiliation] nvarchar(150) NOT NULL,
        [InvestorType] nvarchar(60) NOT NULL,
        [Bio] nvarchar(1000) NOT NULL,
        [PreferredIndustries] nvarchar(400) NOT NULL,
        [PreferredStages] nvarchar(100) NOT NULL,
        [TicketMinUsd] decimal(18,2) NOT NULL,
        [TicketMaxUsd] decimal(18,2) NOT NULL,
        [Location] nvarchar(80) NOT NULL,
        [VerificationStatus] int NOT NULL,
        [VerifiedUntilUtc] datetime2 NULL,
        [ReceivePitches] bit NOT NULL,
        [IsSuspended] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [ColorIndex] int NOT NULL,
        [Version] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_InvestorProfiles] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Investor_TicketRange] CHECK ([TicketMinUsd] <= [TicketMaxUsd]),
        CONSTRAINT [FK_InvestorProfiles_UserAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [UserAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [Startups] (
        [Id] int NOT NULL IDENTITY,
        [OwnerAccountId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [NormalizedLegalName] nvarchar(100) NOT NULL,
        [Tagline] nvarchar(120) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Industry] nvarchar(60) NOT NULL,
        [Stage] int NOT NULL,
        [Location] nvarchar(80) NOT NULL,
        [FoundingYear] int NOT NULL,
        [BusinessModel] nvarchar(200) NULL,
        [Website] nvarchar(200) NULL,
        [Tags] nvarchar(300) NOT NULL,
        [FundingGoalUsd] decimal(18,2) NOT NULL,
        [VerificationStatus] int NOT NULL,
        [VerifiedUntilUtc] datetime2 NULL,
        [WelcomeTokensGranted] bit NOT NULL,
        [IsSuspended] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        [ViewCount] int NOT NULL,
        [ColorIndex] int NOT NULL,
        [Version] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Startups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Startups_UserAccounts_OwnerAccountId] FOREIGN KEY ([OwnerAccountId]) REFERENCES [UserAccounts] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [VerificationDocuments] (
        [Id] int NOT NULL IDENTITY,
        [ApplicationId] int NOT NULL,
        [Kind] nvarchar(40) NOT NULL,
        [StorageKey] nvarchar(200) NOT NULL,
        [ScanStatus] int NOT NULL,
        [Erased] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_VerificationDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VerificationDocuments_VerificationApplications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [VerificationApplications] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [InvestorClaims] (
        [Id] int NOT NULL IDENTITY,
        [InvestorId] int NOT NULL,
        [StartupName] nvarchar(100) NOT NULL,
        [Year] int NOT NULL,
        [Round] nvarchar(40) NOT NULL,
        [IsConfirmed] bit NOT NULL,
        [EvidenceReference] nvarchar(200) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_InvestorClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InvestorClaims_InvestorProfiles_InvestorId] FOREIGN KEY ([InvestorId]) REFERENCES [InvestorProfiles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [ConnectionRequests] (
        [Id] int NOT NULL IDENTITY,
        [Type] int NOT NULL,
        [StartupId] int NOT NULL,
        [InvestorId] int NOT NULL,
        [Message] nvarchar(3000) NOT NULL,
        [SentAtUtc] datetime2 NOT NULL,
        [ExpiresAtUtc] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [DecidedAtUtc] datetime2 NULL,
        [DecisionNote] nvarchar(300) NULL,
        [TokenBucket] int NULL,
        [AiDecisionVersion] nvarchar(40) NULL,
        [IdempotencyKey] nvarchar(100) NOT NULL,
        [Version] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ConnectionRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ConnectionRequests_InvestorProfiles_InvestorId] FOREIGN KEY ([InvestorId]) REFERENCES [InvestorProfiles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ConnectionRequests_Startups_StartupId] FOREIGN KEY ([StartupId]) REFERENCES [Startups] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [OwnershipTransferCases] (
        [Id] int NOT NULL IDENTITY,
        [StartupId] int NOT NULL,
        [FromAccountId] int NOT NULL,
        [IncomingOwnerName] nvarchar(150) NOT NULL,
        [IncomingOwnerEmail] nvarchar(256) NOT NULL,
        [OwnerConsentAtUtc] datetime2 NULL,
        [IncomingConsentAtUtc] datetime2 NULL,
        [Status] int NOT NULL,
        [ReviewerAccountId] int NULL,
        [DecisionReason] nvarchar(500) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_OwnershipTransferCases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OwnershipTransferCases_Startups_StartupId] FOREIGN KEY ([StartupId]) REFERENCES [Startups] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [Posts] (
        [Id] int NOT NULL IDENTITY,
        [StartupId] int NOT NULL,
        [Category] int NOT NULL,
        [LiveTitle] nvarchar(150) NULL,
        [LiveBody] nvarchar(max) NULL,
        [PendingTitle] nvarchar(150) NULL,
        [PendingBody] nvarchar(max) NULL,
        [PendingState] int NOT NULL,
        [PendingReasonCode] nvarchar(60) NULL,
        [ImageStorageKey] nvarchar(200) NULL,
        [PublishedAtUtc] datetime2 NULL,
        [IsHidden] bit NOT NULL,
        [ViewCount] int NOT NULL,
        [ShareCount] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Posts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Posts_Startups_StartupId] FOREIGN KEY ([StartupId]) REFERENCES [Startups] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [PrivateDocuments] (
        [Id] int NOT NULL IDENTITY,
        [StartupId] int NOT NULL,
        [Kind] int NOT NULL,
        [FileName] nvarchar(200) NOT NULL,
        [SizeBytes] bigint NOT NULL,
        [StorageKey] nvarchar(200) NOT NULL,
        [ScanStatus] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PrivateDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PrivateDocuments_Startups_StartupId] FOREIGN KEY ([StartupId]) REFERENCES [Startups] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [ProfileViewEvents] (
        [Id] int NOT NULL IDENTITY,
        [StartupId] int NOT NULL,
        [ViewerKey] nvarchar(100) NOT NULL,
        [ViewedAtUtc] datetime2 NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_ProfileViewEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProfileViewEvents_Startups_StartupId] FOREIGN KEY ([StartupId]) REFERENCES [Startups] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [StartupMetrics] (
        [Id] int NOT NULL IDENTITY,
        [StartupId] int NOT NULL,
        [Name] nvarchar(80) NOT NULL,
        [Value] nvarchar(80) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_StartupMetrics] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StartupMetrics_Startups_StartupId] FOREIGN KEY ([StartupId]) REFERENCES [Startups] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [TeamAccounts] (
        [Id] int NOT NULL IDENTITY,
        [StartupId] int NOT NULL,
        [Username] nvarchar(30) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Designation] nvarchar(100) NOT NULL,
        [LinkedInUrl] nvarchar(max) NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [Status] int NOT NULL,
        [PreviousConversationIds] nvarchar(500) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_TeamAccounts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TeamAccounts_Startups_StartupId] FOREIGN KEY ([StartupId]) REFERENCES [Startups] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [Wallets] (
        [Id] int NOT NULL IDENTITY,
        [StartupId] int NOT NULL,
        [WelcomeBalance] int NOT NULL,
        [PurchasedBalance] int NOT NULL,
        [Version] uniqueidentifier NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Wallets] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Wallet_Purchased_NonNegative] CHECK ([PurchasedBalance] >= 0),
        CONSTRAINT [CK_Wallet_Welcome_NonNegative] CHECK ([WelcomeBalance] >= 0),
        CONSTRAINT [FK_Wallets_Startups_StartupId] FOREIGN KEY ([StartupId]) REFERENCES [Startups] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [PostComments] (
        [Id] int NOT NULL IDENTITY,
        [PostId] int NOT NULL,
        [ParentCommentId] int NULL,
        [Depth] int NOT NULL,
        [AuthorAccountId] int NOT NULL,
        [AuthorName] nvarchar(150) NOT NULL,
        [Body] nvarchar(1000) NOT NULL,
        [DeletedAtUtc] datetime2 NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PostComments] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Comment_Depth] CHECK ([Depth] BETWEEN 1 AND 3),
        CONSTRAINT [FK_PostComments_PostComments_ParentCommentId] FOREIGN KEY ([ParentCommentId]) REFERENCES [PostComments] ([Id]),
        CONSTRAINT [FK_PostComments_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [PostReactions] (
        [Id] int NOT NULL IDENTITY,
        [PostId] int NOT NULL,
        [ActorAccountId] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PostReactions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PostReactions_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE TABLE [DocumentAccessGrants] (
        [Id] int NOT NULL IDENTITY,
        [DocumentId] int NOT NULL,
        [InvestorId] int NOT NULL,
        [GrantedAtUtc] datetime2 NOT NULL,
        [RevokedAtUtc] datetime2 NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_DocumentAccessGrants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentAccessGrants_InvestorProfiles_InvestorId] FOREIGN KEY ([InvestorId]) REFERENCES [InvestorProfiles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DocumentAccessGrants_PrivateDocuments_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [PrivateDocuments] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AccountTokens_Purpose_TokenHash] ON [AccountTokens] ([Purpose], [TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AccountTokens_UserAccountId] ON [AccountTokens] ([UserAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AdminAudit_CreatedAtUtc] ON [AdminAudit] ([CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Blocks_StartupId_InvestorId] ON [Blocks] ([StartupId], [InvestorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ConnectionRequests_IdempotencyKey] ON [ConnectionRequests] ([IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ConnectionRequests_InvestorId_Type_SentAtUtc] ON [ConnectionRequests] ([InvestorId], [Type], [SentAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ConnectionRequests_Status_ExpiresAtUtc] ON [ConnectionRequests] ([Status], [ExpiresAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_ConnectionRequest_PendingPair] ON [ConnectionRequests] ([StartupId], [InvestorId]) WHERE [Status] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Connections_ConversationId] ON [Connections] ([ConversationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Connections_StartupId_InvestorId] ON [Connections] ([StartupId], [InvestorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ConversationMembers_ConversationId_Kind_AccountId_TeamAccountId] ON [ConversationMembers] ([ConversationId], [Kind], [AccountId], [TeamAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ConversationMembers_TeamAccountId_RemovedAtUtc] ON [ConversationMembers] ([TeamAccountId], [RemovedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Conversations_StartupId_InvestorId] ON [Conversations] ([StartupId], [InvestorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DocumentAccessGrants_DocumentId_InvestorId] ON [DocumentAccessGrants] ([DocumentId], [InvestorId]) WHERE [RevokedAtUtc] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DocumentAccessGrants_InvestorId] ON [DocumentAccessGrants] ([InvestorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InvestorClaims_InvestorId] ON [InvestorClaims] ([InvestorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InvestorProfiles_AccountId] ON [InvestorProfiles] ([AccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InvestorProfiles_VerificationStatus_IsSuspended_IsDeleted] ON [InvestorProfiles] ([VerificationStatus], [IsSuspended], [IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InvitationDailyCounters_InvestorId_DhakaDate] ON [InvitationDailyCounters] ([InvestorId], [DhakaDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Messages_ConversationId_SentAtUtc] ON [Messages] ([ConversationId], [SentAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_RecipientAccountId_IsRead] ON [Notifications] ([RecipientAccountId], [IsRead]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Outbox_ProcessedAtUtc] ON [Outbox] ([ProcessedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OwnershipTransferCases_StartupId] ON [OwnershipTransferCases] ([StartupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PaymentOrders_OrderNumber] ON [PaymentOrders] ([OrderNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PaymentOrders_ProviderTransactionId] ON [PaymentOrders] ([ProviderTransactionId]) WHERE [ProviderTransactionId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PitchDrafts_StartupId] ON [PitchDrafts] ([StartupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PostComments_ParentCommentId] ON [PostComments] ([ParentCommentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PostComments_PostId_CreatedAtUtc] ON [PostComments] ([PostId], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PostReactions_PostId_ActorAccountId] ON [PostReactions] ([PostId], [ActorAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Posts_IsHidden_Category_PublishedAtUtc] ON [Posts] ([IsHidden], [Category], [PublishedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Posts_StartupId_PublishedAtUtc] ON [Posts] ([StartupId], [PublishedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PrivateDocuments_StartupId] ON [PrivateDocuments] ([StartupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProfileViewEvents_StartupId_ViewerKey_ViewedAtUtc] ON [ProfileViewEvents] ([StartupId], [ViewerKey], [ViewedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PromotionPackages_Days] ON [PromotionPackages] ([Days]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Promotions_PostId] ON [Promotions] ([PostId]) WHERE [Status] <> 3');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Promotions_Status_StartupId] ON [Promotions] ([Status], [StartupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Reports_Status_CreatedAtUtc] ON [Reports] ([Status], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StartupMetrics_StartupId] ON [StartupMetrics] ([StartupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Startups_NormalizedLegalName] ON [Startups] ([NormalizedLegalName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Startups_OwnerAccountId] ON [Startups] ([OwnerAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Startups_VerificationStatus_IsSuspended_IsDeleted] ON [Startups] ([VerificationStatus], [IsSuspended], [IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TeamAccounts_Email] ON [TeamAccounts] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TeamAccounts_StartupId_Status] ON [TeamAccounts] ([StartupId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TeamAccounts_Username] ON [TeamAccounts] ([Username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TokenLedger_IdempotencyKey] ON [TokenLedger] ([IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TokenLedger_StartupId_CreatedAtUtc] ON [TokenLedger] ([StartupId], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserAccounts_NormalizedEmail] ON [UserAccounts] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VerificationApplications_Status_SubmittedAtUtc] ON [VerificationApplications] ([Status], [SubmittedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VerificationDocuments_ApplicationId] ON [VerificationDocuments] ([ApplicationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Wallets_StartupId] ON [Wallets] ([StartupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261107184048_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261107184048_InitialCreate', N'10.0.14');
END;

COMMIT;
GO

