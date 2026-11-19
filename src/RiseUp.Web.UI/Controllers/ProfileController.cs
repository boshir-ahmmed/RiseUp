using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Facade.Profiles;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Web.UI.Infrastructure;

namespace RiseUp.Web.UI.Controllers;

/// <summary>Personal profile (everyone signed in) and startup branding images (Main Startup Owner). Rules live in the Facade.</summary>
[Authorize]
public sealed class ProfileController(IProfileFacade profiles, IFileStorage storage) : AppController
{
    [HttpGet("/" + UrlRewrite.Me)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var r = await profiles.GetMineAsync(Actor, ct);
        return r.IsFailure ? Forbid() : View(r.Value);
    }

    [HttpGet("/" + UrlRewrite.EditProfile)]
    public async Task<IActionResult> Edit(CancellationToken ct)
    {
        var r = await profiles.GetMineAsync(Actor, ct);
        return r.IsFailure ? Forbid() : View(r.Value);
    }

    [HttpPost("/" + UrlRewrite.EditProfile)]
    public async Task<IActionResult> Save(UpdateProfileRequest request, CancellationToken ct)
    {
        Flash(await profiles.UpdateAsync(Actor, request, ct));
        return Redirect("/me/edit");
    }

    [HttpPost("/me/name/cancel")]
    public async Task<IActionResult> CancelName(CancellationToken ct) { Flash(await profiles.CancelNameChangeAsync(Actor, ct)); return Redirect("/me/edit"); }

    // Cropped images arrive from cropper.js as JPEG blobs; the server re-validates everything.
    [HttpPost("/me/photo"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Photo(IFormFile? file, CancellationToken ct) =>
        await ImageAsync(file, f => profiles.SetPhotoAsync(Actor, f, ct), ct);

    [HttpPost("/me/photo/remove")]
    public async Task<IActionResult> RemovePhoto(CancellationToken ct) => Back(await profiles.RemovePhotoAsync(Actor, ct), nameof(Edit));

    [HttpPost("/me/cover"), RequestSizeLimit(9 * 1024 * 1024)]
    public async Task<IActionResult> Cover(IFormFile? file, CancellationToken ct) =>
        await ImageAsync(file, f => profiles.SetCoverAsync(Actor, f, ct), ct);

    [HttpPost("/me/cover/remove")]
    public async Task<IActionResult> RemoveCover(CancellationToken ct) => Back(await profiles.RemoveCoverAsync(Actor, ct), nameof(Edit));

    [Authorize(Policy = Policies.Founder), HttpPost("/settings/startup/logo"), RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<IActionResult> Logo(IFormFile? file, CancellationToken ct) =>
        await ImageAsync(file, f => profiles.SetStartupImageAsync(Actor, MediaKind.StartupLogo, f, ct), ct);

    [Authorize(Policy = Policies.Founder), HttpPost("/settings/startup/cover"), RequestSizeLimit(9 * 1024 * 1024)]
    public async Task<IActionResult> StartupCover(IFormFile? file, CancellationToken ct) =>
        await ImageAsync(file, f => profiles.SetStartupImageAsync(Actor, MediaKind.StartupCover, f, ct), ct);

    [Authorize(Policy = Policies.Founder), HttpPost("/settings/startup/{kind}/remove")]
    public async Task<IActionResult> RemoveStartupImage(string kind, CancellationToken ct) =>
        Back(await profiles.RemoveStartupImageAsync(Actor, kind == "logo" ? MediaKind.StartupLogo : MediaKind.StartupCover, ct), "Settings");

    private async Task<IActionResult> ImageAsync(IFormFile? file, Func<Facade.Media.UploadedFile, Task<Result>> apply, CancellationToken ct)
    {
        var (saved, error) = await UploadSaver.SaveAsync(file, UploadSaver.ImageExtensions, storage, null, ct);
        if (saved is null) return Json(Result.Failure(ErrorCodes.InvalidMedia, error!));
        return Json(await apply(saved));
    }
}
