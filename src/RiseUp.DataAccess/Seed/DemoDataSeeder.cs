using Microsoft.EntityFrameworkCore;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Governance;
using RiseUp.Entities.Identity;
using RiseUp.Entities.Investors;
using RiseUp.Entities.Payments;
using RiseUp.Entities.Promotions;
using RiseUp.Entities.Social;
using RiseUp.Entities.Startups;
using RiseUp.Entities.Verification;
using RiseUp.Entities.Wallets;
using RiseUp.Framework.Time;

namespace RiseUp.DataAccess.Seed;

/// <summary>
/// Clearly labelled DEMO data (all people and organisations are fictional). Runs only in Development or tests.
/// Every demo password is <see cref="DemoPassword"/>; admin accounts still require 2FA (demo code: any 6 digits in Development).
/// </summary>
public sealed partial class DemoDataSeeder(RiseUpDbContext db, IPasswordHasher hasher, IClock clock, IFileStorage storage, Microsoft.Extensions.Options.IOptions<SeedOptions> seedOptions)
{
    public const string DemoPassword = "Demo#2026pass";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.UserAccounts.AnyAsync(ct)) return;
        var now = clock.UtcNow;
        var hash = hasher.Hash(DemoPassword);

        UserAccount U(string email, string name, PrimaryRole role, AdminRole admin = AdminRole.None) => new()
        {
            Email = email, NormalizedEmail = email.ToUpperInvariant(), FullName = name, PasswordHash = hash, PrimaryRole = role,
            AdminRole = admin, EmailVerified = true, MfaEnabled = admin != AdminRole.None, AcceptedTermsVersion = "0.1",
            AcceptedPrivacyVersion = "0.1", PolicyAcceptedAtUtc = now.AddDays(-100), CreatedAtUtc = now.AddDays(-100),
        };

        var founders = new[]
        {
            U("farhan@nodifreight.example", "Farhan Kabir", PrimaryRole.Founder),
            U("hello@takaledger.example", "Nabila Rahman", PrimaryRole.Founder),
            U("team@shasthyolink.example", "Dr. Mahin Siddiqui", PrimaryRole.Founder),
            U("sabbir@pathshalabyte.example", "Sabbir Hasan", PrimaryRole.Founder),
            U("ishrat@sobujgrid.example", "Ishrat Jahan", PrimaryRole.Founder),
            U("rk@haatbazar.example", "Rezaul Karim", PrimaryRole.Founder),
            U("tasnim@juteloop.example", "Tasnim Ara", PrimaryRole.Founder),
            U("ayesha@krishicloud.example", "Ayesha Noor", PrimaryRole.Founder),
        };
        var investorUsers = new[]
        {
            U("sadia@padmavc.example", "Sadia Haque", PrimaryRole.Investor),
            U("imran@angel.example", "Imran Chowdhury", PrimaryRole.Investor),
            U("nusrat@meghnacap.example", "Nusrat Jahan", PrimaryRole.Investor),
            U("arif@bengalangels.example", "Arif Hossain", PrimaryRole.Investor),
            U("tanvir@deltagp.example", "Tanvir Alam", PrimaryRole.Investor),
            U("mehnaz@angel.example", "Mehnaz Karim", PrimaryRole.Investor),
        };
        var admins = new[]
        {
            U("super@riseup.example", "Lamia (Super Admin)", PrimaryRole.Admin, AdminRole.SuperAdmin),
            U("verify@riseup.example", "Rashed (Verification)", PrimaryRole.Admin, AdminRole.VerificationAdmin),
            U("moderator@riseup.example", "Priya (Moderator)", PrimaryRole.Admin, AdminRole.Moderator),
            U("finance@riseup.example", "Kamal (Finance)", PrimaryRole.Admin, AdminRole.FinanceAdmin),
        };
        db.UserAccounts.AddRange(founders); db.UserAccounts.AddRange(investorUsers); db.UserAccounts.AddRange(admins);
        await db.SaveChangesAsync(ct);

        Startup S(int i, string name, string tagline, string desc, string industry, FundingStage stage, string loc, int year, decimal goal, bool verified, params (string, string)[] metrics) => new()
        {
            OwnerAccountId = founders[i].Id, Name = name, NormalizedLegalName = name.ToUpperInvariant(), Tagline = tagline, Description = desc,
            Industry = industry, Stage = stage, Location = loc, FoundingYear = year, FundingGoalUsd = goal, ColorIndex = i,
            BusinessModel = "See profile", Website = name.Replace(" ", "").ToLowerInvariant() + ".example", Tags = industry.ToLowerInvariant(),
            VerificationStatus = verified ? VerificationStatus.Verified : VerificationStatus.Pending,
            VerifiedUntilUtc = verified ? now.AddDays(220) : null, WelcomeTokensGranted = verified, ViewCount = 200 + i * 300,
            CreatedAtUtc = now.AddDays(-90 + i * 10),
            Metrics = metrics.Select(m => new StartupMetric { Name = m.Item1, Value = m.Item2, CreatedAtUtc = now }).ToList(),
        };
        var startups = new[]
        {
            S(0, "Nodi Freight", "Digital freight matching for Bangladesh's SME shippers and river-port truckers.", "Nodi Freight connects small and mid-sized shippers with vetted truck and launch operators across 38 districts. Real-time quotes, digital proof of delivery and mobile-money settlement reduce empty return trips.", "Logistics", FundingStage.Seed, "Dhaka", 2022, 600000, true, ("Monthly GMV", "$310k"), ("Active shippers", "1,240"), ("MoM growth", "14%")),
            S(1, "Taka Ledger", "Bookkeeping and credit scoring for neighbourhood shops.", "An Android-first ledger app for shops that turns daily transactions into an alternative credit score for partner MFIs.", "Fintech", FundingStage.PreSeed, "Dhaka", 2023, 250000, true, ("Shops onboarded", "8,900")),
            S(2, "Shasthyo Link", "Tele-triage and pharmacy fulfilment for upazila clinics.", "Shasthyo Link equips rural clinics with a triage workflow, specialist teleconsultations and next-day medicine delivery.", "Healthtech", FundingStage.Seed, "Dhaka", 2021, 900000, true, ("Partner clinics", "64")),
            S(3, "Pathshala Byte", "Bangla-first coding lessons for secondary students.", "Gamified Bangla-language programming curriculum delivered offline-first for schools with limited connectivity.", "Edtech", FundingStage.PreSeed, "Rajshahi", 2024, 150000, true, ("Schools", "37")),
            S(4, "Sobuj Grid", "Pay-as-you-go solar micro-grids for char islands.", "Containerised solar micro-grids with smart meters and mobile-money top-ups for off-grid river island communities.", "Climate & Energy", FundingStage.Seed, "Khulna", 2020, 1200000, true, ("Households served", "4,300")),
            S(5, "Haat Bazar Direct", "Wholesale sourcing app linking haat traders to city retailers.", "A B2B marketplace that consolidates produce and FMCG from rural haats into next-morning deliveries.", "E-commerce", FundingStage.SeriesA, "Chattogram", 2019, 3000000, true, ("Annual GMV", "$9.4M")),
            S(6, "Jute Loop", "Traceable jute composites replacing plastic packaging.", "Jute Loop manufactures moulded jute-fibre packaging for garment exporters, with batch-level traceability.", "Manufacturing", FundingStage.PreSeed, "Narayanganj", 2023, 400000, true, ("Pilot buyers", "5")),
            S(7, "Krishi Cloud", "Soil and weather advisory over SMS for smallholder farmers.", "Localised crop advisories combining soil tests, weather feeds and SMS/voice delivery for farmers without smartphones.", "Agritech", FundingStage.PreSeed, "Bogura", 2025, 200000, false, ("Farmers", "2,100")),
        };
        db.Startups.AddRange(startups);

        InvestorProfile I(int i, string aff, string type, string industries, string stages, decimal min, decimal max, string loc, bool verified, bool receive, string bio) => new()
        {
            AccountId = investorUsers[i].Id, DisplayName = investorUsers[i].FullName, Affiliation = aff, InvestorType = type,
            PreferredIndustries = industries, PreferredStages = stages, TicketMinUsd = min, TicketMaxUsd = max, Location = loc, Bio = bio,
            VerificationStatus = verified ? VerificationStatus.Verified : VerificationStatus.Pending,
            VerifiedUntilUtc = verified ? now.AddDays(250) : null, ReceivePitches = receive, ColorIndex = i + 2, CreatedAtUtc = now.AddDays(-120),
        };
        var investors = new[]
        {
            I(0, "Padma Ventures (demo)", "Venture fund", "Fintech,Logistics,E-commerce", "Seed,SeriesA", 50000, 500000, "Dhaka", true, true, "Partner focused on infrastructure for SME commerce."),
            I(1, "Independent angel", "Angel", "Agritech,Climate & Energy,Logistics", "PreSeed,Seed", 10000, 100000, "Chattogram", true, true, "Ex-operator in agri supply chains."),
            I(2, "Meghna Capital (demo)", "Venture fund", "Healthtech,Edtech", "SeriesA", 250000, 2000000, "Dhaka", true, false, "Invests in health and learning outcomes at scale."),
            I(3, "Bengal Angels Network (demo)", "Angel network", "E-commerce,Logistics,SaaS", "PreSeed", 5000, 50000, "Sylhet", true, true, "Syndicate lead for early B2B commerce."),
            I(4, "Delta Growth Partners (demo)", "Venture fund", "Fintech,SaaS,Logistics", "SeriesA,SeriesB", 500000, 5000000, "Singapore / Dhaka", true, true, "Growth-stage investor across South and Southeast Asia."),
            I(5, "Independent angel", "Angel", "Climate & Energy,Manufacturing", "PreSeed,Seed", 10000, 150000, "Dhaka", false, true, "Circular-economy angel."),
        };
        investors[0].Claims.Add(new InvestorClaim { StartupName = "Taka Ledger", Year = 2024, Round = "Pre-seed", IsConfirmed = true, CreatedAtUtc = now });
        investors[3].Claims.Add(new InvestorClaim { StartupName = "Haat Bazar Direct", Year = 2020, Round = "Pre-seed", IsConfirmed = false, CreatedAtUtc = now });
        db.InvestorProfiles.AddRange(investors);
        await db.SaveChangesAsync(ct);

        // Wallets: verified startups received their one-time welcome grant.
        foreach (var s in startups.Where(s => s.WelcomeTokensGranted))
        {
            db.Wallets.Add(new StartupWallet { StartupId = s.Id, WelcomeBalance = 20, CreatedAtUtc = now });
            db.TokenLedger.Add(new TokenLedgerEntry { StartupId = s.Id, Description = "Welcome grant (first verification)", Bucket = TokenBucket.Welcome, Amount = 20, Reference = "verification", IdempotencyKey = $"welcome:{s.Id}", CreatedAtUtc = now.AddDays(-80) });
        }
        db.TokenPackages.AddRange(
            new TokenPackage { Tokens = 10, PriceBdt = 500, CreatedAtUtc = now },
            new TokenPackage { Tokens = 25, PriceBdt = 1100, CreatedAtUtc = now },
            new TokenPackage { Tokens = 60, PriceBdt = 2400, CreatedAtUtc = now });
        db.PromotionPackages.AddRange(
            new PromotionPackage { Days = 1, CostPurchasedTokens = 3, CreatedAtUtc = now },
            new PromotionPackage { Days = 3, CostPurchasedTokens = 7, CreatedAtUtc = now },
            new PromotionPackage { Days = 7, CostPurchasedTokens = 15, CreatedAtUtc = now });

        // Existing connection + conversation between Nodi Freight and Sadia Haque.
        var nodi = startups[0]; var sadia = investors[0];
        var conv = new Conversation { StartupId = nodi.Id, InvestorId = sadia.Id, CreatedAtUtc = now.AddDays(-38) };
        conv.Members.Add(new ConversationMember { Kind = MemberKind.Owner, AccountId = founders[0].Id, AddedAtUtc = now.AddDays(-38) });
        conv.Members.Add(new ConversationMember { Kind = MemberKind.Investor, AccountId = investorUsers[0].Id, AddedAtUtc = now.AddDays(-38) });
        db.Conversations.Add(conv);
        var rafi = new TeamAccount { StartupId = nodi.Id, Username = "rafi.ops", Email = "rafi@nodifreight.example", FullName = "Rafi Ahmed", Designation = "Head of Operations", PasswordHash = hash, Status = TeamAccountStatus.Active, CreatedAtUtc = now.AddDays(-37) };
        db.TeamAccounts.Add(rafi);
        db.ConnectionRequests.Add(new ConnectionRequest { Type = RequestType.Pitch, StartupId = nodi.Id, InvestorId = sadia.Id, Message = "Nodi Freight is digitising SME freight across 38 districts.", SentAtUtc = now.AddDays(-40), ExpiresAtUtc = now.AddDays(-26), Status = RequestStatus.Accepted, DecidedAtUtc = now.AddDays(-38), TokenBucket = TokenBucket.Welcome, IdempotencyKey = "seed:r0", CreatedAtUtc = now.AddDays(-40) });
        await db.SaveChangesAsync(ct);
        conv.Members.Add(new ConversationMember { Kind = MemberKind.Team, TeamAccountId = rafi.Id, AddedAtUtc = now.AddDays(-37) });
        db.Connections.Add(new StartupInvestorConnection { StartupId = nodi.Id, InvestorId = sadia.Id, ConversationId = conv.Id, CreatedAtUtc = now.AddDays(-38) });
        db.Messages.AddRange(
            new Message { ConversationId = conv.Id, AuthorKind = MemberKind.Investor, AuthorAccountId = investorUsers[0].Id, AuthorName = "Sadia Haque", Body = "Thanks for the pitch, Farhan. Could you share unit economics by corridor?", SentAtUtc = now.AddDays(-38), CreatedAtUtc = now.AddDays(-38) },
            new Message { ConversationId = conv.Id, AuthorKind = MemberKind.Owner, AuthorAccountId = founders[0].Id, AuthorName = "Farhan Kabir", Body = "Absolutely. I've added Rafi from our ops team, he owns the corridor data.", SentAtUtc = now.AddDays(-37), CreatedAtUtc = now.AddDays(-37) },
            new Message { ConversationId = conv.Id, AuthorKind = MemberKind.Team, AuthorTeamAccountId = rafi.Id, AuthorName = "Rafi Ahmed", Body = "Dhaka–Chattogram is our densest lane: 46% of GMV.", SentAtUtc = now.AddDays(-37).AddHours(1), CreatedAtUtc = now.AddDays(-37) });

        // Pending requests: pitch Taka Ledger -> Sadia, invitation Arif -> Nodi Freight.
        db.ConnectionRequests.Add(new ConnectionRequest { Type = RequestType.Pitch, StartupId = startups[1].Id, InvestorId = sadia.Id, Message = "Taka Ledger is turning 8,900 shop ledgers into credit histories. We are raising $250k.", SentAtUtc = now.AddDays(-2), ExpiresAtUtc = now.AddDays(12), Status = RequestStatus.Pending, TokenBucket = TokenBucket.Welcome, IdempotencyKey = "seed:r4", CreatedAtUtc = now.AddDays(-2) });
        db.ConnectionRequests.Add(new ConnectionRequest { Type = RequestType.Invitation, StartupId = nodi.Id, InvestorId = investors[3].Id, Message = "Would love to learn more about your SME shipper growth.", SentAtUtc = now.AddDays(-1), ExpiresAtUtc = now.AddDays(13), Status = RequestStatus.Pending, IdempotencyKey = "seed:r3", CreatedAtUtc = now.AddDays(-1) });
        (await db.Wallets.FirstAsync(w => w.StartupId == startups[1].Id, ct)).WelcomeBalance = 19;
        db.TokenLedger.Add(new TokenLedgerEntry { StartupId = startups[1].Id, Description = "Pitch delivered → Sadia Haque", Bucket = TokenBucket.Welcome, Amount = -1, Reference = "seed:r4", IdempotencyKey = "pitch:seed:r4", CreatedAtUtc = now.AddDays(-2) });

        // Posts (AI-approved live revisions).
        StartupPost P(Startup s, PostCategory c, string title, string body, int daysAgo) => new()
        { StartupId = s.Id, Category = c, LiveTitle = title, LiveBody = body, PendingState = ModerationState.Approved, PublishedAtUtc = now.AddDays(-daysAgo), CreatedAtUtc = now.AddDays(-daysAgo), ViewCount = 400 + daysAgo * 37 };
        var posts = new[]
        {
            P(nodi, PostCategory.Milestones, "Nodi Freight crosses 1,240 active SME shippers", "This quarter we passed 1,240 active shippers and cut empty return trips on the Dhaka–Chattogram corridor by 19%.", 2),
            P(startups[2], PostCategory.Announcements, "Shasthyo Link now live in 64 upazila clinics", "We added 12 clinics in Sylhet division this month. Specialist teleconsultations now reach patients within a median 22 minutes.", 4),
            P(startups[5], PostCategory.Hiring, "We're hiring: Category Manager, FMCG (Chattogram)", "Haat Bazar Direct is hiring a category manager to grow our FMCG supplier base.", 6),
            P(startups[1], PostCategory.Products, "Taka Ledger 2.0: offline sync and Bangla voice entry", "Shopkeepers can now record sales by voice in Bangla, even without connectivity.", 8),
            P(startups[6], PostCategory.Products, "Introducing LoopTray: moulded jute trays for garment exports", "LoopTray replaces single-use plastic trays in folded-shirt packaging, with batch traceability for EU buyers.", 3),
            P(startups[3], PostCategory.Achievements, "Pathshala Byte wins national ed-innovation challenge", "Our offline-first Bangla coding curriculum was recognised for reaching 37 schools in Rajshahi division.", 1),
        };
        db.Posts.AddRange(posts);
        await db.SaveChangesAsync(ct);
        db.PostReactions.AddRange(new PostReaction { PostId = posts[0].Id, ActorAccountId = investorUsers[0].Id, CreatedAtUtc = now }, new PostReaction { PostId = posts[0].Id, ActorAccountId = investorUsers[3].Id, CreatedAtUtc = now });
        db.PostComments.Add(new PostComment { PostId = posts[0].Id, Depth = 1, AuthorAccountId = investorUsers[0].Id, AuthorName = "Sadia Haque", Body = "Impressive corridor efficiency gains.", CreatedAtUtc = now.AddDays(-1) });
        // Nodi Freight bought 25 credits and promoted post 1 for 3 days.
        var nodiWallet = await db.Wallets.FirstAsync(w => w.StartupId == nodi.Id, ct);
        nodiWallet.WelcomeBalance = 18; nodiWallet.PurchasedBalance = 18;
        db.PaymentOrders.Add(new PaymentOrder { OrderNumber = "PO-7001", StartupId = nodi.Id, PackageId = 2, TokensSnapshot = 25, PriceBdtSnapshot = 1100, Status = PaymentStatus.Paid, ProviderTransactionId = "SBX-88213", CreditedAtUtc = now.AddDays(-20), Method = "bKash (sandbox)", CreatedAtUtc = now.AddDays(-20) });
        db.TokenLedger.AddRange(
            new TokenLedgerEntry { StartupId = nodi.Id, Description = "Pitch delivered → Sadia Haque", Bucket = TokenBucket.Welcome, Amount = -1, Reference = "seed:r0", IdempotencyKey = "pitch:seed:r0", CreatedAtUtc = now.AddDays(-40) },
            new TokenLedgerEntry { StartupId = nodi.Id, Description = Mitigation(), Bucket = TokenBucket.Welcome, Amount = -1, Reference = "seed:r1", IdempotencyKey = "pitch:seed:r1", CreatedAtUtc = now.AddDays(-12) },
            new TokenLedgerEntry { StartupId = nodi.Id, Description = "Credit purchase · 25 credits (order PO-7001)", Bucket = TokenBucket.Purchased, Amount = 25, Reference = "order:PO-7001", IdempotencyKey = "order:SBX-88213", CreatedAtUtc = now.AddDays(-20) },
            new TokenLedgerEntry { StartupId = nodi.Id, Description = "Promotion · 3 days", Bucket = TokenBucket.Purchased, Amount = -7, Reference = $"post:{posts[0].Id}", IdempotencyKey = "promo:seed:1", CreatedAtUtc = now.AddDays(-1.5) });
        db.Promotions.Add(new Promotion { PostId = posts[0].Id, StartupId = nodi.Id, Days = 3, CostTokens = 7, RemainingSeconds = (long)TimeSpan.FromDays(1.5).TotalSeconds, Status = PromotionStatus.Active, StartedAtUtc = now.AddDays(-1.5), LastTickUtc = now, Impressions = 2140, Clicks = 96, Engagements = 31 });

        // Manual verification queue.
        db.VerificationApplications.Add(new VerificationApplication
        {
            Subject = VerificationSubject.Startup, StartupId = startups[7].Id, Status = VerificationStatus.Pending, LicenceHolderType = LicenceHolderType.Individual,
            LicenceHolderName = "Ayesha Noor", SubmittedAtUtc = now.AddDays(-2), CreatedAtUtc = now.AddDays(-2),
            Documents = [new() { Kind = "NID", StorageKey = "demo-nid", ScanStatus = ScanStatus.Clean }, new() { Kind = "TradeLicence", StorageKey = "demo-licence", ScanStatus = ScanStatus.Clean }],
        });
        db.VerificationApplications.Add(new VerificationApplication
        {
            Subject = VerificationSubject.Investor, InvestorId = investors[5].Id, Status = VerificationStatus.Pending, SubmittedAtUtc = now.AddDays(-1), CreatedAtUtc = now.AddDays(-1),
            Documents = [new() { Kind = "Passport", StorageKey = "demo-passport", ScanStatus = ScanStatus.Clean }, new() { Kind = "Credential", StorageKey = "demo-credential", ScanStatus = ScanStatus.Clean }],
        });
        db.Reports.Add(new UserReport { Kind = ReportKind.Comment, TargetReference = "comment:1", Reason = "Spam / self-promotion", ReporterAccountId = investorUsers[3].Id, Status = ReportStatus.Open, CreatedAtUtc = now.AddDays(-6) });
        db.AdminAudit.Add(new AdminActionAudit { ActorAccountId = admins[1].Id, ActorRole = "VerificationAdmin", Action = "Approved startup verification", Target = "Nodi Freight", Reason = "NID + trade licence holder match", CreatedAtUtc = now.AddDays(-80) });
        await db.SaveChangesAsync(ct);
        await SeedPitchingAndProfilesAsync(founders, investorUsers, admins, startups, investors, rafi, posts, ct);
    }

    private static string Mitigation() => "Pitch delivered → Imran Chowdhury";
}
