using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Common;

namespace RiseUp.DataAccess.Configurations;

public sealed class BlockRelationshipConfiguration : IEntityTypeConfiguration<BlockRelationship>
{
    public void Configure(EntityTypeBuilder<BlockRelationship> b)
    {
        b.HasIndex(x => new { x.StartupId, x.InvestorId }).IsUnique();
    }
}
