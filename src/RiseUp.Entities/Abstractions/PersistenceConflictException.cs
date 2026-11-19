using RiseUp.Entities.Common;

namespace RiseUp.Entities.Abstractions;


/// <summary>Raised by the Unit of Work when a unique index or concurrency token rejects a commit.</summary>
public sealed class PersistenceConflictException(string message, Exception? inner = null) : Exception(message, inner);
