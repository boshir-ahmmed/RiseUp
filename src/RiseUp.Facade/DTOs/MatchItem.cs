using Microsoft.Extensions.Logging;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Investors;
using RiseUp.Entities.Startups;
using RiseUp.Facade.Common;

namespace RiseUp.Facade.Discovery;

public sealed record MatchItem(int Id, string Name, string Subtitle, int Score, IReadOnlyList<string> Reasons, int ColorIndex);
