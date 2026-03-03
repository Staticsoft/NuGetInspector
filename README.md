# NuGetInspector

A .NET global tool that inspects NuGet package public APIs and formats the output for AI agent consumption.

## Installation

```bash
dotnet tool install --global Staticsoft.NuGetInspector
```

## Usage

### List all public types in a package

```bash
nuget-inspector list-types --package Newtonsoft.Json --version 13.0.3
```

Example output:
```
Package: Newtonsoft.Json 13.0.3

CLASSES (42):
  Newtonsoft.Json.JsonConvert
  Newtonsoft.Json.JsonSerializer
  ...

INTERFACES (8):
  Newtonsoft.Json.IJsonLineInfo
  ...

ENUMS (12):
  Newtonsoft.Json.DateFormatHandling
  ...

STRUCTS (3):
  Newtonsoft.Json.Schema.JsonSchemaType
  ...
```

### Describe the API of a specific type

```bash
nuget-inspector describe --type Newtonsoft.Json.JsonConvert --package Newtonsoft.Json --version 13.0.3
```

Example output:
```
CLASS: Newtonsoft.Json.JsonConvert
Assembly: Newtonsoft.Json.dll

STATIC METHODS:
  string SerializeObject(object value)
  string SerializeObject(object value, Formatting formatting)
  object DeserializeObject(string value)
  T DeserializeObject<T>(string value)
  ...

PROPERTIES:
  [static] JsonSerializerSettings DefaultSettings { get; set; }
```

## How It Works

The tool resolves packages through the NuGet feeds configured in `NuGet.config` at the working directory (falling back to standard NuGet settings). Packages are cached in the global NuGet packages folder; ones not already cached are downloaded to a temp directory.

Transitive dependencies are resolved automatically so that reflection works correctly. Assembly inspection uses `MetadataLoadContext` — packages are never executed.

The TFM selection priority is: `net9.0` → `net8.0` → `net7.0` → `net6.0` → `net5.0` → `netcoreapp3.1` → `netstandard2.1` → `netstandard2.0` → `net48` and older.

## Development

See [code/CLAUDE.md](code/CLAUDE.md) for build commands, architecture notes, and test setup details.
