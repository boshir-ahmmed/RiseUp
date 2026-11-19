using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Pitching;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Pitching;

public interface IPitchRoomFacade
{
    /// <summary>requestId 0 = the founder's own preview ("exactly what an investor sees").</summary>
    Task<Result<PitchRoomDto>> GetAsync(Actor actor, int requestId, CancellationToken ct = default);
    Task<Result> TrackAsync(Actor actor, int requestId, TrackInput input, CancellationToken ct = default);
    Task<Result> AskAsync(Actor actor, int requestId, string text, CancellationToken ct = default);
    Task<Result> AnswerAsync(Actor actor, int questionId, string text, CancellationToken ct = default);
    Task<Result> RequestMoreInfoAsync(Actor actor, int requestId, string message, CancellationToken ct = default);
    Task<IReadOnlyList<SentPitchDto>> ListSentAsync(Actor actor, CancellationToken ct = default);
}
