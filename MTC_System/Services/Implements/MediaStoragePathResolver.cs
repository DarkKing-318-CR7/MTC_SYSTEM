namespace MTC_System.Services.Implements;

public static class MediaStoragePathResolver
{
    public static string ResolveStorageRoot(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredRoot = configuration["MediaUpload:StorageRoot"] ?? "App_Data/MediaUploads";
        if (Path.IsPathRooted(configuredRoot))
            throw new InvalidOperationException("MediaUpload:StorageRoot must be relative to the content root.");

        var contentRoot = ResolveCanonicalPath(environment.ContentRootPath);
        var storageRoot = ResolveCanonicalPath(Path.Combine(environment.ContentRootPath, configuredRoot));
        if (!IsSameOrWithin(storageRoot, contentRoot))
            throw new InvalidOperationException("MediaUpload:StorageRoot must remain inside the content root.");

        var webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(contentRoot, "wwwroot")
            : Path.IsPathRooted(environment.WebRootPath)
                ? environment.WebRootPath
                : Path.Combine(contentRoot, environment.WebRootPath);
        webRoot = ResolveCanonicalPath(webRoot);
        if (IsSameOrWithin(storageRoot, webRoot))
            throw new InvalidOperationException("MediaUpload:StorageRoot cannot be equal to or located inside the public web root.");

        return storageRoot;
    }

    public static string ResolveUnderRoot(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidDataException("Stored media path is invalid.");

        var fullRoot = ResolveCanonicalPath(root);
        var fullPath = ResolveCanonicalPath(Path.Combine(fullRoot, relativePath));
        if (!IsSameOrWithin(fullPath, fullRoot) || PathsEqual(fullPath, fullRoot))
            throw new InvalidDataException("Stored media path is invalid.");
        return fullPath;
    }

    private static bool IsSameOrWithin(string candidate, string parent)
    {
        var relative = Path.GetRelativePath(ResolveCanonicalPath(parent), ResolveCanonicalPath(candidate));
        return relative == "." || (!Path.IsPathRooted(relative) &&
            relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, PathComparison) &&
            !relative.StartsWith(".." + Path.AltDirectorySeparatorChar, PathComparison));
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(Path.TrimEndingDirectorySeparator(ResolveCanonicalPath(left)),
            Path.TrimEndingDirectorySeparator(ResolveCanonicalPath(right)), PathComparison);

    private static string ResolveCanonicalPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var pathRoot = Path.GetPathRoot(fullPath)!;
        var current = pathRoot;
        foreach (var component in fullPath[pathRoot.Length..].Split(
                     new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, component);
            FileAttributes attributes;
            try { attributes = File.GetAttributes(current); }
            catch (FileNotFoundException) { continue; }
            catch (DirectoryNotFoundException) { continue; }

            if ((attributes & FileAttributes.ReparsePoint) == 0)
                continue;

            FileSystemInfo link = (attributes & FileAttributes.Directory) != 0
                ? new DirectoryInfo(current)
                : new FileInfo(current);
            current = Path.GetFullPath(link.ResolveLinkTarget(returnFinalTarget: true)?.FullName
                ?? throw new InvalidOperationException("Unable to resolve a Media storage path link safely."));
        }
        return Path.GetFullPath(current);
    }

    private static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;
}
