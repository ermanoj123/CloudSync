using System.Threading.Channels;
using CloudSync.Core.Interfaces;
using CloudSync.Core.Models;
using Microsoft.Extensions.Logging;

namespace CloudSync.Infrastructure.Scanning;

public class DeepScanProducer : IFileScanner
{
    private readonly ChannelWriter<UploadJob> _channelWriter;
    private readonly List<FolderMapping> _mappings;
    private readonly IStateRepository _stateRepo;
    private readonly ILogger<DeepScanProducer> _logger;

    public DeepScanProducer(ChannelWriter<UploadJob> channelWriter, List<FolderMapping> mappings, IStateRepository stateRepo, ILogger<DeepScanProducer> logger)
    {
        _channelWriter = channelWriter;
        _mappings = mappings;
        _stateRepo = stateRepo;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting periodic deep scan");
            
            foreach (var mapping in _mappings)
            {
                if (!Directory.Exists(mapping.LocalPath)) continue;

                var searchOption = mapping.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                
                // Using EnumerateFiles for memory efficiency as requested
                foreach (var file in Directory.EnumerateFiles(mapping.LocalPath, "*.*", searchOption))
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        var fileInfo = new FileInfo(file);
                        var state = await _stateRepo.GetByLocalPathAsync(file, cancellationToken);

                        // Very basic hash check (in reality, you'd compute the hash of the stream)
                        // Using LastWriteTime as a proxy check to avoid hashing huge files on every run
                        if (state == null || state.LastModifiedUtc < fileInfo.LastWriteTimeUtc)
                        {
                            var job = new UploadJob
                            {
                                LocalPath = file,
                                RemoteFolderId = mapping.RemoteFolderId,
                                ExistingGoogleFileId = state?.GoogleFileId,
                                Trigger = JobTrigger.DeepScan
                            };

                            // Bounded channel will apply backpressure here
                            await _channelWriter.WriteAsync(job, cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to assess file {File}", file);
                    }
                }
            }

            // configurable interval, hardcoded to 1 hour for now
            await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
        }
    }
}
