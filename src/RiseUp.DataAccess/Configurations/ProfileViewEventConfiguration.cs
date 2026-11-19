using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Startups;

namespace RiseUp.DataAccess.Configurations;

public sealed class ProfileViewEventConfiguration : IEntityTypeConfiguration<ProfileViewEvent>
{
    public void Configure(EntityTypeBuilder<ProfileViewEvent> b)
    {
        b.Property(x => x.ViewerKey).HasMaxLength(100).IsRequired();
        b.HasIndex(x => new { x.StartupId, x.ViewerKey, x.ViewedAtUtc });
        b.HasOne<Startup>().WithMany().HasForeignKey(x => x.StartupId).OnDelete(DeleteBehavior.Cascade);
    }
}
