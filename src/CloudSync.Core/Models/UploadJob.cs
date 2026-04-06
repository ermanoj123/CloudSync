namespace CloudSync.Core.Models;

/// <summary>
/// Represents a single unit of work placed onto the Channel by a producer.
/// Consumed by a worker task that calls the Google Drive upload engine.
/// </summary>
public sealed class UploadJob
{
    /// <summary>Absolute path of the local file to be uploaded.</summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>SHA-256 hash computed by the producer before enqueuing.</summary>
    public string FileHash { get; set; } = string.Empty;

    /// <summary>Target Google Drive folder ID for this job.</summary>
    public string RemoteFolderId { get; set; } = string.Empty;

    /// <summary>
    /// Existing Drive file ID if the file is being updated (not a first upload).
    /// Null means a new file will be created.
    /// </summary>
    public string? ExistingGoogleFileId { get; set; }

    /// <summary>UTC time the job was enqueued — used for telemetry.</summary>
    public DateTime EnqueuedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Number of times this job has been retried at the worker level.</summary>
    public int RetryCount { get; set; }

    /// <summary>Trigger that caused this job to be enqueued.</summary>
    public JobTrigger Trigger { get; set; } = JobTrigger.DeepScan;
}

public enum JobTrigger
{
    /// <summary>Enqueued during the periodic deep-scan pass.</summary>
    DeepScan,

    /// <summary>Enqueued immediately by the FileSystemWatcher on change/create/rename.</summary>
    FileSystemWatcher
}
