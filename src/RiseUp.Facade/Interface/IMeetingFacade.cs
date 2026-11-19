using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Pitching;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Pitching;

public interface IMeetingFacade
{
    Task<IReadOnlyList<MeetingDto>> ListAsync(Actor actor, int conversationId, CancellationToken ct = default);
    Task<Result<int>> ProposeAsync(Actor actor, int conversationId, IReadOnlyList<DateTime> slotsUtc, string? agenda, CancellationToken ct = default);
    Task<Result> AcceptAsync(Actor actor, int meetingId, DateTime slotUtc, CancellationToken ct = default);
    Task<Result> CancelAsync(Actor actor, int meetingId, CancellationToken ct = default);
    Task<Result<(DateTime StartUtc, string Title, string Agenda)>> GetCalendarAsync(Actor actor, int meetingId, CancellationToken ct = default);
}
