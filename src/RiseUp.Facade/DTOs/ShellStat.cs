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

public sealed record ShellStat(string Label, string Value, string Href);
