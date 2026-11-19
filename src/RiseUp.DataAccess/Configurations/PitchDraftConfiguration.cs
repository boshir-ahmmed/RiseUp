using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Common;

namespace RiseUp.DataAccess.Configurations;

public sealed class PitchDraftConfiguration : IEntityTypeConfiguration<PitchDraft>
{
    public void Configure(EntityTypeBuilder<PitchDraft> b)
    {
        b.Property(x => x.Text).HasMaxLength(3000).IsRequired();
        b.Property(x => x.ReasonCode).HasMaxLength(60);
        b.HasIndex(x => x.StartupId);
    }
}
