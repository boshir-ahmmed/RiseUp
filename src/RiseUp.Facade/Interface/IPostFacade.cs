using FluentValidation;
using Microsoft.Extensions.Logging;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Promotions;
using RiseUp.Entities.Social;
using RiseUp.Facade.Common;
using RiseUp.Facade.Discovery;
using RiseUp.Facade.Media;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Posts;

public interface IPostFacade
{
    Task<Result<int>> SubmitAsync(Actor actor, SubmitPostRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<FeedItemDto>> GetFeedAsync(Actor actor, string tab, PostCategory? category, int take, CancellationToken ct = default);
    Task<Result<PostDetailDto>> GetDetailAsync(Actor actor, int postId, CancellationToken ct = default);
    Task<IReadOnlyList<FeedItemDto>> ListForStartupAsync(Actor actor, int startupId, CancellationToken ct = default);
    Task<IReadOnlyList<OwnPostDto>> ListOwnAsync(Actor actor, CancellationToken ct = default);
    Task<Result<bool>> ToggleLikeAsync(Actor actor, int postId, CancellationToken ct = default);
    Task<Result<int>> CommentAsync(Actor actor, int postId, int? parentCommentId, string body, CancellationToken ct = default);
    Task<Result> DeleteCommentAsync(Actor actor, int commentId, CancellationToken ct = default);
    Task<Result<string>> ShareAsync(Actor actor, int postId, CancellationToken ct = default);
    /// <summary>Private bookmark for any signed-in founder or investor.</summary>
    Task<Result<bool>> ToggleSaveAsync(Actor actor, int postId, CancellationToken ct = default);
    /// <summary>Adds a ≤90 s video (and poster frame) to an existing post of the caller's startup.</summary>
    Task<Result> AttachVideoAsync(Actor actor, int postId, UploadedFile video, UploadedFile? poster, CancellationToken ct = default);
}
