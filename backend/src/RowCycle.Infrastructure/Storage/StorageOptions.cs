namespace RowCycle.Infrastructure.Storage;

/// <summary>Section <c>Storage</c>.</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Folder for uploads, outside the API's code folder (NFR-04). Relative paths are resolved from the app's folder.</summary>
    public string LocalPath { get; set; } = "storage";

    /// <summary>URL prefix the files are served under (read-only).</summary>
    public string PublicPath { get; set; } = "/media";
}
