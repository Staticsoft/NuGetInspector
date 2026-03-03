# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

**Build:**
```bash
dotnet build
```

**Run all tests:**
```bash
dotnet test
```

**Run a single test:**
```bash
dotnet test --filter "FullyQualifiedName~ListTypes_ReturnsKnownClass"
```

**Pack and install the tool locally:**
```bash
dotnet pack NuGetInspector/NuGetInspector.csproj -c Release
dotnet tool install --global --add-source ./NuGetInspector/nupkg Staticsoft.NuGetInspector
```

**Run the tool (after install):**
```bash
nuget-inspector list-types --package <id> --version <ver>
nuget-inspector describe --type <FullTypeName> --package <id> --version <ver>
```

## Architecture

This is a .NET global tool (`nuget-inspector`) that inspects NuGet package APIs for AI agent consumption. All namespace roots use `Staticsoft.` prefix (set in `Directory.Build.props` via `RootNamespace`).

**Data flow for both commands:**
1. `Inspector` orchestrates the pipeline
2. `NuGetDownloader` resolves the package: checks global NuGet cache → temp dir (`%TEMP%/nuget-inspector/`) → downloads from feeds in `NuGet.config`
3. `NuGetDownloader.EnsureTransitiveDependenciesAsync` walks the dependency graph recursively to collect dependency DLL dirs (needed for reflection)
4. `FrameworkSelector.SelectDll` picks the best `lib/<tfm>/` DLL using a hardcoded TFM priority list (net9.0 → net8.0 → … → net45)
5. `AssemblyReader.ReadTypes` uses `MetadataLoadContext` (not live loading) with all dependency paths as resolvers, returns a `PackageTypeInfo` record
6. `OutputFormatter` renders plain text output optimized for AI readability

**Key type records** (all in `AssemblyReader.cs`):
- `TypeInfo` — `FullName`, `Name`, `Kind` (CLASS/INTERFACE/ENUM/STRUCT), `Methods`, `Properties`, `EnumValues`
- `TypeMethodInfo` — includes `IsConstructor` and `IsStatic` flags
- `PackageTypeInfo` — wraps `AllTypes` list

**`AssemblyReader` filtering:** Excludes `ToString`, `GetHashCode`, `Equals`, `GetType`. Property accessor methods (`get_*`/`set_*`) are excluded from methods. Enum types skip methods/properties entirely and populate `EnumValues` instead.

## Test Setup

Tests in `NuGetInspector.Tests/` depend on `TestLibrary` being packed to `LocalFeed/` before they run. This is automatic: the test `.csproj` has a `BeforeTargets="Build"` MSBuild target that packs `TestLibrary` into `LocalFeed/` on every build.

`NuGet.config` at the solution root registers `./LocalFeed` as the `local` package source, so `NuGetDownloader` can find `TestLibrary 1.0.0` during tests. Tests instantiate `Inspector` directly via `new Inspector(solutionRoot)` — the solution root is found by walking up from `AppContext.BaseDirectory` until a `*.sln*` file is found.

When adding types to `TestLibrary/SampleTypes.cs`, bump `VersionPrefix` in `TestLibrary/TestLibrary.csproj` and update test assertions accordingly.
