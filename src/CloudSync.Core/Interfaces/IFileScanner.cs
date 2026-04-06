using CloudSync.Core.Models;

namespace CloudSync.Core.Interfaces;

/// <summary>
/// Scans local folders for files that need to be uploaded.
/// Implementations: DeepScanProducer (periodic) and FileSystemWatcherProducer (event-driven).
/// </summary>
public interface IFileScanner
{
    /// <summary>
    /// Starts the scanning process, pushing <see cref="UploadJob"/> items
    /// into the shared channel until the <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken);
}
