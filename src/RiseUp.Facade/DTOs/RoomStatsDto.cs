using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Pitching;

public sealed record RoomStatsDto(DateTime? OpenedAtUtc, int PitchWatch, int DemoWatch, int SlidesViewed, int DocsViewed);
