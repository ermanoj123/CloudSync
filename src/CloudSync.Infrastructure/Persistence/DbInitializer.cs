using CloudSync.Core.Models;
using LiteDB;

namespace CloudSync.Infrastructure.Persistence;

public static class DbInitializer
{
    public static void Initialize(string connectionString = @"Filename=C:\CloudSync\cloudsync.db;Connection=Shared")
    {
        using var db = new LiteDatabase(connectionString);
        var collection = db.GetCollection<FileRecord>("filerecords");

        // Setup indexes for blazing fast querying
        collection.EnsureIndex(x => x.LocalPath, unique: true);
        collection.EnsureIndex(x => x.Status);
    }
}
