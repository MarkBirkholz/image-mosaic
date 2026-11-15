using ImageMosaic.Core.Imaging;

namespace ImageMosaic.Core.Tiles;

public static class ImageFileScanner
{
    /// <summary>
    /// Lists image files under <paramref name="root"/> recursively. Extensions are matched
    /// case-insensitively; hidden and system files and folders and unreadable folders are skipped.
    /// The result is sorted so runs are reproducible.
    /// </summary>
    public static IReadOnlyList<string> Scan(string root, CancellationToken cancellationToken = default)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
            ReturnSpecialDirectories = false,
        };

        var files = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*", options))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ImageCodec.IsSupported(file))
            {
                files.Add(file);
            }
        }

        files.Sort(StringComparer.OrdinalIgnoreCase);
        return files;
    }
}
