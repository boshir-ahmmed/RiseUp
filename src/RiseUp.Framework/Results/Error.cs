namespace RiseUp.Framework.Results;

/// <summary>A stable, machine-readable business error. Never carries exception details to users.</summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
