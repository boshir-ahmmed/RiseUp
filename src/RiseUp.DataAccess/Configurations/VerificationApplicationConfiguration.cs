using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Verification;

namespace RiseUp.DataAccess.Configurations;

public sealed class VerificationApplicationConfiguration : IEntityTypeConfiguration<VerificationApplication>
{
    public void Configure(EntityTypeBuilder<VerificationApplication> b)
    {
        b.Property(x => x.Status).IsConcurrencyToken();
        b.Property(x => x.LicenceHolderName).HasMaxLength(150);
        b.Property(x => x.DecisionReason).HasMaxLength(500);
        b.HasIndex(x => new { x.Status, x.SubmittedAtUtc });
        b.HasMany(x => x.Documents).WithOne().HasForeignKey(d => d.ApplicationId).OnDelete(DeleteBehavior.Cascade);
    }
}
