using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Pitching;

public sealed record TrackInput(string Kind, PitchVideoSlot? Slot, int? Percent, int? Slide, int? DocumentId);
