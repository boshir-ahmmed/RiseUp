namespace RiseUp.DataAccess.Migrations;

// Shared by the SQL Server migration and the SQLite demo upgrade. Keep V1 stable for existing databases.
internal static class AccountContactSchemaV1
{
    private const string Columns = "(OwnerKind, OwnerId, EmailKey, PhoneKey)";
    private const string UserRows = "SELECT 1 AS OwnerKind, Id AS OwnerId, Email, Phone FROM UserAccounts WHERE DeletedAtUtc IS NULL";
    private const string TeamRows = "SELECT 2 AS OwnerKind, Id AS OwnerId, Email, CAST(NULL AS nvarchar(30)) AS Phone FROM TeamAccounts WHERE Status <> 3";

    internal static IEnumerable<string> Install(bool sqlite)
    {
        yield return """
            CREATE TABLE AccountContacts (
                OwnerKind int NOT NULL,
                OwnerId int NOT NULL,
                EmailKey nvarchar(256) NOT NULL,
                PhoneKey nvarchar(32) NULL,
                PRIMARY KEY (OwnerKind, OwnerId)
            )
            """;
        yield return $"INSERT INTO AccountContacts {Columns} " + Keys(UserRows + " UNION ALL " + TeamRows, sqlite);
        if (!sqlite)
            yield return """
                DECLARE @conflict nvarchar(2048);
                SELECT TOP (1) @conflict = CONCAT('Duplicate account email. Resolve these owner records and rerun Update-Database (1=user, 2=team): ',
                    STRING_AGG(CONVERT(nvarchar(max), CONCAT(OwnerKind, '/', OwnerId)), ', '))
                FROM AccountContacts GROUP BY EmailKey HAVING COUNT(*) > 1;
                IF @conflict IS NOT NULL THROW 51001, @conflict, 1;
                SELECT TOP (1) @conflict = CONCAT('Duplicate contact number. Resolve these user IDs and rerun Update-Database: ',
                    STRING_AGG(CONVERT(nvarchar(max), OwnerId), ', '))
                FROM AccountContacts WHERE PhoneKey IS NOT NULL GROUP BY PhoneKey HAVING COUNT(*) > 1;
                IF @conflict IS NOT NULL THROW 51002, @conflict, 1;
                """;
        yield return "CREATE UNIQUE INDEX UX_AccountContacts_Email ON AccountContacts (EmailKey)";
        yield return "CREATE UNIQUE INDEX UX_AccountContacts_Phone ON AccountContacts (PhoneKey) WHERE PhoneKey IS NOT NULL";
        foreach (var isTeam in new[] { false, true })
        {
            var table = isTeam ? "TeamAccounts" : "UserAccounts";
            var kind = isTeam ? 2 : 1;
            var rows = isTeam ? TeamRows : UserRows;
            var trigger = "TR_" + table + "_Contacts";
            if (!sqlite)
            {
                var changed = isTeam ? "UPDATE(Email) OR UPDATE(Status)" : "UPDATE(Email) OR UPDATE(Phone) OR UPDATE(DeletedAtUtc)";
                yield return $"""
                    CREATE TRIGGER {trigger} ON {table} AFTER INSERT, UPDATE, DELETE AS
                    BEGIN
                        SET NOCOUNT ON;
                        IF EXISTS (SELECT 1 FROM inserted) AND NOT ({changed}) RETURN;
                        DELETE c FROM AccountContacts c JOIN deleted d ON d.Id = c.OwnerId WHERE c.OwnerKind = {kind};
                        INSERT INTO AccountContacts {Columns} {Keys(rows.Replace(table, "inserted"), false)};
                    END
                    """;
                continue;
            }
            var insert = $"INSERT INTO AccountContacts {Columns} " + Keys(rows + " AND Id = NEW.Id", true);
            var delete = $"DELETE FROM AccountContacts WHERE OwnerKind = {kind} AND OwnerId = OLD.Id";
            var fields = isTeam ? "Email, Status" : "Email, Phone, DeletedAtUtc";
            yield return $"CREATE TRIGGER {trigger}_Insert AFTER INSERT ON {table} BEGIN {insert}; END";
            yield return $"CREATE TRIGGER {trigger}_Update AFTER UPDATE OF {fields} ON {table} BEGIN {delete}; {insert}; END";
            yield return $"CREATE TRIGGER {trigger}_Delete AFTER DELETE ON {table} BEGIN {delete}; END";
        }
    }

    private static string Keys(string rows, bool sqlite)
    {
        var phone = "Phone";
        // Bengali and ASCII digits, and common formatting of international and domestic numbers.
        if (sqlite)
        {
            for (var i = 0; i < 10; i++)
                phone = $"REPLACE({phone}, '{(char)('০' + i)}', '{i}')";
        }
        else
        {
            phone = "TRANSLATE(Phone, N'০১২৩৪৫৬৭৮৯', '0123456789')";
        }
        foreach (var separator in " ().-+")
            phone = $"REPLACE({phone}, '{separator}', '')";
        var character = sqlite ? "CHAR" : "NCHAR";
        foreach (var whitespace in new[] { 9, 10, 13, 160 })
            phone = $"REPLACE({phone}, {character}({whitespace}), '')";
        var tail = sqlite ? "SUBSTR(Digits, 3)" : "SUBSTRING(Digits, 3, 30)";
        var length = sqlite ? "LENGTH(Digits)" : "LEN(Digits)";
        var bd = sqlite ? "'88' || Digits" : "'88' + Digits";
        return $"""
            SELECT OwnerKind, OwnerId, UPPER(LTRIM(RTRIM(Email))),
                CASE WHEN Digits LIKE '00%' THEN {tail}
                     WHEN Digits LIKE '01%' AND {length} = 11 THEN {bd}
                     ELSE NULLIF(Digits, '') END
            FROM (SELECT OwnerKind, OwnerId, Email, {phone} AS Digits FROM ({rows}) AS owners) AS contacts
            """;
    }
}
