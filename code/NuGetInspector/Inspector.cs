namespace Staticsoft.NuGetInspector;

public class Inspector(string? baseDirectory = null)
{
    public async Task<string> ListTypesAsync(string packageId, string version)
    {
        var downloader = new NuGetDownloader(baseDirectory);
        var packageDir = await downloader.EnsurePackageAsync(packageId, version);
        var dllPath = FrameworkSelector.SelectDll(packageDir, packageId);
        var tfm = FrameworkSelector.GetSelectedFramework(dllPath);
        var depDirs = await downloader.EnsureTransitiveDependenciesAsync(packageId, version, tfm);
        var info = AssemblyReader.ReadTypes(dllPath, depDirs);
        return OutputFormatter.FormatTypeList(packageId, version, info.AllTypes);
    }

    public async Task<string> DescribeTypeAsync(string typeName, string packageId, string version)
    {
        var downloader = new NuGetDownloader(baseDirectory);
        var packageDir = await downloader.EnsurePackageAsync(packageId, version);
        var dllPath = FrameworkSelector.SelectDll(packageDir, packageId);
        var tfm = FrameworkSelector.GetSelectedFramework(dllPath);
        var depDirs = await downloader.EnsureTransitiveDependenciesAsync(packageId, version, tfm);
        var info = AssemblyReader.ReadTypes(dllPath, depDirs);

        var type = info.AllTypes.FirstOrDefault(t =>
            string.Equals(t.FullName, typeName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase));

        if (type == null)
            throw new InvalidOperationException($"Type '{typeName}' not found in package {packageId} {version}.");

        var assemblyName = Path.GetFileName(dllPath);
        return OutputFormatter.FormatTypeDescription(type, assemblyName);
    }
}
