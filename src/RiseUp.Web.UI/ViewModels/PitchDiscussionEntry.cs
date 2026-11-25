using RiseUp.Facade.Pitching;

namespace RiseUp.Web.UI.ViewModels;

public sealed record PitchDiscussionEntry(int Id, bool IsRequest, string? AskedBy, DateTime AskedAtUtc,
    string Prompt, DateTime? AnsweredAtUtc, string? Reply, string? FileName, string? FileUrl)
{
    public string Anchor => IsRequest ? $"info-{Id}" : $"question-{Id}";

    public static IReadOnlyList<PitchDiscussionEntry> ForRoom(PitchRoomDto room)
    {
        var questions = room.Questions.Select(q => new PitchDiscussionEntry(q.Id, false, q.AskerName,
            q.AtUtc, q.Text, q.AnsweredAtUtc, q.Answer, null, null));
        var requests = room.InfoItems.Select(i => new PitchDiscussionEntry(i.Id, true, room.InvestorName,
            i.RequestedAtUtc, i.Prompt, i.AnsweredAtUtc, i.ReplyText, i.FileName, i.FileUrl));
        return questions.Concat(requests).OrderBy(e => e.AskedAtUtc).ThenBy(e => e.Id).ToList();
    }
}
