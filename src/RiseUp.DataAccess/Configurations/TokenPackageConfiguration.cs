using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Payments;

namespace RiseUp.DataAccess.Configurations;

public sealed class TokenPackageConfiguration : IEntityTypeConfiguration<TokenPackage>
{
    public void Configure(EntityTypeBuilder<TokenPackage> b)
    {
        b.Property(x => x.PriceBdt).HasPrecision(18, 2);
    }
}
