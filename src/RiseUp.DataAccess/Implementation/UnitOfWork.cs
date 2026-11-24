using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using RiseUp.Framework.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RiseUp.Entities.Abstractions;

namespace RiseUp.DataAccess.Repositories;

/// <summary>Single commit point per use case. Translates constraint/concurrency failures into a domain exception.</summary>
public sealed class UnitOfWork(RiseUpDbContext db) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try { return await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException ex) { throw new PersistenceConflictException("The record was changed by another request.", ex); }
        catch (DbUpdateException ex) { throw new PersistenceConflictException("A uniqueness or integrity rule rejected the change.", ex, ContactError(ex.InnerException)); }
    }

    private static Error? ContactError(Exception? cause)
    {
        if (cause is not (SqlException { Number: 2601 or 2627 } or SqliteException { SqliteExtendedErrorCode: 2067 })) return null;
        var message = cause.Message;
        if (message.Contains("'UX_AccountContacts_Phone'") || message.Contains("constraint failed: AccountContacts.PhoneKey"))
            return new Error(ErrorCodes.PhoneTaken, "This contact number is already in use.");
        if (message.Contains("'UX_AccountContacts_Email'") || message.Contains("constraint failed: AccountContacts.EmailKey")
            || message.Contains("'IX_UserAccounts_NormalizedEmail'") || message.Contains("constraint failed: UserAccounts.NormalizedEmail")
            || message.Contains("'IX_TeamAccounts_Email'") || message.Contains("constraint failed: TeamAccounts.Email"))
            return new Error(ErrorCodes.EmailTaken, "This email address is already in use.");
        return null;
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        if (db.Database.CurrentTransaction is not null) return new NestedTransaction();
        var isolation = db.Database.IsSqlServer() ? IsolationLevel.Serializable : IsolationLevel.Serializable;
        return new EfTransaction(await db.Database.BeginTransactionAsync(isolation, ct));
    }

    private sealed class EfTransaction(IDbContextTransaction tx) : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken ct = default) => tx.CommitAsync(ct);
        public Task RollbackAsync(CancellationToken ct = default) => tx.RollbackAsync(ct);
        public ValueTask DisposeAsync() => tx.DisposeAsync();
    }

    private sealed class NestedTransaction : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
