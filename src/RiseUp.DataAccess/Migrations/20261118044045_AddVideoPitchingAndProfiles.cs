using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RiseUp.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoPitchingAndProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CoverMediaId",
                table: "Startups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LogoMediaId",
                table: "Startups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PosterMediaId",
                table: "Posts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoMediaId",
                table: "Posts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoSeconds",
                table: "Posts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachedDocumentIds",
                table: "ConnectionRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InfoRequestMessage",
                table: "ConnectionRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InfoRequestedAtUtc",
                table: "ConnectionRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackageVersion",
                table: "ConnectionRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    OwnerStartupId = table.Column<int>(type: "int", nullable: true),
                    OwnerAccountId = table.Column<int>(type: "int", nullable: true),
                    OwnerTeamAccountId = table.Column<int>(type: "int", nullable: true),
                    ScanStatus = table.Column<int>(type: "int", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Meetings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    ProposedByKind = table.Column<int>(type: "int", nullable: false),
                    ProposedByAccountId = table.Column<int>(type: "int", nullable: true),
                    ProposedByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slot1Utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Slot2Utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Slot3Utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Agenda = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ConfirmedSlotUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meetings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meetings_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NameChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    FromName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ToName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewerAccountId = table.Column<int>(type: "int", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NameChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NameChangeRequests_UserAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PitchEngagements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PitchWatchPercent = table.Column<int>(type: "int", nullable: false),
                    DemoWatchPercent = table.Column<int>(type: "int", nullable: false),
                    SlidesViewedMask = table.Column<long>(type: "bigint", nullable: false),
                    DocumentsViewed = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PitchEngagements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PitchEngagements_ConnectionRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ConnectionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PitchPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartupId = table.Column<int>(type: "int", nullable: false),
                    PackageVersion = table.Column<int>(type: "int", nullable: false),
                    PitchVideoId = table.Column<int>(type: "int", nullable: true),
                    DemoVideoId = table.Column<int>(type: "int", nullable: true),
                    TeaserVideoId = table.Column<int>(type: "int", nullable: true),
                    PitchPosterId = table.Column<int>(type: "int", nullable: true),
                    DemoPosterId = table.Column<int>(type: "int", nullable: true),
                    TeaserPosterId = table.Column<int>(type: "int", nullable: true),
                    PublicTeaser = table.Column<bool>(type: "bit", nullable: false),
                    PublicDemo = table.Column<bool>(type: "bit", nullable: false),
                    CaptionsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    DeckMediaId = table.Column<int>(type: "int", nullable: true),
                    AskAmountUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Instrument = table.Column<int>(type: "int", nullable: false),
                    ValuationUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountPercent = table.Column<int>(type: "int", nullable: false),
                    RunwayMonths = table.Column<int>(type: "int", nullable: false),
                    MinTicketUsd = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Highlights = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PitchPackages", x => x.Id);
                    table.CheckConstraint("CK_PitchPackage_Amounts", "[AskAmountUsd] >= 0 AND [ValuationUsd] >= 0 AND [MinTicketUsd] >= 0");
                    table.CheckConstraint("CK_PitchPackage_Discount", "[DiscountPercent] >= 0 AND [DiscountPercent] <= 50");
                    table.ForeignKey(
                        name: "FK_PitchPackages_Startups_StartupId",
                        column: x => x.StartupId,
                        principalTable: "Startups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PitchQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    AskedByAccountId = table.Column<int>(type: "int", nullable: false),
                    AskerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    AnsweredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PitchQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PitchQuestions_ConnectionRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "ConnectionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedPosts_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedPosts_UserAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Shortlist",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvestorId = table.Column<int>(type: "int", nullable: false),
                    StartupId = table.Column<int>(type: "int", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shortlist", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shortlist_InvestorProfiles_InvestorId",
                        column: x => x.InvestorId,
                        principalTable: "InvestorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Shortlist_Startups_StartupId",
                        column: x => x.StartupId,
                        principalTable: "Startups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    TeamAccountId = table.Column<int>(type: "int", nullable: true),
                    Headline = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    LinkUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PhotoMediaId = table.Column<int>(type: "int", nullable: true),
                    CoverMediaId = table.Column<int>(type: "int", nullable: true),
                    NameChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberProfiles", x => x.Id);
                    table.CheckConstraint("CK_MemberProfile_OneOwner", "([AccountId] IS NULL) <> ([TeamAccountId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_MemberProfiles_MediaAssets_CoverMediaId",
                        column: x => x.CoverMediaId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberProfiles_MediaAssets_PhotoMediaId",
                        column: x => x.PhotoMediaId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberProfiles_TeamAccounts_TeamAccountId",
                        column: x => x.TeamAccountId,
                        principalTable: "TeamAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberProfiles_UserAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PitchDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    MediaId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    AllowDownload = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PitchDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PitchDocuments_MediaAssets_MediaId",
                        column: x => x.MediaId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PitchDocuments_PitchPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "PitchPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PitchFaqs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PitchFaqs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PitchFaqs_PitchPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "PitchPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PitchSlides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PitchSlides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PitchSlides_PitchPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "PitchPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PitchTeamMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PitchTeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PitchTeamMembers_PitchPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "PitchPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UseOfFunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Percent = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UseOfFunds", x => x.Id);
                    table.CheckConstraint("CK_UseOfFunds_Percent", "[Percent] >= 0 AND [Percent] <= 100");
                    table.ForeignKey(
                        name: "FK_UseOfFunds_PitchPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "PitchPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_VideoMediaId",
                table: "Posts",
                column: "VideoMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_OwnerStartupId_Kind",
                table: "MediaAssets",
                columns: new[] { "OwnerStartupId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_ScanStatus",
                table: "MediaAssets",
                column: "ScanStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_StorageKey",
                table: "MediaAssets",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_ConversationId",
                table: "Meetings",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberProfiles_AccountId",
                table: "MemberProfiles",
                column: "AccountId",
                unique: true,
                filter: "[AccountId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MemberProfiles_CoverMediaId",
                table: "MemberProfiles",
                column: "CoverMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberProfiles_PhotoMediaId",
                table: "MemberProfiles",
                column: "PhotoMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberProfiles_TeamAccountId",
                table: "MemberProfiles",
                column: "TeamAccountId",
                unique: true,
                filter: "[TeamAccountId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_NameChange_PendingPerAccount",
                table: "NameChangeRequests",
                column: "AccountId",
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PitchDocuments_MediaId",
                table: "PitchDocuments",
                column: "MediaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PitchDocuments_PackageId",
                table: "PitchDocuments",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PitchEngagements_RequestId",
                table: "PitchEngagements",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PitchFaqs_PackageId",
                table: "PitchFaqs",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PitchPackages_StartupId",
                table: "PitchPackages",
                column: "StartupId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PitchQuestions_RequestId",
                table: "PitchQuestions",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PitchSlides_PackageId_Position",
                table: "PitchSlides",
                columns: new[] { "PackageId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_PitchTeamMembers_PackageId",
                table: "PitchTeamMembers",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPosts_AccountId",
                table: "SavedPosts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPosts_PostId_AccountId",
                table: "SavedPosts",
                columns: new[] { "PostId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shortlist_InvestorId_StartupId",
                table: "Shortlist",
                columns: new[] { "InvestorId", "StartupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shortlist_StartupId",
                table: "Shortlist",
                column: "StartupId");

            migrationBuilder.CreateIndex(
                name: "IX_UseOfFunds_PackageId",
                table: "UseOfFunds",
                column: "PackageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Meetings");

            migrationBuilder.DropTable(
                name: "MemberProfiles");

            migrationBuilder.DropTable(
                name: "NameChangeRequests");

            migrationBuilder.DropTable(
                name: "PitchDocuments");

            migrationBuilder.DropTable(
                name: "PitchEngagements");

            migrationBuilder.DropTable(
                name: "PitchFaqs");

            migrationBuilder.DropTable(
                name: "PitchQuestions");

            migrationBuilder.DropTable(
                name: "PitchSlides");

            migrationBuilder.DropTable(
                name: "PitchTeamMembers");

            migrationBuilder.DropTable(
                name: "SavedPosts");

            migrationBuilder.DropTable(
                name: "Shortlist");

            migrationBuilder.DropTable(
                name: "UseOfFunds");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropTable(
                name: "PitchPackages");

            migrationBuilder.DropIndex(
                name: "IX_Posts_VideoMediaId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CoverMediaId",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "LogoMediaId",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "PosterMediaId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoMediaId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoSeconds",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "AttachedDocumentIds",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "InfoRequestMessage",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "InfoRequestedAtUtc",
                table: "ConnectionRequests");

            migrationBuilder.DropColumn(
                name: "PackageVersion",
                table: "ConnectionRequests");
        }
    }
}
