using System.CommandLine;
using Staticsoft.NuGetInspector;

var packageOption = new Option<string>("--package", "NuGet package ID") { IsRequired = true };
var versionOption = new Option<string>("--version", "NuGet package version") { IsRequired = true };
var typeOption = new Option<string>("--type", "Fully qualified type name") { IsRequired = true };

var handlerExitCode = 0;

async Task Run(Func<Inspector, Task<string>> inspect)
{
    try
    {
        var inspector = new Inspector(Directory.GetCurrentDirectory());
        Console.WriteLine(await inspect(inspector));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        handlerExitCode = 1;
    }
}

var listTypesCommand = new Command("list-types", "List all public types in a NuGet package");
listTypesCommand.AddOption(packageOption);
listTypesCommand.AddOption(versionOption);
listTypesCommand.SetHandler((string package, string version) =>
    Run(inspector => inspector.ListTypesAsync(package, version)),
    packageOption, versionOption);

var describeCommand = new Command("describe", "Describe the API surface of a type in a NuGet package");
describeCommand.AddOption(typeOption);
describeCommand.AddOption(packageOption);
describeCommand.AddOption(versionOption);
describeCommand.SetHandler((string type, string package, string version) =>
    Run(inspector => inspector.DescribeTypeAsync(type, package, version)),
    typeOption, packageOption, versionOption);

var rootCommand = new RootCommand("NuGet package API inspector for AI agents");
rootCommand.AddCommand(listTypesCommand);
rootCommand.AddCommand(describeCommand);

var exitCode = await rootCommand.InvokeAsync(args);
return exitCode != 0 ? exitCode : handlerExitCode;
