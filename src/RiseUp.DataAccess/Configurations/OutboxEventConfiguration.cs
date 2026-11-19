using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Governance;

namespace RiseUp.DataAccess.Configurations;

public sealed class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> b)
    {
        b.Property(x => x.Type).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.ProcessedAtUtc);
    }
}
