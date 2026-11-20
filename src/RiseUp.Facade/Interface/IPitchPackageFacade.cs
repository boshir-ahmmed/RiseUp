using FluentValidation;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Media;
using RiseUp.Entities.Pitching;
using RiseUp.Entities.Startups;
using RiseUp.Facade.Common;
using RiseUp.Facade.Discovery;
using RiseUp.Facade.Media;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Pitching;

public interface IPitchPackageFacade
{
    Task<(string Name, VideoDto? Teaser, bool Public)> GetTeaserSettingsAsync(Actor actor, CancellationToken ct = default);
    Task<VideoDto?> GetPublicTeaserAsync(int startupId, CancellationToken ct = default);
    Task<Result<int>> MoveTeaserToDraftAsync(Actor actor, CancellationToken ct = default);
    Task<Result<PitchPackageDto>> GetOwnAsync(Actor actor, CancellationToken ct = default);
    Task<Result> SetVideoAsync(Actor actor, PitchVideoSlot slot, UploadedFile video, UploadedFile? poster, CancellationToken ct = default);
    Task<Result> RemoveVideoAsync(Actor actor, PitchVideoSlot slot, CancellationToken ct = default);
    Task<Result> SetVisibilityAsync(Actor actor, bool? publicTeaser, bool? publicDemo, bool? captions, CancellationToken ct = default);
    Task<Result> SetDeckAsync(Actor actor, UploadedFile deck, CancellationToken ct = default);
    Task<Result> RemoveDeckAsync(Actor actor, CancellationToken ct = default);
    Task<Result> SaveSlidesAsync(Actor actor, IReadOnlyList<SlideDto> slides, CancellationToken ct = default);
    Task<Result> AddDocumentAsync(Actor actor, UploadedFile file, string label, CancellationToken ct = default);
    Task<Result> RemoveDocumentAsync(Actor actor, int documentId, CancellationToken ct = default);
    Task<Result> SetDocumentDownloadAsync(Actor actor, int documentId, bool allow, CancellationToken ct = default);
    Task<Result> SaveAskAsync(Actor actor, AskInput ask, CancellationToken ct = default);
    Task<Result> SaveStoryAsync(Actor actor, IReadOnlyList<string> highlights, IReadOnlyList<TeamDto> team, IReadOnlyList<FaqDto> faq, CancellationToken ct = default);
    /// <summary>Public teasers for the feed spotlight and Reels. The ask and metrics are included ONLY for verified investors.</summary>
    Task<IReadOnlyList<SpotlightDto>> ListSpotlightAsync(Actor actor, int take, CancellationToken ct = default);
}
