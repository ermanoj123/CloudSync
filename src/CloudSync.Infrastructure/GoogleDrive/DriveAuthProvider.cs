using CloudSync.Core.Interfaces;

namespace CloudSync.Infrastructure.GoogleDrive;

public class DriveAuthProvider
{
    private readonly ICredentialVault _vault;

    public DriveAuthProvider(ICredentialVault vault)
    {
        _vault = vault;
    }

    /// <summary>
    /// For demonstration: Retrieves the OAuth refresh token or Service Account JSON 
    /// from the secure vault to build Google Credentials.
    /// </summary>
    public string GetCredentialJson()
    {
        var json = _vault.Load("GoogleDriveServiceAccount");
        if (string.IsNullOrEmpty(json))
        {
            throw new InvalidOperationException("Google Drive credentials not found in vault.");
        }
        return json;
    }
}
