using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Startups;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Common;

public sealed class TeamGroupMembership(IConversationRepository conversations, IInvestorRepository investors, ActivityRecorder activity, IClock clock)
{
    public async Task AddAsync(Conversation group, TeamAccount member, CancellationToken ct)
    {
        var investor = await investors.GetByIdAsync(group.InvestorId, ct);
        var link = $"/conversations/{group.Id}";
        await conversations.AddMemberAsync(new ConversationMember { ConversationId = group.Id, Kind = MemberKind.Team,
            TeamAccountId = member.Id, AddedAtUtc = clock.UtcNow }, ct);
        await conversations.AddMessageAsync(new Message { ConversationId = group.Id, AuthorKind = MemberKind.Owner, AuthorName = "RiseUp",
            IsSystem = true, SentAtUtc = clock.UtcNow, Body = $"{member.FullName} ({member.Designation}) joined the conversation." }, ct);
        await activity.NotifyAccountAsync(investor?.AccountId, $"{member.FullName} was added to your conversation.", link, ct);
        await activity.NotifyTeamAsync(member.Id, $"You were added to the conversation with {investor?.DisplayName}.", link, ct);
    }
}
