using RiseUp.Entities.Abstractions;
using RiseUp.Facade.Common;

namespace RiseUp.Facade.Notifications;

public sealed record NotificationDto(int Id, string Text, string? Link, bool IsRead, DateTime AtUtc);
