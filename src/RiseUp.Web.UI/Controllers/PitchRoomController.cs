using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiseUp.Entities.Common;
using RiseUp.Entities.Abstractions;
using RiseUp.Facade.Media;
using RiseUp.Facade.ConnectionRequests;
using RiseUp.Facade.Pitching;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Web.UI.Infrastructure;

namespace RiseUp.Web.UI.Controllers;

public sealed record TrackBody(string Kind, PitchVideoSlot? Slot, int? Percent, int? Slide, int? DocumentId);

/// <summary>Pitch room for the receiving investor and the owning founder (id 0 = founder preview).</summary>
[Authorize(Policy = Policies.FounderOrInvestor)]
public sealed class PitchRoomController(IPitchRoomFacade rooms, IConnectionRequestFacade requests, IFileStorage storage) : AppController
{
    [HttpGet("/" + UrlRewrite.PitchRoom)]
    public async Task<IActionResult> Index(int id, CancellationToken ct)
    {
        var r = await rooms.GetAsync(Actor, id, ct);
        if (r.IsFailure) return r.Error.Code == ErrorCodes.NotFound ? NotFound() : Forbid();
        return View(r.Value);
    }

    [HttpPost("/pitch-room/{id:int}/track")]
    public async Task<IActionResult> Track(int id, [FromBody] TrackBody body, CancellationToken ct) =>
        Json(await rooms.TrackAsync(Actor, id, new TrackInput(body.Kind ?? "", body.Slot, body.Percent, body.Slide, body.DocumentId), ct));

    [HttpPost("/pitch-room/{id:int}/questions")]
    public async Task<IActionResult> Ask(int id, string text, CancellationToken ct) { Flash(await rooms.AskAsync(Actor, id, text, ct)); return Redirect($"/pitch-room/{id}#qa"); }

    [HttpPost("/pitch-questions/{id:int}/answer")]
    public async Task<IActionResult> Answer(int id, int requestId, string text, CancellationToken ct) { Flash(await rooms.AnswerAsync(Actor, id, text, ct)); return Redirect($"/pitch-room/{requestId}#qa"); }

    [HttpPost("/pitch-room/{id:int}/more-info")]
    public async Task<IActionResult> MoreInfo(int id, string message, CancellationToken ct) { Flash(await rooms.RequestMoreInfoAsync(Actor, id, message, ct)); return Redirect($"/pitch-room/{id}#information"); }

    [Authorize(Policy = Policies.Founder), HttpPost("/pitch-room/{id:int}/information/{itemId:int}/reply")]
    [RequestSizeLimit(BusinessRules.PitchDocumentMaxBytes + 128 * 1024)]
    public async Task<IActionResult> ReplyToInfo(int id, int itemId, string? text, IFormFile? file, CancellationToken ct)
    {
        var location = $"/pitch-room/{id}#info-{itemId}";
        UploadedFile? staged = null;
        if (file is not null)
        {
            if (file.Length > BusinessRules.PitchDocumentMaxBytes)
            {
                Flash(Result.Failure(ErrorCodes.InvalidMedia, "Attachments can be up to 10 MB."));
                return Redirect(location);
            }
            var upload = await UploadSaver.SaveAsync(file, UploadSaver.DocumentExtensions, storage, null, ct);
            if (upload.File is null)
            {
                Flash(Result.Failure(ErrorCodes.InvalidMedia, upload.Error!));
                return Redirect(location);
            }
            staged = upload.File;
        }
        try
        {
            var result = await rooms.ReplyToInfoAsync(Actor, id, itemId, text, staged, ct);
            if (result.IsSuccess) staged = null;
            Flash(result);
        }
        finally
        {
            if (staged is not null) await storage.DeleteAsync(staged.StorageKey, CancellationToken.None);
        }
        return Redirect(location);
    }

    [Authorize(Policy = Policies.Investor), HttpPost("/pitch-room/{id:int}/respond")]
    public async Task<IActionResult> Respond(int id, bool accept, string? note, CancellationToken ct)
    {
        var result = await requests.RespondAsync(Actor, id, accept, note, ct);
        Flash(result);
        return Redirect(result.IsSuccess && accept ? "/conversations" : $"/pitch-room/{id}");
    }
}
