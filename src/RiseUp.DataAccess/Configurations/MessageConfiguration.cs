using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiseUp.Entities.Conversations;

namespace RiseUp.DataAccess.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        b.Property(x => x.AuthorName).HasMaxLength(150).IsRequired();
        b.HasIndex(x => new { x.ConversationId, x.SentAtUtc });
        b.HasOne<Conversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
    }
}
