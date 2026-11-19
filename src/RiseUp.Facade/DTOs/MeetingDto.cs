using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Pitching;

public sealed record MeetingDto(int Id, string ProposedByName, bool ProposedByMe, IReadOnlyList<DateTime> SlotsUtc, string Agenda, MeetingStatus Status,
    DateTime? ConfirmedSlotUtc, bool CanAccept, bool CanCancel);
