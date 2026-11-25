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


/// <summary>Everything the top bar and sidebar need for the signed-in user, in one query set.</summary>
public sealed record ShellDto(string Name, string Headline, string? PhotoUrl, string? CoverUrl, int ColorIndex, bool IsVerified, bool HasPendingName,
    int UnreadNotifications, int UnreadMessages, int PendingBadge, IReadOnlyList<ShellStat> Stats);
