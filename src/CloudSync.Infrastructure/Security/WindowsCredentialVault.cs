using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using CloudSync.Core.Interfaces;

namespace CloudSync.Infrastructure.Security;

/// <summary>
/// A credential vault backed by the Windows Data Protection API (DPAPI).
/// Suitable for services running under a specific service account.
/// </summary>
public class WindowsCredentialVault : ICredentialVault
{
    private readonly string _storageDirectory;

    public WindowsCredentialVault()
    {
        // Store the encrypted credentials in the common application data folder or local app data
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        _storageDirectory = Path.Combine(appData, "CloudSync", "Secure");
        Directory.CreateDirectory(_storageDirectory);
    }

    public void Save(string key, string secret)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("DPAPI is only supported on Windows.");
        }

        var secretBytes = Encoding.UTF8.GetBytes(secret);
        // Protect the data using the current machine scope so the service account can read it.
        // For better security, DataProtectionScope.CurrentUser is preferred if the service runs under a specific user.
        var encryptedBytes = ProtectedData.Protect(secretBytes, null, DataProtectionScope.LocalMachine);
        
        var filePath = GetFilePath(key);
        File.WriteAllBytes(filePath, encryptedBytes);
    }

    public string? Load(string key)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("DPAPI is only supported on Windows.");
        }

        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var encryptedBytes = File.ReadAllBytes(filePath);
            var secretBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(secretBytes);
        }
        catch (CryptographicException)
        {
            // Decryption failed
            return null;
        }
    }

    public void Delete(string key)
    {
        var filePath = GetFilePath(key);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private string GetFilePath(string key)
    {
        // Simple hash for the filename to avoid invalid characters
        var safeKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(key))
            .Replace('/', '_')
            .Replace('+', '-')
            .TrimEnd('=');
        return Path.Combine(_storageDirectory, $"{safeKey}.dat");
    }
}
