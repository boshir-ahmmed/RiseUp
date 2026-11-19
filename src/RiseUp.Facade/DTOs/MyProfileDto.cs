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

public sealed record MyProfileDto(string Name, string Headline, string Location, string Bio, string? Link, string? PhotoUrl, string? CoverUrl, int ColorIndex,
    bool IsVerified, bool IsTeamMember, bool IsAdmin, bool CanEditName, bool CanHaveCover, string? PendingName, DateTime? NextNameChangeAtUtc,
    string RoleLine, int? StartupId, string? StartupName, int? InvestorId, int Strength, IReadOnlyList<(string Item, bool Done)> Checklist);
