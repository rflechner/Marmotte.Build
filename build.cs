#!/usr/bin/env dotnet
#:package Spectre.Console
#:include "src/Marmotte.Build/Runtime.cs"
#:include "src/Marmotte.Build/PathInfo.cs"
#:include "src/Marmotte.Build/CommandOptions.cs"
#:include "src/Marmotte.Build/Shell.cs"
#:include "src/Marmotte.Build/Dotnet/MSBuildCommandOptions.cs"
#:include "src/Marmotte.Build/Dotnet/Dotnet.cs"
#:include "src/Marmotte.Build/Dotnet/Dotnet.Clean.cs"
#:include "src/Marmotte.Build/Dotnet/Dotnet.Build.cs"
#:include "src/Marmotte.Build/Dotnet/Dotnet.Restore.cs"
#:include "src/Marmotte.Build/Dotnet/Dotnet.Test.cs"
#:include "src/Marmotte.Build/Dotnet/Dotnet.Pack.cs"
#:include "src/Marmotte.Build/Dotnet/Dotnet.NuGet.cs"
#:include "src/Marmotte.Build/ContinuousIntegration/GitHubActions.cs"

using System.Collections.Immutable;
using Marmotte.Build;
using Marmotte.Build.ContinuousIntegration;
using Spectre.Console;
using static Marmotte.Build.Shell;

const string nugetSource = "https://nuget.pkg.github.com/rflechner/index.json";
const string configuration = "Release";
var publish = args.Contains("--publish");
var properties = ImmutableDictionary<string, string>.Empty;

var versionIndex = Array.IndexOf(args, "--version");
if (versionIndex >= 0)
{
    if (versionIndex + 1 >= args.Length || string.IsNullOrWhiteSpace(args[versionIndex + 1])
        || args[versionIndex + 1].StartsWith('-'))
    {
        Console.Error.WriteLine("--version requires a NuGet version, such as 1.2.3 or 1.2.3-preview.1.");
        return 1;
    }

    properties = properties.SetItem("Version", args[versionIndex + 1]);
}
else if (GitHubActions.RefType == "tag")
{
    var version = GitHubActions.RefName;
    if (version?.StartsWith('v') == true)
        version = version[1..];

    if (string.IsNullOrWhiteSpace(version))
    {
        Console.Error.WriteLine("GITHUB_REF_NAME must contain a NuGet version when GITHUB_REF_TYPE is tag.");
        return 1;
    }

    properties = properties.SetItem("Version", version);
}

var githubToken = GitHubActions.GithubToken;
if (publish && string.IsNullOrWhiteSpace(githubToken))
{
    Console.Error.WriteLine("Publishing requires GITHUB_TOKEN with permission to write GitHub Packages.");
    return 1;
}

PathInfo root = Runtime.GetScriptDirectory();
var solution = root / "Marmotte.Build.slnx";
var project = root / "src" / "Marmotte.Build" / "Marmotte.Build.csproj";
var packagesDirectory = root / "artefacts" / "nugets";

AnsiConsole.Write(new FigletText("Marmotte.Build").Color(Color.Green1));

if (packagesDirectory.IsDirectory)
    Directory.Delete(packagesDirectory, recursive: true);
Directory.CreateDirectory(packagesDirectory);

var steps = new Dictionary<string, Func<ShellExecuteResult>>();
steps.Add("Clean", () => Dotnet.Clean(o => o with
    {
        Project = solution,
        Configuration = configuration,
        Properties = properties,
    }, workingDirectory: root));

steps.Add("Restore", () => Dotnet.Restore(o => o with
{
    Project = solution,
    Properties = properties,
}, workingDirectory: root));

steps.Add("Build", () => Dotnet.Build(o => o with
{
    Project = solution,
    Configuration = configuration,
    NoRestore = true,
    Properties = properties,
}, workingDirectory: root));

steps.Add("Test", () => Dotnet.Test(o => o with
{
    Solution = solution,
    Configuration = configuration,
    NoBuild = true,
    NoRestore = true,
    Properties = properties,
}, workingDirectory: root));

steps.Add("Pack", () => Dotnet.Pack(o => o with
{
    Project = project,
    Configuration = configuration,
    NoBuild = true,
    NoRestore = true,
    Output = packagesDirectory,
    Properties = properties,
}, workingDirectory: root));

foreach (var (name, step) in steps)
{
    AnsiConsole.MarkupLine($"[gold3]Running step {name}...[/]");
    step().ThrowIfNotFailed();
}

var packages = (packagesDirectory / "*.nupkg").GetChildren()
    .Where(path => path.IsFile && !path.Name.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
    .ToArray();
if (packages.Length == 0)
{
    Console.Error.WriteLine($"No NuGet packages were produced in {packagesDirectory}.");
    return 1;
}

Console.WriteLine($"Packages created in {packagesDirectory}.");
if (!publish)
    return 0;

// NuGet in .NET 11 reads the API key from the environment, keeping it out of Shell's command log.
var previousApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY");
try
{
    Environment.SetEnvironmentVariable("NUGET_API_KEY", githubToken);
    Dotnet.NuGet.Push(packages, o => o with
    {
        Source = nugetSource,
        SkipDuplicate = true,
        NoSymbols = true,
    }, workingDirectory: root).ThrowIfNotFailed();
}
finally
{
    Environment.SetEnvironmentVariable("NUGET_API_KEY", previousApiKey);
}

return 0;
