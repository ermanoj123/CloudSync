using System.Threading.Channels;
using CloudSync.Core.Interfaces;
using CloudSync.Core.Models;
using CloudSync.Infrastructure.GoogleDrive;
using CloudSync.Infrastructure.Persistence;
using CloudSync.Infrastructure.Scanning;
using CloudSync.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CloudSync.Worker;

public class Program
{
    public static int Main(string[] args)
    {
        // Early logging setup
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            CreateHostBuilder(args).Build().Run();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService(options =>
            {
                options.ServiceName = "CloudSync Service";
            })
            .UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                // Log to Windows Event Log (Requires Admin to create source initially, ignoring for now)
                // .WriteTo.EventLog("CloudSync", manageEventSource: true)
                // Log to rolling file
                .WriteTo.File(@"C:\CloudSync\Logs\cloudsync_.txt", rollingInterval: RollingInterval.Day)
            )
            .ConfigureServices((hostContext, services) =>
            {
                // Ensure directories exist
                Directory.CreateDirectory(@"C:\CloudSync");
                Directory.CreateDirectory(@"C:\CloudSync\Logs");

                // Init DB on startup (LiteDB uses a different connection string format)
                DbInitializer.Initialize(@"Filename=C:\CloudSync\cloudsync.db;Connection=Shared");

                var config = hostContext.Configuration;

                // Load mappings
                var mappings = new List<FolderMapping>();
                config.GetSection("FolderMappings").Bind(mappings);
                services.AddSingleton(mappings);

                // Setup DB
                services.AddSingleton<IStateRepository>(new LiteDbStateRepository(@"Filename=C:\CloudSync\cloudsync.db;Connection=Shared"));

                // Setup Vault
                services.AddSingleton<ICredentialVault, WindowsCredentialVault>();

                // Drive Auth & Upload
                services.AddSingleton<DriveAuthProvider>();
                services.AddSingleton<IUploadEngine, DriveUploadEngine>();

                // Configure Bounded Channel to prevent memory spikes
                var channel = Channel.CreateBounded<UploadJob>(new BoundedChannelOptions(100)
                {
                    FullMode = BoundedChannelFullMode.Wait // Backpressure: Producer will wait if queue is full
                });
                services.AddSingleton(channel.Reader);
                services.AddSingleton(channel.Writer);

                // Add Scanners
                services.AddHostedService<ScannerHostedService>(); // A wrapper to start both producers
                services.AddSingleton<IFileScanner, DeepScanProducer>();
                services.AddSingleton<IFileScanner, FileSystemWatcherProducer>();

                // Add Consumer Workers
                int workerCount = config.GetValue<int>("WorkerCount", 4);
                for (int i = 0; i < workerCount; i++)
                {
                    services.AddHostedService<UploadWorkerService>();
                }
                
                // Add Heartbeat
                services.AddHostedService<HeartbeatService>();
            });
}

// Below are small HostedService wrappers for the main loop

public class ScannerHostedService : BackgroundService
{
    private readonly IEnumerable<IFileScanner> _scanners;
    public ScannerHostedService(IEnumerable<IFileScanner> scanners) => _scanners = scanners;
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = _scanners.Select(s => s.RunAsync(stoppingToken));
        return Task.WhenAll(tasks);
    }
}

public class UploadWorkerService : BackgroundService
{
    private readonly ChannelReader<UploadJob> _reader;
    private readonly IUploadEngine _uploader;
    private readonly IStateRepository _repo;
    private readonly ILogger<UploadWorkerService> _logger;

    public UploadWorkerService(ChannelReader<UploadJob> reader, IUploadEngine uploader, IStateRepository repo, ILogger<UploadWorkerService> logger)
    {
        _reader = reader;
        _uploader = uploader;
        _repo = repo;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Create the Polly policy once per worker (not per job) for efficiency
        var retryPolicy = CloudSync.Resilience.ResiliencePipelineFactory.CreateDriveApiRetryPolicy();

        await foreach (var job in _reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var fileId = await retryPolicy.ExecuteAsync(
                    async () => await _uploader.UploadAsync(job, stoppingToken));
                
                // Update DB on success
                var rec = await _repo.GetByLocalPathAsync(job.LocalPath, stoppingToken) ?? new FileRecord { LocalPath = job.LocalPath };
                rec.GoogleFileId = fileId;
                rec.GoogleParentFolderId = job.RemoteFolderId;
                rec.Status = SyncStatus.Synced;
                rec.LastSyncedUtc = DateTime.UtcNow;
                
                // Use actual hash in real implementation
                rec.FileHash = "stub-hash";
                rec.LastModifiedUtc = File.GetLastWriteTimeUtc(job.LocalPath);

                await _repo.UpsertAsync(rec, stoppingToken);

                _logger.LogInformation("Successfully uploaded {File} to {DriveId}", job.LocalPath, fileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload {File}", job.LocalPath);
                // Update DB on failure etc
            }
        }
    }
}

public class HeartbeatService : BackgroundService
{
    private readonly ILogger<HeartbeatService> _logger;
    public HeartbeatService(ILogger<HeartbeatService> logger) => _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("CloudSync Heartbeat - Service is alive and well.");
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}
