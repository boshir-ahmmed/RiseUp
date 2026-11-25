namespace RiseUp.Facade.ConnectionRequests;

public sealed record ConnectedProfileDto(int ProfileId, int ConversationId, DateTime ConnectedAtUtc, string Name,
    string? Detail, string? Location, string? ImageUrl, bool ProfileAvailable);
