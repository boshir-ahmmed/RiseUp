namespace RiseUp.Entities.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

/// <summary>
/// Optimistic concurrency token. DataAccess regenerates <see cref="Version"/> on every update, so two
/// concurrent writers of the same wallet / request / promotion cannot both commit.
/// </summary>
public interface IConcurrencyAware
{
    Guid Version { get; set; }
}
