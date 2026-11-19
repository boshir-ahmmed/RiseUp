using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Social;
using RiseUp.Entities.Startups;

namespace RiseUp.DataAccess.Configurations;

public sealed class StartupPostConfiguration : IEntityTypeConfiguration<StartupPost>
{
    public void Configure(EntityTypeBuilder<StartupPost> b)
    {
        b.Property(x => x.LiveTitle).HasMaxLength(150);
        b.Property(x => x.LiveBody).HasMaxLength(5000);
        b.Property(x => x.PendingTitle).HasMaxLength(150);
        b.Property(x => x.PendingBody).HasMaxLength(5000);
        b.Property(x => x.PendingReasonCode).HasMaxLength(60);
        b.Property(x => x.ImageStorageKey).HasMaxLength(200);
        b.HasIndex(x => new { x.StartupId, x.PublishedAtUtc });
        b.HasIndex(x => x.VideoMediaId);
        b.HasIndex(x => new { x.IsHidden, x.Category, x.PublishedAtUtc });
        b.HasOne<Startup>().WithMany().HasForeignKey(x => x.StartupId).OnDelete(DeleteBehavior.Cascade);
    }
}
