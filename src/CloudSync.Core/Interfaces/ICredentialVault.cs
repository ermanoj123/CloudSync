namespace CloudSync.Core.Interfaces;

/// <summary>
/// Abstracts secure storage of credentials.
/// On Windows: backed by the Windows Credential Locker (DPAPI).
/// In tests: backed by an in-memory store.
/// </summary>
public interface ICredentialVault
{
    /// <summary>
    /// Saves a secret value under the given key.
    /// Overwrites any existing value for that key.
    /// </summary>
    void Save(string key, string secret);

    /// <summary>
    /// Retrieves a secret value by key.
    /// Returns null if the key does not exist.
    /// </summary>
    string? Load(string key);

    /// <summary>Removes a key-secret pair from the vault.</summary>
    void Delete(string key);
}
