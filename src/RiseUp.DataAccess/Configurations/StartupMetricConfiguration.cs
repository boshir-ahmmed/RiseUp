using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Startups;

namespace RiseUp.DataAccess.Configurations;

public sealed class StartupMetricConfiguration : IEntityTypeConfiguration<StartupMetric>
{
    public void Configure(EntityTypeBuilder<StartupMetric> b)
    {
        b.Property(x => x.Name).HasMaxLength(80).IsRequired();
        b.Property(x => x.Value).HasMaxLength(80).IsRequired();
    }
}
