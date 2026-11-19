using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Common;

namespace RiseUp.DataAccess.Configurations;

public sealed class InvitationDailyCounterConfiguration : IEntityTypeConfiguration<InvitationDailyCounter>
{
    public void Configure(EntityTypeBuilder<InvitationDailyCounter> b)
    {
        b.HasIndex(x => new { x.InvestorId, x.DhakaDate }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("CK_InvitationCounter_Range", "[Count] >= 0 AND [Count] <= 10"));
    }
}
