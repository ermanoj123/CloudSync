using System.Threading.Channels;
using CloudSync.Core.Interfaces;
using CloudSync.Core.Models;
using Microsoft.Extensions.Logging;

namespace CloudSync.Infrastructure.Scanning;

public class FileSystemWatcherProducer : IFileScanner, IDisposable
{
    private readonly ChannelWriter<UploadJob> _channelWriter;
    private readonly IEnumerable<FolderMapping> _mappings;
    private readonly IStateRepository _stateRepo;
    private readonly ILogger<FileSystemWatcherProducer> _logger;
    private readonly List<FileSystemWatcher> _watchers = new();

    public FileSystemWatcherProducer(ChannelWriter<UploadJob> channelWriter, IEnumerable<FolderMapping> mappings, IStateRepository stateRepo, ILogger<FileSystemWatcherProducer> logger)
    {
        _channelWriter = channelWriter;
        _mappings = mappings;
        _stateRepo = stateRepo;
        _logger = logger;
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (var mapping in _mappings)
        {
            if (!Directory.Exists(mapping.LocalPath)) continue;

            var watcher = new FileSystemWatcher(mapping.LocalPath)
            {
                IncludeSubdirectories = mapping.Recursive,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                // Increase from default 8KB to prevent buffer overflow on busy directories
                InternalBufferSize = 65536
            };

            watcher.Changed += (s, e) => OnFileEvent(e.FullPath, mapping);
            watcher.Created += (s, e) => OnFileEvent(e.FullPath, mapping);
            watcher.Renamed += (s, e) => OnFileEvent(e.FullPath, mapping); // Also handle names changing
            
            watcher.EnableRaisingEvents = true;
            _watchers.Add(watcher);
            
            _logger.LogInformation("Watcher active for {Path}", mapping.LocalPath);
        }

        // Just hold until canceled
        return Task.Delay(-1, cancellationToken);
    }

    private void OnFileEvent(string path, FolderMapping mapping)
    {
        try
        {
            if (File.Exists(path))
            {
                // In a real app, need a debouncer or delay as FileStream might still be locked
                var job = new UploadJob
                {
                    LocalPath = path,
                    RemoteFolderId = mapping.RemoteFolderId,
                    Trigger = JobTrigger.FileSystemWatcher
                };

                    // Log a warning if the bounded channel is full so jobs are never silently dropped
                    if (!_channelWriter.TryWrite(job))
                    {
                        _logger.LogWarning(
                            "Upload channel is full — job dropped for {Path}. Consider increasing WorkerCount or channel capacity.",
                            path);
                    }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pushing event for {Path}", path);
        }
    }

    public void Dispose()
    {
        foreach (var w in _watchers)
        {
            w.EnableRaisingEvents = false;
            w.Dispose();
        }
    }
}
