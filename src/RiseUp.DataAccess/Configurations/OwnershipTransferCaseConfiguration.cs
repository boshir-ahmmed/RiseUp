using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Verification;
using RiseUp.Entities.Startups;

namespace RiseUp.DataAccess.Configurations;

public sealed class OwnershipTransferCaseConfiguration : IEntityTypeConfiguration<OwnershipTransferCase>
{
    public void Configure(EntityTypeBuilder<OwnershipTransferCase> b)
    {
        b.Property(x => x.IncomingOwnerName).HasMaxLength(150).IsRequired();
        b.Property(x => x.IncomingOwnerEmail).HasMaxLength(256).IsRequired();
        b.Property(x => x.DecisionReason).HasMaxLength(500);
        b.HasOne<Startup>().WithMany().HasForeignKey(x => x.StartupId).OnDelete(DeleteBehavior.Cascade);
    }
}
