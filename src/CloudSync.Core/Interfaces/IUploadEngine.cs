using CloudSync.Core.Models;

namespace CloudSync.Core.Interfaces;

/// <summary>
/// Executes uploads to Google Drive for a given <see cref="UploadJob"/>.
/// All implementations must use async I/O and ResumableUpload for large files.
/// </summary>
public interface IUploadEngine
{
    /// <summary>
    /// Uploads or updates a file on Google Drive.
    /// Returns the Drive file ID on success.
    /// Throws on unrecoverable errors (Polly retries are applied internally).
    /// </summary>
    Task<string> UploadAsync(UploadJob job, CancellationToken cancellationToken);
}
