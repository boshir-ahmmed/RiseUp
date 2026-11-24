using RiseUp.Framework.Results;

namespace RiseUp.Entities.Abstractions;


/// <summary>Raised by the Unit of Work when a unique index or concurrency token rejects a commit.</summary>
public sealed class PersistenceConflictException(string message, Exception? inner = null, Error? contactError = null) : Exception(message, inner)
{
    public Error? ContactError { get; } = contactError;
}
