#!/usr/bin/env dotnet
#:package Spectre.Console
#:package LibGit2Sharp
#:property PublishAot=false
#:project "src/Marmotte.Build/Marmotte.Build.csproj"

using System.Collections.Immutable;
using Marmotte.Build;
using Marmotte.Build.ContinuousIntegration;
using Marmotte.Build.VersionControl;
using Spectre.Console;
using static Marmotte.Build.Shell;
using static Marmotte.Build.Targets;

const string nugetSource = "https://nuget.pkg.github.com/rflechner/index.json";
const string configuration = "Release";
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

PathInfo root = Runtime.GetScriptDirectory();
var solution = root / "Marmotte.Build.slnx";
var project = root / "src" / "Marmotte.Build" / "Marmotte.Build.csproj";
var packagesDirectory = root / "artefacts" / "nugets";

AnsiConsole.Write(new FigletText("Marmotte.Build").Color(Color.Green1));

AnsiConsole.MarkupLine($"[blue]Current branch:[/] {Git.CurrentRepository.Branch}");
AnsiConsole.MarkupLine($"[blue]Current tag:[/] {Git.CurrentRepository.LatestTag?.Name}");

Target("Clean", () =>
{
    if (packagesDirectory.IsDirectory)
        Directory.Delete(packagesDirectory, recursive: true);
    Directory.CreateDirectory(packagesDirectory);

    return Dotnet.Clean(o => o with
    {
        Project = solution,
        Configuration = configuration,
        Properties = properties,
    }, workingDirectory: root);
});

Target("Restore", () => Dotnet.Restore(o => o with
{
    Project = solution,
    Properties = properties,
}, workingDirectory: root));

Target("Build", ["Clean", "Restore"], () => Dotnet.Build(o => o with
{
    Project = solution,
    Configuration = configuration,
    NoRestore = true,
    Properties = properties,
}, workingDirectory: root));

Target("Test", ["Build"], () => Dotnet.Test(o => o with
{
    Solution = solution,
    Configuration = configuration,
    NoBuild = true,
    NoRestore = true,
    Properties = properties,
}, workingDirectory: root));

Target("Pack", ["Test"], () =>
{
    Dotnet.Pack(o => o with
    {
        Project = project,
        Configuration = configuration,
        NoBuild = true,
        NoRestore = true,
        Output = packagesDirectory,
        Properties = properties,
    }, workingDirectory: root).ThrowIfNotFailed();

    if (FindPackages().Length == 0)
        throw new InvalidOperationException($"No NuGet packages were produced in {packagesDirectory}.");

    Console.WriteLine($"Packages created in {packagesDirectory}.");
});

Target("Publish", ["Pack"], () =>
{
    var githubToken = GitHubActions.GithubToken;
    if (string.IsNullOrWhiteSpace(githubToken))
        throw new InvalidOperationException("Publishing requires GITHUB_TOKEN with permission to write GitHub Packages.");
    
    // NuGet in .NET 11 reads the API key from the environment, keeping it out of Shell's command log.
    var previousApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY");
    try
    {
        Environment.SetEnvironmentVariable("NUGET_API_KEY", githubToken);
        Dotnet.NuGet.Push(FindPackages(), o => o with
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
}, when: () => args.Contains("--publish"));

return RunTarget(args, defaultTarget: "Publish");

PathInfo[] FindPackages() => (packagesDirectory / "*.nupkg").GetChildren()
    .Where(path => path.IsFile && !path.Name.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
    .ToArray();
