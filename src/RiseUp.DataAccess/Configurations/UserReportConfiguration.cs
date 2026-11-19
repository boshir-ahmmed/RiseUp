using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Governance;

namespace RiseUp.DataAccess.Configurations;

public sealed class UserReportConfiguration : IEntityTypeConfiguration<UserReport>
{
    public void Configure(EntityTypeBuilder<UserReport> b)
    {
        b.Property(x => x.TargetReference).HasMaxLength(100).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.Property(x => x.ResolutionNote).HasMaxLength(500);
        b.HasIndex(x => new { x.Status, x.CreatedAtUtc });
    }
}
