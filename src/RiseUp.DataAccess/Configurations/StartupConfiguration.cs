using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Startups;
using RiseUp.Entities.Identity;

namespace RiseUp.DataAccess.Configurations;

public sealed class StartupConfiguration : IEntityTypeConfiguration<Startup>
{
    public void Configure(EntityTypeBuilder<Startup> b)
    {
        b.HasIndex(x => x.OwnerAccountId).IsUnique();              // one startup per founder (FR-023)
        b.HasIndex(x => x.NormalizedLegalName);
        b.HasIndex(x => new { x.VerificationStatus, x.IsSuspended, x.IsDeleted });
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.NormalizedLegalName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Tagline).HasMaxLength(120).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        b.Property(x => x.Industry).HasMaxLength(60).IsRequired();
        b.Property(x => x.Location).HasMaxLength(80).IsRequired();
        b.Property(x => x.BusinessModel).HasMaxLength(200);
        b.Property(x => x.Website).HasMaxLength(200);
        b.Property(x => x.Tags).HasMaxLength(300);
        b.Property(x => x.FundingGoalUsd).HasPrecision(18, 2);
        b.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.OwnerAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Metrics).WithOne().HasForeignKey(m => m.StartupId).OnDelete(DeleteBehavior.Cascade);
    }
}
