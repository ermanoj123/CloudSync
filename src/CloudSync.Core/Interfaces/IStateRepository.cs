using CloudSync.Core.Models;

namespace CloudSync.Core.Interfaces;

/// <summary>
/// Provides CRUD operations for <see cref="FileRecord"/> in the local SQLite database.
/// All methods are async to avoid blocking the thread pool.
/// </summary>
public interface IStateRepository
{
    /// <summary>Returns the record for a given local path, or null if not tracked.</summary>
    Task<FileRecord?> GetByLocalPathAsync(string localPath, CancellationToken ct = default);

    /// <summary>Returns all records matching the given sync status.</summary>
    Task<IReadOnlyList<FileRecord>> GetByStatusAsync(SyncStatus status, CancellationToken ct = default);

    /// <summary>Inserts a new record. Throws if the path already exists.</summary>
    Task InsertAsync(FileRecord record, CancellationToken ct = default);

    /// <summary>Updates an existing record. Throws if the record is not found.</summary>
    Task UpdateAsync(FileRecord record, CancellationToken ct = default);

    /// <summary>Upserts (insert or update) a record based on LocalPath.</summary>
    Task UpsertAsync(FileRecord record, CancellationToken ct = default);

    /// <summary>Marks a record as deleted (logical delete, preserves history).</summary>
    Task MarkDeletedAsync(string localPath, CancellationToken ct = default);

    /// <summary>Returns all tracked paths under a given local root directory.</summary>
    Task<IReadOnlyList<FileRecord>> GetAllUnderPathAsync(string rootPath, CancellationToken ct = default);
}
