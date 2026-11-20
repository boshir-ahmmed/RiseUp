BEGIN TRANSACTION;
ALTER TABLE [Startups] ADD [CoverMediaId] int NULL;

ALTER TABLE [Startups] ADD [LogoMediaId] int NULL;

ALTER TABLE [Posts] ADD [PosterMediaId] int NULL;

ALTER TABLE [Posts] ADD [VideoMediaId] int NULL;

ALTER TABLE [Posts] ADD [VideoSeconds] int NULL;

ALTER TABLE [ConnectionRequests] ADD [AttachedDocumentIds] nvarchar(100) NOT NULL DEFAULT N'';

ALTER TABLE [ConnectionRequests] ADD [InfoRequestMessage] nvarchar(500) NULL;

ALTER TABLE [ConnectionRequests] ADD [InfoRequestedAtUtc] datetime2 NULL;

ALTER TABLE [ConnectionRequests] ADD [PackageVersion] int NULL;

CREATE TABLE [MediaAssets] (
    [Id] int NOT NULL IDENTITY,
    [Kind] int NOT NULL,
    [StorageKey] nvarchar(64) NOT NULL,
    [ContentType] nvarchar(100) NOT NULL,
    [FileName] nvarchar(200) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    [DurationSeconds] int NULL,
    [OwnerStartupId] int NULL,
    [OwnerAccountId] int NULL,
    [OwnerTeamAccountId] int NULL,
    [ScanStatus] int NOT NULL,
    [DeletedAtUtc] datetime2 NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_MediaAssets] PRIMARY KEY ([Id])
);

