using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RowCycle.Application.Common;

namespace RowCycle.Infrastructure.Storage;

/// <summary>Saves uploads to a local folder with random names (development; cloud storage can replace it later).</summary>
internal sealed class LocalFileStorage(IOptions<StorageOptions> options, IHostEnvironment environment) : IFileStorage
{
    public string Root => ResolveRoot(options.Value, environment);

    public async Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken cancellationToken = default)
    {
        // Random name: the uploader's file name is never used on disk (NFR-04).
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var directory = Path.Combine(Root, folder);
        Directory.CreateDirectory(directory);

        await using (var file = new FileStream(Path.Combine(directory, fileName), FileMode.CreateNew, FileAccess.Write))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        return $"{options.Value.PublicPath.TrimEnd('/')}/{folder}/{fileName}";
    }

    public Task DeleteAsync(string url, CancellationToken cancellationToken = default)
    {
        var prefix = options.Value.PublicPath.TrimEnd('/') + "/";
        if (!url.StartsWith(prefix, StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        // Only delete inside the storage folder, whatever the URL says.
        var path = Path.GetFullPath(Path.Combine(Root, url[prefix.Length..]));
        if (path.StartsWith(Path.GetFullPath(Root) + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public static string ResolveRoot(StorageOptions options, IHostEnvironment environment) =>
        Path.GetFullPath(Path.IsPathRooted(options.LocalPath)
            ? options.LocalPath
            : Path.Combine(environment.ContentRootPath, options.LocalPath));
}
