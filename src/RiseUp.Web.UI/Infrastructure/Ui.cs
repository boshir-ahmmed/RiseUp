using RiseUp.Entities.Common;

namespace RiseUp.Web.UI.Infrastructure;

/// <summary>Small presentation helpers shared by Razor views.</summary>
public static class Ui
{
    private static readonly string[] Palette = ["#0A8F85", "#24324A", "#7A5AF8", "#B45309", "#2E7D32", "#C2410C", "#0E7490", "#9F1239", "#4338CA", "#15803D"];
    public static string Color(int index) => Palette[Math.Abs(index) % Palette.Length];
    public const string DefaultAvatar = "/images/identity/member.jpg";
    public const string DefaultStartupLogo = "/images/identity/startup.png";
    public const string DefaultCover = "/images/identity/cover.jpg";

    public static string CoverStyle(string? mediaUrl)
    {
        var images = mediaUrl is null ? $"url('{DefaultCover}')" : $"url('{mediaUrl}'),url('{DefaultCover}')";
        return $"background-image:{images};background-position:center;background-size:cover";
    }

    /// <summary>Two initials from the first letters of the first two words (punctuation such as "(Verification)" ignored).</summary>
    public static string Initials(string name)
    {
        var letters = name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.FirstOrDefault(char.IsLetter)).Where(c => c != default).Take(2).Select(char.ToUpperInvariant);
        var s = string.Concat(letters);
        return s.Length == 0 ? "?" : s;
    }
    public static string Stage(FundingStage s) => s switch { FundingStage.PreSeed => "Pre-seed", FundingStage.SeriesA => "Series A", FundingStage.SeriesB => "Series B", _ => s.ToString() };
    public static string Usd(decimal? v) => v is null ? "—" : $"${v:N0}";
    public static string Bdt(decimal v) => $"৳{v:N0}";
    public static string Ago(DateTime utc)
    {
        var d = DateTime.UtcNow - utc;
        return d.TotalMinutes < 1 ? "just now" : d.TotalHours < 1 ? $"{(int)d.TotalMinutes}m ago" : d.TotalDays < 1 ? $"{(int)d.TotalHours}h ago" : $"{(int)d.TotalDays}d ago";
    }
    public static string Left(TimeSpan t) => t <= TimeSpan.Zero ? "0h" : t.TotalDays >= 1 ? $"{(int)t.TotalDays}d {t.Hours}h" : $"{t.Hours}h {t.Minutes}m";
    public static string Dhaka(DateTime utc) => RiseUp.Framework.Time.DhakaTime.ToDhaka(utc).ToString("dd MMM yyyy, HH:mm");
    public static string StatusClass(string status) => status switch
    {
        "Verified" or "Accepted" or "Approved" or "Active" or "Paid" or "Clean" => "b-ver",
        "Pending" or "PendingCheck" or "AwaitingReview" or "AwaitingIncomingConsent" => "b-pend",
        "Rejected" or "Failed" or "Infected" => "b-danger",
        "Expired" or "Paused" or "Open" or "AiUnavailable" => "b-warn",
        _ => "b-gray",
    };
}
