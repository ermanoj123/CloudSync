using CloudSync.Core.Interfaces;
using CloudSync.Core.Models;
using LiteDB;

namespace CloudSync.Infrastructure.Persistence;

/// <summary>
/// A NoSQL implementation using LiteDB.
/// </summary>
public class LiteDbStateRepository : IStateRepository
{
    private readonly string _connectionString;

    public LiteDbStateRepository(string connectionString = @"Filename=C:\CloudSync\cloudsync.db;Connection=Shared")
    {
        _connectionString = connectionString;
    }

    public Task<FileRecord?> GetByLocalPathAsync(string localPath, CancellationToken ct = default)
    {
        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<FileRecord>("filerecords");
        var record = collection.FindOne(x => x.LocalPath == localPath);
        return Task.FromResult<FileRecord?>(record);
    }

    public Task<IReadOnlyList<FileRecord>> GetByStatusAsync(SyncStatus status, CancellationToken ct = default)
    {
        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<FileRecord>("filerecords");
        var records = collection.Find(x => x.Status == status).ToList();
        return Task.FromResult<IReadOnlyList<FileRecord>>(records);
    }

    public Task InsertAsync(FileRecord record, CancellationToken ct = default)
    {
        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<FileRecord>("filerecords");
        collection.Insert(record);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(FileRecord record, CancellationToken ct = default)
    {
        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<FileRecord>("filerecords");
        collection.Update(record);
        return Task.CompletedTask;
    }

    public async Task UpsertAsync(FileRecord record, CancellationToken ct = default)
    {
        var existing = await GetByLocalPathAsync(record.LocalPath, ct);
        if (existing == null)
        {
            await InsertAsync(record, ct);
        }
        else
        {
            // Ensure ID is transferred so LiteDB does a proper update if needed
            record.Id = existing.Id;
            await UpdateAsync(record, ct);
        }
    }

    public Task MarkDeletedAsync(string localPath, CancellationToken ct = default)
    {
        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<FileRecord>("filerecords");
        var record = collection.FindOne(x => x.LocalPath == localPath);
        if (record != null)
        {
            record.Status = SyncStatus.Deleted;
            collection.Update(record);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FileRecord>> GetAllUnderPathAsync(string rootPath, CancellationToken ct = default)
    {
        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetCollection<FileRecord>("filerecords");
        var records = collection.Find(x => x.LocalPath.StartsWith(rootPath)).ToList();
        return Task.FromResult<IReadOnlyList<FileRecord>>(records);
    }
}
