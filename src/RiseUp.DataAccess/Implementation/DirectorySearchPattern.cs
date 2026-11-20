namespace RiseUp.DataAccess.Repositories;

internal static class DirectorySearchPattern
{
    // Treat SQL LIKE wildcards as ordinary characters in names and firms.
    public static string Literal(string value) => value.Trim().Replace("~", "~~", StringComparison.Ordinal)
        .Replace("%", "~%", StringComparison.Ordinal).Replace("_", "~_", StringComparison.Ordinal)
        .Replace("[", "~[", StringComparison.Ordinal);
}
