using System.Text;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Identity;
using RiseUp.Entities.Investors;
using RiseUp.Entities.Media;
using RiseUp.Entities.Pitching;
using RiseUp.Entities.Profiles;
using RiseUp.Entities.Social;
using RiseUp.Entities.Startups;
using Microsoft.EntityFrameworkCore;

namespace RiseUp.DataAccess.Seed;

public sealed class SeedOptions
{
    /// <summary>Folder with the demo pitch videos (resolved against the content root by Web.UI). Missing folder = packages without videos.</summary>
    public string DemoMediaPath { get; set; } = "DemoMedia";
}

/// <summary>v3/v4 demo data: personal profiles, full pitch packages with demo videos, Q&amp;A, engagement, shortlist and video posts.</summary>
public sealed partial class DemoDataSeeder
{
    private sealed record PackageText(string Key, string Problem, string Solution, string Market, string Model, (string, string)[] Team, decimal Amount,
        FundingInstrument Instrument, decimal Valuation, int Discount, int Runway, decimal MinTicket, (string, int)[] Use, string[] Highlights, (string, string)[] Faq);

    private static readonly PackageText[] Packages =
    [
        new("nodi", "38% of trucks drive back empty; SME shippers wait days for a quote.", "Real-time quotes from vetted carriers, digital proof of delivery, mobile-money settlement.",
            "$4.1B domestic road freight; 68% moved by SMEs without contracts.", "8% commission per load + ৳2,500/month fleet SaaS.",
            [("Farhan Kabir", "CEO · ex-Bengal Logistics ops"), ("Rafi Ahmed", "Head of Operations"), ("Meem Akter", "Finance Lead")], 600000, FundingInstrument.Safe, 4500000, 20, 18, 25000,
            [("Corridor expansion", 45), ("Carrier app", 30), ("Working capital", 25)], ["1,240 active shippers, +14% MoM", "19% fewer empty return trips on Dhaka–Chattogram", "Payback on shipper acquisition: 3.2 months"],
            [("Why now?", "Digital payments adoption among truckers crossed 60% in 2025; settlement no longer needs cash."), ("Competition?", "Informal brokers. No digital player covers river-port launches."), ("Exit?", "Regional logistics consolidators; two inbound conversations this year.")]),
        new("taka", "8 million neighbourhood shops keep paper ledgers and are invisible to lenders.", "Voice-entry Bangla ledger that becomes an alternative credit score for partner MFIs.",
            "$2.3B unmet SME micro-credit demand.", "Freemium app + per-approval API fee from lenders.", [("Nabila Rahman", "CEO · ex-microfinance"), ("Hasan Mahmud", "CTO")],
            250000, FundingInstrument.Safe, 2000000, 20, 16, 10000, [("Lender integrations", 40), ("Field onboarding", 35), ("Product", 25)],
            ["8,900 shops, 5,100 monthly active", "3 lender partners live, 412 loans approved", "Default rate on scored loans: 1.8%"], [("Data privacy?", "Shop owners opt in per lender; scores are revocable.")]),
        new("shasthyo", "Rural patients travel 4+ hours for a specialist opinion.", "Triage workflow, specialist video consults and next-day medicine delivery for upazila clinics.",
            "64 million rural Bangladeshis, 2,900 upazila clinics.", "৳120 per consultation + 12% pharmacy margin.", [("Dr. Mahin Siddiqui", "Founder · MBBS, MPH"), ("Sumaiya Islam", "Head of Clinical Ops")],
            900000, FundingInstrument.Equity, 6000000, 0, 20, 50000, [("Clinic rollout", 50), ("Pharmacy logistics", 30), ("Clinical team", 20)],
            ["64 clinics, 11,400 consults/month", "41% repeat rate", "Median specialist wait: 22 minutes"], [("Regulation?", "Operates under national telemedicine guidelines.")]),
        new("pathshala", "Coding courses are in English and need fast internet; most schools have neither.", "Offline-first Bangla curriculum with gamified projects and teacher training.",
            "20,000 secondary schools; ICT is now compulsory.", "৳45,000 per school per year.", [("Sabbir Hasan", "Founder · ex-teacher"), ("Priya Das", "Curriculum lead")],
            150000, FundingInstrument.Safe, 1200000, 20, 14, 5000, [("Curriculum", 40), ("Teacher training", 35), ("Offline app", 25)],
            ["37 schools, 6,800 learners", "1,900 student projects this term", "National ed-innovation challenge winner"], [("Why Bangla?", "Comprehension scores rose 2.1x versus the English pilot.")]),
        new("sobuj", "2.5 million char-island residents have no grid and burn kerosene.", "Containerised solar micro-grids with smart meters and mobile-money top-ups.",
            "400+ chars; $180M annual kerosene spend.", "Energy-as-a-service, prepaid.", [("Ishrat Jahan", "CEO · ex-energy finance"), ("Tanzil Ahmed", "Grid engineering")],
            1200000, FundingInstrument.Equity, 7500000, 0, 24, 50000, [("3 new micro-grids", 60), ("Smart meters", 25), ("Ops team", 15)],
            ["4,300 households, 98.6% uptime", "ARPU $4.10, 92% mobile-money top-ups", "Blended-finance pilot approved for 3 islands"], [("Monsoon risk?", "Containers are raised 2.4 m; 2025 season had zero flood downtime.")]),
        new("haat", "City retailers pay four middlemen; haat traders receive 60% of the price.", "B2B app: order by 8 pm, delivered by 7 am from rural haats.",
            "$11B FMCG + fresh wholesale in Dhaka and Chattogram.", "Take rate 7% + logistics fee.", [("Rezaul Karim", "CEO"), ("Nasrin Akter", "Supply chain"), ("Imtiaz Hossain", "CTO")],
            3000000, FundingInstrument.Equity, 18000000, 0, 24, 250000, [("Chattogram hub", 40), ("Sylhet launch", 35), ("Tech", 25)],
            ["$9.4M annual GMV, 14,200 retailers", "Contribution margin 6.2%", "Hub utilisation 81%"], [("Perishables?", "Cold room at the hub; spoilage under 1.4%.")]),
        new("jute", "Garment exporters ship billions of plastic trays a year to EU buyers who now refuse them.", "Moulded jute-fibre trays with batch-level traceability.",
            "EU packaging rules phase out single-use plastic trays by 2030.", "B2B supply contracts, ৳11 per tray.", [("Tasnim Ara", "Founder · textile engineer"), ("Arif Mahmud", "Plant manager")],
            400000, FundingInstrument.ConvertibleNote, 0, 25, 18, 20000, [("Second press line", 55), ("Certification", 25), ("Sales", 20)],
            ["5 pilot buyers, 60k units/month", "31% less packaging weight per shipment", "EU buyer LOIs covering 1.1M trays"], [("Certification?", "Textile certification in progress.")]),
    ];

