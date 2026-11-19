using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Conversations;

namespace RiseUp.DataAccess.Configurations;

public sealed class ConversationMemberConfiguration : IEntityTypeConfiguration<ConversationMember>
{
    public void Configure(EntityTypeBuilder<ConversationMember> b)
    {
        b.HasIndex(x => new { x.ConversationId, x.Kind, x.AccountId, x.TeamAccountId });
        b.HasIndex(x => new { x.TeamAccountId, x.RemovedAtUtc });
    }
}
