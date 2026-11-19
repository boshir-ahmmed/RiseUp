using Microsoft.EntityFrameworkCore;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Media;
using RiseUp.Entities.Pitching;
using RiseUp.Entities.Profiles;
using RiseUp.Entities.Social;

namespace RiseUp.DataAccess.Repositories;

public sealed class MeetingRepository(RiseUpDbContext db) : Repository<Meeting>(db), IMeetingRepository
{
    public async Task<IReadOnlyList<Meeting>> ListForConversationAsync(int conversationId, CancellationToken ct = default) =>
        await Set.AsNoTracking().Where(m => m.ConversationId == conversationId).OrderByDescending(m => m.CreatedAtUtc).ToListAsync(ct);
}
