namespace Staticsoft.NuGetInspector;

public static class FrameworkSelector
{
    static readonly string[] Priority =
    [
        "net9.0", "net8.0", "net7.0", "net6.0", "net5.0",
        "netcoreapp3.1", "netcoreapp3.0", "netcoreapp2.1",
        "netstandard2.1", "netstandard2.0",
        "net48", "net472", "net462", "net461", "net45"
    ];

    public static string SelectDll(string extractDir, string packageId)
    {
        var libDir = Path.Combine(extractDir, "lib");
        if (!Directory.Exists(libDir))
            throw new InvalidOperationException($"No lib/ directory found in {extractDir}");

        foreach (var tfm in Priority)
        {
            var tfmDir = Path.Combine(libDir, tfm);
            if (!Directory.Exists(tfmDir)) continue;

            var named = Path.Combine(tfmDir, $"{packageId}.dll");
            if (File.Exists(named)) return named;

            var first = Directory.GetFiles(tfmDir, "*.dll").FirstOrDefault();
            if (first != null) return first;
        }

        // fallback: pick the TFM directory that exists, prefer highest
        var available = Directory.GetDirectories(libDir)
            .Select(d => (Dir: d, Tfm: Path.GetFileName(d)))
            .ToList();

        foreach (var (dir, _) in available.OrderBy(x => Array.IndexOf(Priority, x.Tfm)))
        {
            var named = Path.Combine(dir, $"{packageId}.dll");
            if (File.Exists(named)) return named;
            var first = Directory.GetFiles(dir, "*.dll").FirstOrDefault();
            if (first != null) return first;
        }

        throw new InvalidOperationException($"No suitable DLL found in {libDir} for package {packageId}");
    }

    public static string GetSelectedFramework(string dllPath)
        => Path.GetFileName(Path.GetDirectoryName(dllPath)!)!;

    public static string? TrySelectDll(string packageDir, string preferredTfm)
    {
        var libDir = Path.Combine(packageDir, "lib");
        if (!Directory.Exists(libDir)) return null;

        var preferred = Path.Combine(libDir, preferredTfm);
        if (Directory.Exists(preferred))
        {
            var dll = Directory.GetFiles(preferred, "*.dll").FirstOrDefault();
            if (dll != null) return dll;
        }

        foreach (var tfm in Priority)
        {
            var tfmDir = Path.Combine(libDir, tfm);
            if (!Directory.Exists(tfmDir)) continue;
            var dll = Directory.GetFiles(tfmDir, "*.dll").FirstOrDefault();
            if (dll != null) return dll;
        }

        return null;
    }
}
