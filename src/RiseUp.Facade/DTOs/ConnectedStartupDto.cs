namespace RiseUp.Facade.ConnectionRequests;

public sealed record ConnectedStartupDto(int StartupId, int ConversationId, DateTime ConnectedAtUtc, string Name,
    string? Industry, string? Location, string? LogoUrl, bool ProfileAvailable);
