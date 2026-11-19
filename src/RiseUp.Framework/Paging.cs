namespace RiseUp.Framework;

/// <summary>Bounded paging. Lists are never unbounded.</summary>
public sealed record PageRequest(int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 50;
    public int SafePage => Page < 1 ? 1 : Page;
    public int SafeSize => PageSize < 1 ? 20 : Math.Min(PageSize, MaxPageSize);
    public int Skip => (SafePage - 1) * SafeSize;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
