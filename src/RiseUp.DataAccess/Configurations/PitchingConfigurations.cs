using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Identity;
using RiseUp.Entities.Investors;
using RiseUp.Entities.Media;
using RiseUp.Entities.Pitching;
using RiseUp.Entities.Profiles;
using RiseUp.Entities.Social;
using RiseUp.Entities.Startups;

namespace RiseUp.DataAccess.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> b)
    {
        b.Property(x => x.StorageKey).HasMaxLength(64).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        b.Property(x => x.FileName).HasMaxLength(200).IsRequired();
        b.HasIndex(x => x.StorageKey).IsUnique();
        b.HasIndex(x => x.ScanStatus);
        b.HasIndex(x => new { x.OwnerStartupId, x.Kind });
    }
}

public sealed class MemberProfileConfiguration : IEntityTypeConfiguration<MemberProfile>
{
    public void Configure(EntityTypeBuilder<MemberProfile> b)
    {
        b.HasIndex(x => x.AccountId).IsUnique().HasFilter("[AccountId] IS NOT NULL");
        b.HasIndex(x => x.TeamAccountId).IsUnique().HasFilter("[TeamAccountId] IS NOT NULL");
        b.Property(x => x.Headline).HasMaxLength(120).IsRequired();
        b.Property(x => x.Location).HasMaxLength(80).IsRequired();
        b.Property(x => x.Bio).HasMaxLength(600).IsRequired();
        b.Property(x => x.LinkUrl).HasMaxLength(200);
        b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<TeamAccount>().WithMany().HasForeignKey(x => x.TeamAccountId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.PhotoMediaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.CoverMediaId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t => t.HasCheckConstraint("CK_MemberProfile_OneOwner", "([AccountId] IS NULL) <> ([TeamAccountId] IS NULL)"));
    }
}

public sealed class NameChangeRequestConfiguration : IEntityTypeConfiguration<NameChangeRequest>
{
    public void Configure(EntityTypeBuilder<NameChangeRequest> b)
    {
        // At most one pending name change per account, enforced by the database.
        b.HasIndex(x => x.AccountId).IsUnique().HasFilter("[Status] = 0").HasDatabaseName("UX_NameChange_PendingPerAccount");
        b.Property(x => x.FromName).HasMaxLength(150).IsRequired();
        b.Property(x => x.ToName).HasMaxLength(60).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(500);
        b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PitchPackageConfiguration : IEntityTypeConfiguration<PitchPackage>
{
    public void Configure(EntityTypeBuilder<PitchPackage> b)
    {
        b.HasIndex(x => x.StartupId).IsUnique();
        b.Property(x => x.AskAmountUsd).HasPrecision(18, 2);
        b.Property(x => x.ValuationUsd).HasPrecision(18, 2);
        b.Property(x => x.MinTicketUsd).HasPrecision(18, 2);
        b.Property(x => x.Highlights).HasMaxLength(1200).IsRequired();
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_PitchPackage_Discount", "[DiscountPercent] >= 0 AND [DiscountPercent] <= 50");
            t.HasCheckConstraint("CK_PitchPackage_Amounts", "[AskAmountUsd] >= 0 AND [ValuationUsd] >= 0 AND [MinTicketUsd] >= 0");
        });
        b.HasOne<Startup>().WithOne().HasForeignKey<PitchPackage>(x => x.StartupId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Slides).WithOne().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.UseOfFunds).WithOne().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Team).WithOne().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Faq).WithOne().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Documents).WithOne().HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PitchSlideConfiguration : IEntityTypeConfiguration<PitchSlide>
{
    public void Configure(EntityTypeBuilder<PitchSlide> b)
    {
        b.Property(x => x.Title).HasMaxLength(80).IsRequired();
        b.Property(x => x.Body).HasMaxLength(240).IsRequired();
        b.HasIndex(x => new { x.PackageId, x.Position });
    }
}

public sealed class UseOfFundsConfiguration : IEntityTypeConfiguration<UseOfFundsItem>
{
    public void Configure(EntityTypeBuilder<UseOfFundsItem> b) { b.Property(x => x.Label).HasMaxLength(40).IsRequired(); b.ToTable(t => t.HasCheckConstraint("CK_UseOfFunds_Percent", "[Percent] >= 0 AND [Percent] <= 100")); }
}

public sealed class PitchTeamMemberConfiguration : IEntityTypeConfiguration<PitchTeamMember>
{
    public void Configure(EntityTypeBuilder<PitchTeamMember> b) { b.Property(x => x.Name).HasMaxLength(80).IsRequired(); b.Property(x => x.Role).HasMaxLength(120).IsRequired(); }
}

public sealed class PitchFaqConfiguration : IEntityTypeConfiguration<PitchFaq>
{
    public void Configure(EntityTypeBuilder<PitchFaq> b) { b.Property(x => x.Question).HasMaxLength(200).IsRequired(); b.Property(x => x.Answer).HasMaxLength(600).IsRequired(); }
}

public sealed class PitchDocumentConfiguration : IEntityTypeConfiguration<PitchDocument>
{
    public void Configure(EntityTypeBuilder<PitchDocument> b)
    {
        b.Property(x => x.Label).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.MediaId).IsUnique();
        b.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.MediaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PitchQuestionConfiguration : IEntityTypeConfiguration<PitchQuestion>
{
    public void Configure(EntityTypeBuilder<PitchQuestion> b)
    {
        b.Property(x => x.AskerName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Text).HasMaxLength(300).IsRequired();
        b.Property(x => x.Answer).HasMaxLength(600);
        b.HasIndex(x => x.RequestId);
        b.HasOne<ConnectionRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PitchEngagementConfiguration : IEntityTypeConfiguration<PitchEngagement>
{
    public void Configure(EntityTypeBuilder<PitchEngagement> b)
    {
        b.HasIndex(x => x.RequestId).IsUnique();
        b.Property(x => x.DocumentsViewed).HasMaxLength(200).IsRequired();
        b.Ignore(x => x.SlidesViewed); b.Ignore(x => x.DocumentsViewedCount);
        b.HasOne<ConnectionRequest>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ShortlistEntryConfiguration : IEntityTypeConfiguration<ShortlistEntry>
{
    public void Configure(EntityTypeBuilder<ShortlistEntry> b)
    {
        b.HasIndex(x => new { x.InvestorId, x.StartupId }).IsUnique();
        b.Property(x => x.Note).HasMaxLength(300).IsRequired();
        b.HasOne<InvestorProfile>().WithMany().HasForeignKey(x => x.InvestorId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Startup>().WithMany().HasForeignKey(x => x.StartupId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> b)
    {
        b.Property(x => x.ProposedByName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Agenda).HasMaxLength(140).IsRequired();
        b.HasIndex(x => x.ConversationId);
        b.Ignore(x => x.Slots);
        b.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SavedPostConfiguration : IEntityTypeConfiguration<SavedPost>
{
    public void Configure(EntityTypeBuilder<SavedPost> b)
    {
        b.HasIndex(x => new { x.PostId, x.AccountId }).IsUnique();
        b.HasIndex(x => x.AccountId);
        b.HasOne<StartupPost>().WithMany().HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
    }
}