CREATE TABLE [Meetings] (
    [Id] int NOT NULL IDENTITY,
    [ConversationId] int NOT NULL,
    [ProposedByKind] int NOT NULL,
    [ProposedByAccountId] int NULL,
    [ProposedByName] nvarchar(150) NOT NULL,
    [Slot1Utc] datetime2 NOT NULL,
    [Slot2Utc] datetime2 NULL,
    [Slot3Utc] datetime2 NULL,
    [Agenda] nvarchar(140) NOT NULL,
    [Status] int NOT NULL,
    [ConfirmedSlotUtc] datetime2 NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Meetings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Meetings_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [Conversations] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [NameChangeRequests] (
    [Id] int NOT NULL IDENTITY,
    [AccountId] int NOT NULL,
    [FromName] nvarchar(150) NOT NULL,
    [ToName] nvarchar(60) NOT NULL,
    [Status] int NOT NULL,
    [ReviewerAccountId] int NULL,
    [DecidedAtUtc] datetime2 NULL,
    [Reason] nvarchar(500) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_NameChangeRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NameChangeRequests_UserAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [UserAccounts] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PitchEngagements] (
    [Id] int NOT NULL IDENTITY,
    [RequestId] int NOT NULL,
    [OpenedAtUtc] datetime2 NULL,
    [PitchWatchPercent] int NOT NULL,
    [DemoWatchPercent] int NOT NULL,
    [SlidesViewedMask] bigint NOT NULL,
    [DocumentsViewed] nvarchar(200) NOT NULL,
    [Version] uniqueidentifier NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_PitchEngagements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PitchEngagements_ConnectionRequests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [ConnectionRequests] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PitchPackages] (
    [Id] int NOT NULL IDENTITY,
    [StartupId] int NOT NULL,
    [PackageVersion] int NOT NULL,
    [PitchVideoId] int NULL,
    [DemoVideoId] int NULL,
    [TeaserVideoId] int NULL,
    [PitchPosterId] int NULL,
    [DemoPosterId] int NULL,
    [TeaserPosterId] int NULL,
    [PublicTeaser] bit NOT NULL,
    [PublicDemo] bit NOT NULL,
    [CaptionsAvailable] bit NOT NULL,
    [DeckMediaId] int NULL,
    [AskAmountUsd] decimal(18,2) NOT NULL,
    [Instrument] int NOT NULL,
    [ValuationUsd] decimal(18,2) NOT NULL,
    [DiscountPercent] int NOT NULL,
    [RunwayMonths] int NOT NULL,
    [MinTicketUsd] decimal(18,2) NOT NULL,
    [Highlights] nvarchar(1200) NOT NULL,
    [Version] uniqueidentifier NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_PitchPackages] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_PitchPackage_Amounts] CHECK ([AskAmountUsd] >= 0 AND [ValuationUsd] >= 0 AND [MinTicketUsd] >= 0),
    CONSTRAINT [CK_PitchPackage_Discount] CHECK ([DiscountPercent] >= 0 AND [DiscountPercent] <= 50),
    CONSTRAINT [FK_PitchPackages_Startups_StartupId] FOREIGN KEY ([StartupId]) REFERENCES [Startups] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PitchQuestions] (
    [Id] int NOT NULL IDENTITY,
    [RequestId] int NOT NULL,
    [AskedByAccountId] int NOT NULL,
    [AskerName] nvarchar(150) NOT NULL,
    [Text] nvarchar(300) NOT NULL,
    [Answer] nvarchar(600) NULL,
    [AnsweredAtUtc] datetime2 NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_PitchQuestions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PitchQuestions_ConnectionRequests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [ConnectionRequests] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SavedPosts] (
    [Id] int NOT NULL IDENTITY,
    [PostId] int NOT NULL,
    [AccountId] int NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_SavedPosts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SavedPosts_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SavedPosts_UserAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [UserAccounts] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Shortlist] (
    [Id] int NOT NULL IDENTITY,
    [InvestorId] int NOT NULL,
    [StartupId] int NOT NULL,
    [Stage] int NOT NULL,
    [Note] nvarchar(300) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_Shortlist] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Shortlist_InvestorProfiles_InvestorId] FOREIGN KEY ([InvestorId]) REFERENCES [InvestorProfiles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Shortlist_Startups_StartupId] FOREIGN KEY ([StartupId]) REFERENCES [Startups] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [MemberProfiles] (
    [Id] int NOT NULL IDENTITY,
    [AccountId] int NULL,
    [TeamAccountId] int NULL,
    [Headline] nvarchar(120) NOT NULL,
    [Location] nvarchar(80) NOT NULL,
    [Bio] nvarchar(600) NOT NULL,
    [LinkUrl] nvarchar(200) NULL,
    [PhotoMediaId] int NULL,
    [CoverMediaId] int NULL,
    [NameChangedAtUtc] datetime2 NULL,
    [Version] uniqueidentifier NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_MemberProfiles] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_MemberProfile_OneOwner] CHECK (([AccountId] IS NOT NULL AND [TeamAccountId] IS NULL) OR ([AccountId] IS NULL AND [TeamAccountId] IS NOT NULL)),
    CONSTRAINT [FK_MemberProfiles_MediaAssets_CoverMediaId] FOREIGN KEY ([CoverMediaId]) REFERENCES [MediaAssets] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MemberProfiles_MediaAssets_PhotoMediaId] FOREIGN KEY ([PhotoMediaId]) REFERENCES [MediaAssets] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MemberProfiles_TeamAccounts_TeamAccountId] FOREIGN KEY ([TeamAccountId]) REFERENCES [TeamAccounts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MemberProfiles_UserAccounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [UserAccounts] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PitchDocuments] (
    [Id] int NOT NULL IDENTITY,
    [PackageId] int NOT NULL,
    [MediaId] int NOT NULL,
    [Label] nvarchar(60) NOT NULL,
    [AllowDownload] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_PitchDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PitchDocuments_MediaAssets_MediaId] FOREIGN KEY ([MediaId]) REFERENCES [MediaAssets] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_PitchDocuments_PitchPackages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [PitchPackages] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PitchFaqs] (
    [Id] int NOT NULL IDENTITY,
    [PackageId] int NOT NULL,
    [Question] nvarchar(200) NOT NULL,
    [Answer] nvarchar(600) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_PitchFaqs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PitchFaqs_PitchPackages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [PitchPackages] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PitchSlides] (
    [Id] int NOT NULL IDENTITY,
    [PackageId] int NOT NULL,
    [Position] int NOT NULL,
    [Title] nvarchar(80) NOT NULL,
    [Body] nvarchar(240) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_PitchSlides] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PitchSlides_PitchPackages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [PitchPackages] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PitchTeamMembers] (
    [Id] int NOT NULL IDENTITY,
    [PackageId] int NOT NULL,
    [Name] nvarchar(80) NOT NULL,
    [Role] nvarchar(120) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_PitchTeamMembers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PitchTeamMembers_PitchPackages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [PitchPackages] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UseOfFunds] (
    [Id] int NOT NULL IDENTITY,
    [PackageId] int NOT NULL,
    [Label] nvarchar(40) NOT NULL,
    [Percent] int NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_UseOfFunds] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_UseOfFunds_Percent] CHECK ([Percent] >= 0 AND [Percent] <= 100),
    CONSTRAINT [FK_UseOfFunds_PitchPackages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [PitchPackages] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Posts_VideoMediaId] ON [Posts] ([VideoMediaId]);

