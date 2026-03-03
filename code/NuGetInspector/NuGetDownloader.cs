using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Staticsoft.NuGetInspector;

public class NuGetDownloader(string? baseDirectory = null)
{
    public async Task<string> EnsurePackageAsync(string packageId, string version)
    {
        var settings = Settings.LoadDefaultSettings(baseDirectory);

        // Check global packages folder first
        var globalFolder = SettingsUtility.GetGlobalPackagesFolder(settings);
        var globalPackageDir = Path.Combine(globalFolder, packageId.ToLower(), version);
        var globalMetadata = Path.Combine(globalPackageDir, ".nupkg.metadata");
        if (File.Exists(globalMetadata))
            return globalPackageDir;

        // Check temp cache
        var tempDir = Path.Combine(Path.GetTempPath(), "nuget-inspector", packageId.ToLower(), version);
        if (Directory.Exists(tempDir) && Directory.GetFiles(tempDir, "*.dll", SearchOption.AllDirectories).Length > 0)
            return tempDir;

        // Download from feeds
        var sourceProvider = new PackageSourceProvider(settings);
        var sources = sourceProvider.LoadPackageSources().Where(s => s.IsEnabled).ToList();

        var nugetVersion = NuGetVersion.Parse(version);
        var logger = NullLogger.Instance;
        var cache = new SourceCacheContext();

        foreach (var source in sources)
        {
            try
            {
                var repo = Repository.Factory.GetCoreV3(source);
                var resource = await repo.GetResourceAsync<FindPackageByIdResource>();
                if (resource == null) continue;

                using var ms = new MemoryStream();
                var found = await resource.CopyNupkgToStreamAsync(packageId, nugetVersion, ms, cache, logger, CancellationToken.None);
                if (!found) continue;

                ms.Position = 0;
                Directory.CreateDirectory(tempDir);
                ExtractNupkg(ms, tempDir);
                return tempDir;
            }
            catch
            {
                // try next source
            }
        }

        throw new InvalidOperationException($"Package {packageId} {version} was not found in any configured source.");
    }

    public async Task<IReadOnlyList<string>> EnsureTransitiveDependenciesAsync(string packageId, string version, string targetFramework)
    {
        var settings = Settings.LoadDefaultSettings(baseDirectory);
        var sourceProvider = new PackageSourceProvider(settings);
        var repos = sourceProvider.LoadPackageSources()
            .Where(s => s.IsEnabled)
            .Select(s => Repository.Factory.GetCoreV3(s))
            .ToList();

        var framework = NuGetFramework.ParseFolder(targetFramework);
        var cache = new SourceCacheContext();
        var logger = NullLogger.Instance;

        var result = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { packageId };
        var queue = new Queue<(string Id, string Version)>();

        await EnqueueDepsAsync(packageId, version, framework, repos, cache, logger, visited, queue);

        while (queue.Count > 0)
        {
            var (depId, depVersion) = queue.Dequeue();
            try
            {
                var dir = await EnsurePackageAsync(depId, depVersion);
                result.Add(dir);
                await EnqueueDepsAsync(depId, depVersion, framework, repos, cache, logger, visited, queue);
            }
            catch { }
        }

        return result;
    }

    async Task EnqueueDepsAsync(string packageId, string version, NuGetFramework framework,
        IReadOnlyList<SourceRepository> repos, SourceCacheContext cache, ILogger logger,
        HashSet<string> visited, Queue<(string, string)> queue)
    {
        var identity = new PackageIdentity(packageId, NuGetVersion.Parse(version));

        foreach (var repo in repos)
        {
            try
            {
                var resource = await repo.GetResourceAsync<DependencyInfoResource>();
                var info = await resource.ResolvePackage(identity, framework, cache, logger, CancellationToken.None);
                if (info == null) continue;

                foreach (var dep in info.Dependencies)
                {
                    if (!visited.Add(dep.Id)) continue;
                    var resolvedVersion = await ResolveVersionAsync(dep.Id, dep.VersionRange, repos, cache, logger);
                    if (resolvedVersion != null)
                        queue.Enqueue((dep.Id, resolvedVersion));
                }
                return;
            }
            catch { }
        }
    }

    async Task<string?> ResolveVersionAsync(string packageId, VersionRange range,
        IReadOnlyList<SourceRepository> repos, SourceCacheContext cache, ILogger logger)
    {
        // Check global packages cache for an already-installed version matching the range
        var settings = Settings.LoadDefaultSettings(baseDirectory);
        var globalFolder = SettingsUtility.GetGlobalPackagesFolder(settings);
        var globalPkgDir = Path.Combine(globalFolder, packageId.ToLower());
        if (Directory.Exists(globalPkgDir))
        {
            var best = Directory.GetDirectories(globalPkgDir)
                .Select(d => NuGetVersion.TryParse(Path.GetFileName(d), out var v) ? v : null)
                .Where(v => v != null && range.Satisfies(v))
                .OrderByDescending(v => v)
                .FirstOrDefault();
            if (best != null) return best.ToNormalizedString();
        }

        // Query sources for the best available version
        foreach (var repo in repos)
        {
            try
            {
                var resource = await repo.GetResourceAsync<FindPackageByIdResource>();
                var versions = await resource.GetAllVersionsAsync(packageId, cache, logger, CancellationToken.None);
                var best = versions.Where(v => range.Satisfies(v)).OrderByDescending(v => v).FirstOrDefault();
                if (best != null) return best.ToNormalizedString();
            }
            catch { }
        }

        return null;
    }

    static void ExtractNupkg(Stream nupkgStream, string targetDir)
    {
        using var archive = new System.IO.Compression.ZipArchive(nupkgStream, System.IO.Compression.ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            // Skip NuGet metadata files and content roots we don't need
            if (entry.FullName.StartsWith("_rels/", StringComparison.OrdinalIgnoreCase)) continue;
            if (entry.FullName.StartsWith("[Content_Types]", StringComparison.OrdinalIgnoreCase)) continue;
            if (entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase)) continue;

            var destPath = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));
            if (!destPath.StartsWith(Path.GetFullPath(targetDir), StringComparison.OrdinalIgnoreCase))
                continue; // safety: skip path traversal

            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
            {
                Directory.CreateDirectory(destPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            using var entryStream = entry.Open();
            using var fileStream = File.Create(destPath);
            entryStream.CopyTo(fileStream);
        }
    }
}