    private async Task<MediaAsset?> ImportAsync(string fileName, MediaKind kind, string contentType, int? seconds, int? startupId, int? ownerAccountId, CancellationToken ct)
    {
        var path = Path.Combine(seedOptions.Value.DemoMediaPath, fileName);
        if (!File.Exists(path)) return null;
        await using var source = File.OpenRead(path);
        var key = await SavePrivateAsync(source, fileName, ct);
        var asset = new MediaAsset { Kind = kind, StorageKey = key, ContentType = contentType, FileName = fileName, SizeBytes = new FileInfo(path).Length,
            DurationSeconds = seconds, OwnerStartupId = startupId, OwnerAccountId = ownerAccountId, ScanStatus = ScanStatus.Clean };
        db.MediaAssets.Add(asset);
        return asset;
    }

    private async Task<MediaAsset> PdfAsync(string fileName, string title, string[] lines, int startupId, int ownerAccountId, CancellationToken ct)
    {
        var bytes = MinimalPdf(title, lines);
        var key = await SavePrivateAsync(new MemoryStream(bytes), fileName, ct);
        var asset = new MediaAsset { Kind = MediaKind.PitchDocument, StorageKey = key, ContentType = "application/pdf", FileName = fileName, SizeBytes = bytes.Length,
            OwnerStartupId = startupId, OwnerAccountId = ownerAccountId, ScanStatus = ScanStatus.Clean };
        db.MediaAssets.Add(asset);
        return asset;
    }

