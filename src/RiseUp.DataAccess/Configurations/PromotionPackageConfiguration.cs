using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Payments;

namespace RiseUp.DataAccess.Configurations;

public sealed class PromotionPackageConfiguration : IEntityTypeConfiguration<PromotionPackage>
{
    public void Configure(EntityTypeBuilder<PromotionPackage> b)
    {
        b.HasIndex(x => x.Days).IsUnique();
    }
}
