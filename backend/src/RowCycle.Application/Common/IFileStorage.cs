namespace RowCycle.Application.Common;

/// <summary>
/// Stores uploaded files (product images, submission photos). Local disk in development; cloud storage later (SRS §1).
/// Files get random names and are served read-only, never executed (NFR-04).
/// </summary>
public interface IFileStorage
{
    /// <summary>Saves the file under <paramref name="folder"/> with a random name; returns its public URL (e.g. <c>/media/products/ab12….png</c>).</summary>
    Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken cancellationToken = default);

    Task DeleteAsync(string url, CancellationToken cancellationToken = default);
}
