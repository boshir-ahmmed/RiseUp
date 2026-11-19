using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Pitching;

public sealed record RoomQuestionDto(int Id, string AskerName, string Text, DateTime AtUtc, string? Answer, DateTime? AnsweredAtUtc);
