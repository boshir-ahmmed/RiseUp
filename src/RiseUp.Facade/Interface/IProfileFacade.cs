using System.Text.RegularExpressions;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Profiles;
using RiseUp.Facade.Common;
using RiseUp.Facade.Media;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Profiles;

public interface IProfileFacade
{
    Task<ShellDto> GetShellAsync(Actor actor, CancellationToken ct = default);
    Task<Result<MyProfileDto>> GetMineAsync(Actor actor, CancellationToken ct = default);
    Task<Result> UpdateAsync(Actor actor, UpdateProfileRequest request, CancellationToken ct = default);
    Task<Result> SetPhotoAsync(Actor actor, UploadedFile file, CancellationToken ct = default);
    Task<Result> RemovePhotoAsync(Actor actor, CancellationToken ct = default);
    Task<Result> SetCoverAsync(Actor actor, UploadedFile file, CancellationToken ct = default);
    Task<Result> RemoveCoverAsync(Actor actor, CancellationToken ct = default);
    Task<Result> SetStartupImageAsync(Actor actor, MediaKind kind, UploadedFile file, CancellationToken ct = default);
    Task<Result> RemoveStartupImageAsync(Actor actor, MediaKind kind, CancellationToken ct = default);
    Task<Result> CancelNameChangeAsync(Actor actor, CancellationToken ct = default);
    Task<IReadOnlyList<NameChangeDto>> ListNameChangesAsync(Actor admin, CancellationToken ct = default);
    Task<Result> DecideNameChangeAsync(Actor admin, int requestId, bool approve, string? reason, CancellationToken ct = default);
}
