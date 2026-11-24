namespace RiseUp.DataAccess.Migrations;

internal static class PitchInformationSchemaV1
{
    internal const string ImportRequests = """
        INSERT INTO PitchInfoItems (RequestId, Prompt, CreatedAtUtc)
        SELECT Id, InfoRequestMessage, COALESCE(InfoRequestedAtUtc, SentAtUtc)
        FROM ConnectionRequests
        WHERE Type = 1 AND InfoRequestMessage IS NOT NULL AND TRIM(InfoRequestMessage) <> '';
        """;
}
