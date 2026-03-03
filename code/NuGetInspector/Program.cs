using System.CommandLine;
using Staticsoft.NuGetInspector;

var packageOption = new Option<string>("--package", "NuGet package ID") { IsRequired = true };
var versionOption = new Option<string>("--version", "NuGet package version") { IsRequired = true };
var typeOption = new Option<string>("--type", "Fully qualified type name") { IsRequired = true };

var listTypesCommand = new Command("list-types", "List all public types in a NuGet package");
listTypesCommand.AddOption(packageOption);
listTypesCommand.AddOption(versionOption);
listTypesCommand.SetHandler(async (string package, string version) =>
{
    var inspector = new Inspector(Directory.GetCurrentDirectory());
    var result = await inspector.ListTypesAsync(package, version);
    Console.WriteLine(result);
}, packageOption, versionOption);

var describeCommand = new Command("describe", "Describe the API surface of a type in a NuGet package");
describeCommand.AddOption(typeOption);
describeCommand.AddOption(packageOption);
describeCommand.AddOption(versionOption);
describeCommand.SetHandler(async (string type, string package, string version) =>
{
    var inspector = new Inspector(Directory.GetCurrentDirectory());
    var result = await inspector.DescribeTypeAsync(type, package, version);
    Console.WriteLine(result);
}, typeOption, packageOption, versionOption);

var rootCommand = new RootCommand("NuGet package API inspector for AI agents");
rootCommand.AddCommand(listTypesCommand);
rootCommand.AddCommand(describeCommand);

return await rootCommand.InvokeAsync(args);
