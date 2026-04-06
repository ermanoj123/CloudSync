namespace CloudSync.Core.Models;

/// <summary>
/// Defines the relationship between a local folder path and a Google Drive parent folder.
/// Loaded from appsettings.json → FolderMappings[].
/// </summary>
public sealed class FolderMapping
{
    /// <summary>Absolute local path to watch and sync (e.g. C:\Documents\Reports).</summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>Google Drive folder ID to upload files into.</summary>
    public string RemoteFolderId { get; set; } = string.Empty;

    /// <summary>Whether to recurse into sub-directories.</summary>
    public bool Recursive { get; set; } = true;

    /// <summary>
    /// Optional glob/extension filter (e.g. "*.pdf;*.docx").
    /// Leave empty to sync all files.
    /// </summary>
    public string FileFilter { get; set; } = string.Empty;

    /// <summary>Human-readable label used in logs.</summary>
    public string Label { get; set; } = string.Empty;
}
