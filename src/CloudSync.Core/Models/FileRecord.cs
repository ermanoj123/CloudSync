namespace CloudSync.Core.Models;

/// <summary>
/// Represents a tracked file entry persisted in SQLite.
/// Maps a local file to its corresponding Google Drive file.
/// </summary>
public sealed class FileRecord
{
    public int Id { get; set; }

    /// <summary>Absolute local path of the file.</summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>SHA-256 hash of the file content at last sync.</summary>
    public string FileHash { get; set; } = string.Empty;

    /// <summary>Google Drive file ID returned after a successful upload.</summary>
    public string? GoogleFileId { get; set; }

    /// <summary>The Google Drive parent folder ID the file was uploaded into.</summary>
    public string? GoogleParentFolderId { get; set; }

    /// <summary>UTC timestamp of the file's last write time at last sync.</summary>
    public DateTime LastModifiedUtc { get; set; }

    /// <summary>UTC timestamp of the last successful upload.</summary>
    public DateTime? LastSyncedUtc { get; set; }

    /// <summary>Current sync status of this file.</summary>
    public SyncStatus Status { get; set; } = SyncStatus.Pending;

    /// <summary>Number of consecutive upload failures.</summary>
    public int FailureCount { get; set; }

    /// <summary>Last error message, if any.</summary>
    public string? LastError { get; set; }
}

public enum SyncStatus
{
    Pending,
    Uploading,
    Synced,
    Failed,
    Deleted
}
