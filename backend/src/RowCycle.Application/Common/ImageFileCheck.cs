namespace RowCycle.Application.Common;

/// <summary>
/// Checks an upload by its first bytes (its "signature"), not its file name, so a renamed file can't pass (FR-08, NFR-04).
/// </summary>
public static class ImageFileCheck
{
    /// <summary>10 MB, the same limit as submission photos (FR-08).</summary>
    public const long MaxBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Returns <c>.jpg</c>, <c>.png</c> or <c>.webp</c>, or null if the file isn't one of those.
    /// Reads the first 12 bytes and rewinds the stream.
    /// </summary>
    public static async Task<string?> DetectExtensionAsync(Stream content, CancellationToken cancellationToken = default)
    {
        var header = new byte[12];
        var read = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        content.Position = 0;

        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ".jpg";
        }

        if (read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return ".png";
        }

        if (read >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8))
        {
            return ".webp";
        }

        return null;
    }
}
