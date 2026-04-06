using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using CloudSync.Core.Interfaces;
using CloudSync.Core.Models;
using Microsoft.Extensions.Logging;

namespace CloudSync.Infrastructure.GoogleDrive;

public class DriveUploadEngine : IUploadEngine
{
    private readonly DriveService? _driveService;
    private readonly ILogger<DriveUploadEngine> _logger;
    private readonly SemaphoreSlim _throttler = new SemaphoreSlim(4, 4); // E.g., limit to 4 concurrent

    public DriveUploadEngine(DriveAuthProvider authProvider, ILogger<DriveUploadEngine> logger)
    {
        _logger = logger;
        
        // Setup Drive API Service here using authProvider credential
        // This is a stub for the initialization.
        try
        {
            var credential = GoogleCredential.FromJson(authProvider.GetCredentialJson())
                .CreateScoped(DriveService.Scope.DriveFile);

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "CloudSync Windows Service"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Google Drive service. Ensure valid credentials are saved in the vault.");
            _driveService = null;
        }
    }

    public async Task<string> UploadAsync(UploadJob job, CancellationToken cancellationToken)
    {
        if (_driveService == null)
            throw new InvalidOperationException("Google Drive service is not initialized. Check credentials.");

        await _throttler.WaitAsync(cancellationToken);
        try
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(job.LocalPath),
                Parents = string.IsNullOrEmpty(job.RemoteFolderId) ? null : new List<string> { job.RemoteFolderId }
            };

            // Requirement: "Ensure all file I/O uses FileStream with FileOptions.Asynchronous and that the Google Drive UploadAsync method is used to prevent blocking the ThreadPool."
            using var stream = new FileStream(job.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);

            ResumableUpload<Google.Apis.Drive.v3.Data.File, Google.Apis.Drive.v3.Data.File> request;

            if (string.IsNullOrEmpty(job.ExistingGoogleFileId))
            {
                // Create new
                var createRequest = _driveService.Files.Create(fileMetadata, stream, GetMimeType(job.LocalPath));
                createRequest.Fields = "id";
                request = createRequest;
            }
            else
            {
                // Update existing
                var updateRequest = _driveService.Files.Update(fileMetadata, job.ExistingGoogleFileId, stream, GetMimeType(job.LocalPath));
                updateRequest.Fields = "id";
                request = updateRequest;
            }

            request.ChunkSize = ResumableUpload.MinimumChunkSize * 10; // 2.5MB chunks approx

            var progress = await request.UploadAsync(cancellationToken);

            if (progress.Status == UploadStatus.Failed)
            {
                var uploadException = progress.Exception
                    ?? new InvalidOperationException($"Upload failed for '{job.LocalPath}' with no exception detail.");
                _logger.LogError(uploadException, "Upload failed for file {File}", job.LocalPath);
                throw uploadException;
            }

            var fileId = request.ResponseBody?.Id
                ?? throw new InvalidOperationException($"Drive API returned no file ID for '{job.LocalPath}'.");
            return fileId;
        }
        finally
        {
            _throttler.Release();
        }
    }

    private string GetMimeType(string path)
    {
        // simplistic implementation
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".txt" => "text/plain",
            ".pdf" => "application/pdf",
            ".jpg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }
}