    private async Task SeedPitchingAndProfilesAsync(UserAccount[] founders, UserAccount[] investorUsers, UserAccount[] admins, Startup[] startups, InvestorProfile[] investors,
        TeamAccount rafi, StartupPost[] posts, CancellationToken ct)
    {
        var now = clock.UtcNow;
        // ---- personal profiles
        string[] founderBios = ["Building digital freight rails for Bangladesh's SME shippers. Previously ran operations at a regional logistics company. Ask me about corridor economics and river-port launches.", "", "", "", "", "", "", "First-time founder building for smallholder farmers."];
        for (var i = 0; i < founders.Length; i++)
            db.MemberProfiles.Add(new MemberProfile { AccountId = founders[i].Id, Headline = $"Founder & CEO · {startups[i].Name}", Location = $"{startups[i].Location}, Bangladesh", Bio = founderBios[i], CreatedAtUtc = now });
        for (var i = 0; i < investorUsers.Length; i++)
            db.MemberProfiles.Add(new MemberProfile { AccountId = investorUsers[i].Id, Headline = $"{(investors[i].InvestorType == "Venture fund" ? "Partner" : investors[i].InvestorType)} · {investors[i].Affiliation}", Location = investors[i].Location, Bio = investors[i].Bio, CreatedAtUtc = now });
        foreach (var a in admins) db.MemberProfiles.Add(new MemberProfile { AccountId = a.Id, Headline = $"{a.AdminRole} · RiseUp", Location = "Dhaka", CreatedAtUtc = now });
        db.MemberProfiles.Add(new MemberProfile { TeamAccountId = rafi.Id, Headline = $"{rafi.Designation} · {startups[0].Name}", CreatedAtUtc = now });

        // ---- pitch packages (the 7 verified demo startups)
        var packagesByStartup = new Dictionary<int, PitchPackage>();
        for (var i = 0; i < Packages.Length; i++)
        {
            var t = Packages[i]; var s = startups[i]; var owner = founders[i].Id;
            var pitch = await ImportAsync($"{t.Key}-pitch.mp4", MediaKind.PitchVideo, "video/mp4", 30, s.Id, owner, ct);
            var demo = await ImportAsync($"{t.Key}-demo.mp4", MediaKind.DemoVideo, "video/mp4", 18, s.Id, owner, ct);
            var teaser = await ImportAsync($"{t.Key}-teaser.mp4", MediaKind.TeaserVideo, "video/mp4", 12, s.Id, owner, ct);
            var pp = await ImportAsync($"{t.Key}-pitch.jpg", MediaKind.VideoPoster, "image/jpeg", null, s.Id, owner, ct);
            var dp = await ImportAsync($"{t.Key}-demo.jpg", MediaKind.VideoPoster, "image/jpeg", null, s.Id, owner, ct);
            var tp = await ImportAsync($"{t.Key}-teaser.jpg", MediaKind.VideoPoster, "image/jpeg", null, s.Id, owner, ct);
            var onePager = await PdfAsync($"{s.Name.Replace(' ', '_')}_One_Pager.pdf", $"{s.Name} - one-pager", [s.Tagline, t.Problem, t.Solution, t.Market, t.Model], s.Id, owner, ct);
            var traction = await PdfAsync($"{s.Name.Replace(' ', '_')}_Traction_Q3.pdf", $"{s.Name} - traction report", t.Highlights, s.Id, owner, ct);
            await db.SaveChangesAsync(ct);
            var pkg = new PitchPackage
            {
                StartupId = s.Id, PackageVersion = 3, PitchVideoId = pitch?.Id, DemoVideoId = demo?.Id, TeaserVideoId = teaser?.Id, PitchPosterId = pp?.Id, DemoPosterId = dp?.Id,
                TeaserPosterId = tp?.Id, PublicTeaser = teaser is not null, PublicDemo = demo is not null, CaptionsAvailable = true,
                AskAmountUsd = t.Amount, Instrument = t.Instrument, ValuationUsd = t.Valuation, DiscountPercent = t.Discount, RunwayMonths = t.Runway, MinTicketUsd = t.MinTicket,
                Highlights = string.Join('\n', t.Highlights), CreatedAtUtc = now.AddDays(-10),
                Slides = [new() { Position = 0, Title = s.Name, Body = s.Tagline }, new() { Position = 1, Title = "The problem", Body = t.Problem }, new() { Position = 2, Title = "Our solution", Body = t.Solution },
                    new() { Position = 3, Title = "Market", Body = t.Market }, new() { Position = 4, Title = "Traction", Body = string.Join(" · ", t.Highlights) }, new() { Position = 5, Title = "Business model", Body = t.Model },
                    new() { Position = 6, Title = "Team", Body = string.Join(" · ", t.Team.Select(m => $"{m.Item1} — {m.Item2}")) }, new() { Position = 7, Title = "The ask", Body = $"${t.Amount:N0}" }],
                UseOfFunds = t.Use.Select(u => new UseOfFundsItem { Label = u.Item1, Percent = u.Item2 }).ToList(),
                Team = t.Team.Select(m => new PitchTeamMember { Name = m.Item1, Role = m.Item2 }).ToList(),
                Faq = t.Faq.Select(f => new PitchFaq { Question = f.Item1, Answer = f.Item2 }).ToList(),
                Documents = [new() { MediaId = onePager.Id, Label = "One-pager", AllowDownload = true }, new() { MediaId = traction.Id, Label = "Traction report", AllowDownload = false }],
            };
            db.PitchPackages.Add(pkg);
            packagesByStartup[s.Id] = pkg;
        }
        await db.SaveChangesAsync(ct);

        // ---- pitches already in the system get a package snapshot, engagement and Q&A
        var sadia = investors[0];
        foreach (var r in await db.ConnectionRequests.Where(r => r.Type == RequestType.Pitch).ToListAsync(ct))
        {
            if (!packagesByStartup.TryGetValue(r.StartupId, out var pkg)) continue;
            r.PackageVersion = pkg.PackageVersion;
            r.AttachedDocumentIds = string.Join(',', pkg.Documents.Select(d => d.Id));
        }
        var takaPitch = await db.ConnectionRequests.FirstAsync(r => r.IdempotencyKey == "seed:r4", ct);
        db.PitchEngagements.Add(new PitchEngagement { RequestId = takaPitch.Id, OpenedAtUtc = now.AddDays(-1.5), PitchWatchPercent = 72, DemoWatchPercent = 40, SlidesViewedMask = 0b11111,
            DocumentsViewed = packagesByStartup[startups[1].Id].Documents[0].Id.ToString(), CreatedAtUtc = now.AddDays(-1.5) });
        db.PitchQuestions.Add(new PitchQuestion { RequestId = takaPitch.Id, AskedByAccountId = investorUsers[0].Id, AskerName = "Sadia Haque", Text = "What share of scored loans came from repeat shops?",
            Answer = "58% — repeat shops carry 2.4x the ledger history.", AnsweredAtUtc = now.AddDays(-1.2), CreatedAtUtc = now.AddDays(-1.4) });
        var nodiPitch = await db.ConnectionRequests.FirstAsync(r => r.IdempotencyKey == "seed:r0", ct);
        db.PitchEngagements.Add(new PitchEngagement { RequestId = nodiPitch.Id, OpenedAtUtc = now.AddDays(-39.5), PitchWatchPercent = 100, DemoWatchPercent = 100, SlidesViewedMask = 0xFF,
            DocumentsViewed = string.Join(',', packagesByStartup[startups[0].Id].Documents.Select(d => d.Id)), CreatedAtUtc = now.AddDays(-39.5) });
        // A fresh pending pitch from Jute Loop to Sadia so the investor demo has a pitch room to open.
        var jute = startups[6];
        var juteWallet = await db.Wallets.FirstAsync(w => w.StartupId == jute.Id, ct);
        juteWallet.WelcomeBalance--;
        db.TokenLedger.Add(new Entities.Wallets.TokenLedgerEntry { StartupId = jute.Id, Description = "Pitch delivered → Sadia Haque", Bucket = TokenBucket.Welcome, Amount = -1, Reference = "seed:r5", IdempotencyKey = "pitch:seed:r5", CreatedAtUtc = now.AddDays(-13.8) });
        db.ConnectionRequests.Add(new ConnectionRequest { Type = RequestType.Pitch, StartupId = jute.Id, InvestorId = sadia.Id, Message = "Jute Loop supplies export-grade moulded jute packaging. Seeking $400k for a second press line — EU buyers have signed LOIs for 1.1M trays.",
            SentAtUtc = now.AddDays(-13.8), ExpiresAtUtc = now.AddDays(0.2), Status = RequestStatus.Pending, TokenBucket = TokenBucket.Welcome, IdempotencyKey = "seed:r5", PackageVersion = 3,
            AttachedDocumentIds = string.Join(',', packagesByStartup[jute.Id].Documents.Select(d => d.Id)), CreatedAtUtc = now.AddDays(-13.8) });

        // ---- investor shortlist (private)
        db.Shortlist.AddRange(
            new ShortlistEntry { InvestorId = sadia.Id, StartupId = startups[2].Id, Stage = ShortlistStage.Watching, Note = "Strong clinical ops; wait for Series A metrics.", CreatedAtUtc = now.AddDays(-9) },
            new ShortlistEntry { InvestorId = sadia.Id, StartupId = startups[1].Id, Stage = ShortlistStage.PitchReceived, Note = "Good repeat-shop data. Ask about lender concentration.", CreatedAtUtc = now.AddDays(-1.4) },
            new ShortlistEntry { InvestorId = sadia.Id, StartupId = startups[0].Id, Stage = ShortlistStage.InConversation, Note = "Corridor economics call next week.", CreatedAtUtc = now.AddDays(-38) });

        // ---- video posts in the feed
        async Task VideoPost(Startup s, int owner, string key, int secs, PostCategory c, string title, string body, double daysAgo)
        {
            var v = await ImportAsync($"{key}.mp4", MediaKind.PostVideo, "video/mp4", secs, s.Id, owner, ct);
            var p = await ImportAsync($"{key}.jpg", MediaKind.VideoPoster, "image/jpeg", null, s.Id, owner, ct);
            if (v is null) return;
            await db.SaveChangesAsync(ct);
            db.Posts.Add(new StartupPost { StartupId = s.Id, Category = c, LiveTitle = title, LiveBody = body, PendingState = ModerationState.Approved, PublishedAtUtc = now.AddDays(-daysAgo),
                CreatedAtUtc = now.AddDays(-daysAgo), ViewCount = 900, VideoMediaId = v.Id, PosterMediaId = p?.Id, VideoSeconds = secs });
        }
        await VideoPost(startups[0], founders[0].Id, "nodi-demo", 18, PostCategory.Products, "See Nodi Freight match a load in 40 seconds", "A shipper posts Dhaka → Chattogram, three vetted carriers quote, proof of delivery lands the same evening. Full product demo.", 0.4);
        await VideoPost(startups[4], founders[4].Id, "sobuj-teaser", 12, PostCategory.Milestones, "Char Kukri is online — 412 homes, zero kerosene", "Our third micro-grid went live this week. Watch the short story from the island.", 1.2);
        await VideoPost(startups[5], founders[5].Id, "haat-demo", 18, PostCategory.Products, "Order by 8 pm, delivered by 7 am", "How Haat Bazar Direct moves produce from rural haats to 14,200 city retailers overnight.", 2.3);
        await db.SaveChangesAsync(ct);
        _ = posts;
    }

    /// <summary>Builds a small, valid single-page PDF (Helvetica text) so demo pitch documents open in any viewer.</summary>
    private static byte[] MinimalPdf(string title, IEnumerable<string> lines)
    {
        static string Esc(string s) => new string(s.Where(c => c < 128).ToArray()).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var content = new StringBuilder("BT /F1 20 Tf 60 760 Td (" + Esc(title) + ") Tj /F1 12 Tf 0 -36 Td ");
        foreach (var l in lines) content.Append("(" + Esc(l) + ") Tj 0 -22 Td ");
        content.Append("0 -30 Td /F1 9 Tf (RiseUp demo document - fictional data) Tj ET");
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
        };
        var sb = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Length; i++) { offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString())); sb.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); }
        var xref = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var o in offsets) sb.Append($"{o:D10} 00000 n \n");
        sb.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }
}