CREATE INDEX [IX_MediaAssets_OwnerStartupId_Kind] ON [MediaAssets] ([OwnerStartupId], [Kind]);

CREATE INDEX [IX_MediaAssets_ScanStatus] ON [MediaAssets] ([ScanStatus]);

CREATE UNIQUE INDEX [IX_MediaAssets_StorageKey] ON [MediaAssets] ([StorageKey]);

CREATE INDEX [IX_Meetings_ConversationId] ON [Meetings] ([ConversationId]);

CREATE UNIQUE INDEX [IX_MemberProfiles_AccountId] ON [MemberProfiles] ([AccountId]) WHERE [AccountId] IS NOT NULL;

CREATE INDEX [IX_MemberProfiles_CoverMediaId] ON [MemberProfiles] ([CoverMediaId]);

CREATE INDEX [IX_MemberProfiles_PhotoMediaId] ON [MemberProfiles] ([PhotoMediaId]);

CREATE UNIQUE INDEX [IX_MemberProfiles_TeamAccountId] ON [MemberProfiles] ([TeamAccountId]) WHERE [TeamAccountId] IS NOT NULL;

CREATE UNIQUE INDEX [UX_NameChange_PendingPerAccount] ON [NameChangeRequests] ([AccountId]) WHERE [Status] = 0;

CREATE UNIQUE INDEX [IX_PitchDocuments_MediaId] ON [PitchDocuments] ([MediaId]);

CREATE INDEX [IX_PitchDocuments_PackageId] ON [PitchDocuments] ([PackageId]);

CREATE UNIQUE INDEX [IX_PitchEngagements_RequestId] ON [PitchEngagements] ([RequestId]);

CREATE INDEX [IX_PitchFaqs_PackageId] ON [PitchFaqs] ([PackageId]);

CREATE UNIQUE INDEX [IX_PitchPackages_StartupId] ON [PitchPackages] ([StartupId]);

CREATE INDEX [IX_PitchQuestions_RequestId] ON [PitchQuestions] ([RequestId]);

CREATE INDEX [IX_PitchSlides_PackageId_Position] ON [PitchSlides] ([PackageId], [Position]);

CREATE INDEX [IX_PitchTeamMembers_PackageId] ON [PitchTeamMembers] ([PackageId]);

CREATE INDEX [IX_SavedPosts_AccountId] ON [SavedPosts] ([AccountId]);

CREATE UNIQUE INDEX [IX_SavedPosts_PostId_AccountId] ON [SavedPosts] ([PostId], [AccountId]);

CREATE UNIQUE INDEX [IX_Shortlist_InvestorId_StartupId] ON [Shortlist] ([InvestorId], [StartupId]);

CREATE INDEX [IX_Shortlist_StartupId] ON [Shortlist] ([StartupId]);

CREATE INDEX [IX_UseOfFunds_PackageId] ON [UseOfFunds] ([PackageId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261118044045_AddVideoPitchingAndProfiles', N'10.0.14');

COMMIT;
GO

