using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Common;

namespace RiseUp.DataAccess.Configurations;

public sealed class StartupInvestorConnectionConfiguration : IEntityTypeConfiguration<StartupInvestorConnection>
{
    public void Configure(EntityTypeBuilder<StartupInvestorConnection> b)
    {
        b.HasIndex(x => new { x.StartupId, x.InvestorId }).IsUnique();   // one connection per pair (FR-090)
        b.HasIndex(x => x.ConversationId).IsUnique();
    }
}
