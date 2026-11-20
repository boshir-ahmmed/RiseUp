using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RiseUp.Entities.Common;
using RiseUp.Facade.Media;
using RiseUp.Facade.Pitching;
using RiseUp.Facade.Posts;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;
using RiseUp.Web.UI.Infrastructure;

namespace RiseUp.Web.UI.Controllers;

/// <summary>Reels, shortlist, meetings, saved posts and media streaming.</summary>
public sealed class DiscoveryExtrasController(IPitchPackageFacade packages, IShortlistFacade shortlist, IMeetingFacade meetings, IPostFacade posts, IMediaFacade media) : AppController
{
    [HttpGet("/" + UrlRewrite.Reels)]
    public async Task<IActionResult> Reels(CancellationToken ct) => View(new ViewModels.ReelsViewModel(await posts.GetFeedAsync(Actor, "foryou", PostCategory.Reel, 50, ct), await packages.ListSpotlightAsync(Actor, 30, ct)));

    // ------------------------------------------------------------ media (re-authorised on every request; supports HTTP Range for seeking)
    [HttpGet("/media/{id:int}")]
    public async Task<IActionResult> Media(int id, bool download = false, CancellationToken ct = default)
    {
        var r = await media.OpenAsync(Actor, id, download, ct);
        if (r.IsFailure) return r.Error.Code == ErrorCodes.ForbiddenDocument ? Forbid() : NotFound();
        var m = r.Value!;
        Response.Headers.CacheControl = "private, no-cache";
        return m.AsAttachment ? File(m.Content, m.ContentType, m.FileName, enableRangeProcessing: true) : File(m.Content, m.ContentType, enableRangeProcessing: true);
    }

    // ------------------------------------------------------------ saved posts
    [Authorize(Policy = Policies.FounderOrInvestor), HttpPost("/posts/{id:int}/save")]
    public async Task<IActionResult> Save(int id, CancellationToken ct) { var r = await posts.ToggleSaveAsync(Actor, id, ct); return Json(r, r.Value); }

    // ------------------------------------------------------------ shortlist (investor-private)
    [Authorize(Policy = Policies.Investor), HttpGet("/" + UrlRewrite.Shortlist)]
    public async Task<IActionResult> Shortlist(CancellationToken ct) => View(await shortlist.ListAsync(Actor, ct));

    [Authorize(Policy = Policies.Investor), HttpPost("/shortlist/{startupId:int}/toggle")]
    public async Task<IActionResult> Toggle(int startupId, CancellationToken ct)
    {
        var r = await shortlist.ToggleAsync(Actor, startupId, ct);
        return Request.Headers.Accept.ToString().Contains("application/json") ? Json(r, r.Value) : Back(r, nameof(Shortlist));
    }

    [Authorize(Policy = Policies.Investor), HttpPost("/shortlist/{startupId:int}")]
    public async Task<IActionResult> UpdateShortlist(int startupId, ShortlistStage stage, string? note, CancellationToken ct) =>
        Back(await shortlist.UpdateAsync(Actor, startupId, stage, note, ct), nameof(Shortlist));

    // ------------------------------------------------------------ meetings (inside a connected conversation)
    /// <summary>Slots arrive as Dhaka wall-clock times from datetime-local inputs.</summary>
    [Authorize(Policy = Policies.FounderOrInvestor), HttpPost("/conversations/{id:int}/meetings")]
    public async Task<IActionResult> Propose(int id, DateTime?[]? slots, string? agenda, CancellationToken ct)
    {
        var utc = (slots ?? []).Where(s => s is not null).Select(s => DhakaTime.FromDhaka(s!.Value)).ToList();
        Flash(await meetings.ProposeAsync(Actor, id, utc, agenda, ct));
        return Redirect($"/conversations/{id}#meetings");
    }

    [Authorize(Policy = Policies.FounderOrInvestor), HttpPost("/meetings/{id:int}/accept")]
    public async Task<IActionResult> Accept(int id, long slotTicks, int conversationId, CancellationToken ct)
    {
        Flash(await meetings.AcceptAsync(Actor, id, new DateTime(slotTicks, DateTimeKind.Utc), ct));
        return Redirect($"/conversations/{conversationId}#meetings");
    }

    [Authorize(Policy = Policies.FounderOrInvestor), HttpPost("/meetings/{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, int conversationId, CancellationToken ct) { Flash(await meetings.CancelAsync(Actor, id, ct)); return Redirect($"/conversations/{conversationId}#meetings"); }

    /// <summary>Downloads a standards-compliant .ics file for a confirmed meeting.</summary>
    [Authorize(Policy = Policies.ChatParticipant), HttpGet("/meetings/{id:int}/calendar.ics")]
    public async Task<IActionResult> Calendar(int id, CancellationToken ct)
    {
        var r = await meetings.GetCalendarAsync(Actor, id, ct);
        if (r.IsFailure) return NotFound();
        var (start, title, agenda) = r.Value;
        static string Esc(string s) => s.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n");
        var ics = string.Join("\r\n", "BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:-//RiseUp//Meetings//EN", "BEGIN:VEVENT", $"UID:riseup-meeting-{id}@riseup",
            $"DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}", $"DTSTART:{start:yyyyMMddTHHmmssZ}", $"DTEND:{start.AddMinutes(45):yyyyMMddTHHmmssZ}",
            $"SUMMARY:{Esc(title)}", $"DESCRIPTION:{Esc(string.IsNullOrEmpty(agenda) ? "Video call link: agree in your RiseUp conversation." : agenda)}", "END:VEVENT", "END:VCALENDAR", "");
        return File(System.Text.Encoding.UTF8.GetBytes(ics), "text/calendar", $"riseup-meeting-{id}.ics");
    }
}
