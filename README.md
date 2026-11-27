# Marmotte.Build

![Marmotte.Build: the turquoise mascot coding a C# build pipeline on a laptop above a wooden sign](docs/images/marmotte-build-banner.png)

A minimalist C# DSL for scripting builds with .NET file-based apps.

Write your build as a C# file, compose paths with `/`, find files with globs, and run commands with explicit results. Build steps follow normal C# control flow: variables, functions, loops, and pattern matching.

## Project status

> **Experimental proof of concept — not production-ready.**
> The API is still evolving and may change without compatibility guarantees.

<img src="docs/images/references/experimental-proof-of-concept.png" alt="The Marmotte mascot experimenting beside an Experimental Proof of Concept sign" width="320" />

The current implementation provides the following building blocks:

| API | Purpose |
| --- | --- |
| `GitHubActions` | Read GitHub Actions and runner variables with IDE completion. |
| `PathInfo` | Compose paths, check files and directories, and enumerate glob matches. |
| `Shell` | Run an executable synchronously, with separate arguments and an optional working directory. |
| `Dotnet` | Call the .NET CLI through small wrappers such as `Build`, `Test`, and `Pack`. |

Your script controls execution order and decides how to handle failures. There is no task graph, dependency scheduler, or incremental build engine in Marmotte.Build itself.

## Prerequisites

- **.NET 11 SDK**: the projects currently target `net11.0`, and `Shell` uses C# union types.
- Any executable invoked by your script must be available on `PATH`, or supplied by path.
- NuGet access for the initial dependency restore.

[Directory.Build.props](Directory.Build.props) defines the target framework and language version. [global.json](global.json) selects Microsoft.Testing.Platform for tests; it does not pin an SDK version.

## Try the existing example

From the repository root:

```sh
dotnet run --file build.cs
```

The checked-in [build.cs](build.cs) is a small path exploration demo: it loads `PathInfo.cs` with `#:include`, uses Spectre.Console for output, and lists the library's project file. It does not yet build or test the solution.

## Write a build script

The following example uses the sources directly. Save it as `build-pipeline.cs` at the repository root, then run it from that directory. The `#:include` directives bring the three building blocks into the file-based app without a separate script project.

```csharp
#!/usr/bin/env dotnet
#:include "src/Marmotte.Build/PathInfo.cs"
#:include "src/Marmotte.Build/Shell.cs"
#:include "src/Marmotte.Build/Dotnet.cs"

using Marmotte.Build;
using static Marmotte.Build.Shell;

PathInfo root = PathInfo.CurrentDirectory;
var configuration = args.Length > 0 ? args[0] : "Release";

foreach (var project in (root / "src" / "**" / "*.csproj").GetChildren())
    Console.WriteLine($"Project: {project}");

var buildExitCode = GetExitCode(Dotnet.Build(
    ["Marmotte.Build.slnx", "--configuration", configuration],
    workingDirectory: root));

if (buildExitCode != 0)
    return buildExitCode;

return GetExitCode(Dotnet.Test(
    ["--solution", "Marmotte.Build.slnx", "--configuration", configuration, "--no-build"],
    workingDirectory: root));

static int GetExitCode(ShellExecuteResult result)
{
    switch (result.Value)
    {
        case ShellExecuteSuccess:
            return 0;
        case ShellExecuteFailure failure:
            return failure.ExitCode;
        case ShellExecuteNotStarted notStarted:
            Console.Error.WriteLine($"Could not start {notStarted.Command}: {notStarted.Reason}");
            return 1;
        default:
            throw new InvalidOperationException("Unexpected command result.");
    }
}
```

```sh
dotnet run --file build-pipeline.cs
dotnet run --file build-pipeline.cs -- Debug
```

The script builds the solution, runs tests only after a successful build, and returns a non-zero exit code on failure. Command failures are returned as values; ignoring a result does not automatically stop the script.

## Read GitHub Actions variables

In a file-based build script, include the helper and type `GitHubActions.` to browse workflow variables or `GitHubActions.Runner.` for runner details.

```csharp
#!/usr/bin/env dotnet
#:include "src/Marmotte.Build/GitHubActions.cs"

using Marmotte.Build;

if (GitHubActions.IsRunning)
    Console.WriteLine($"{GitHubActions.Repository} run #{GitHubActions.RunNumber} on {GitHubActions.Runner.OS}");

if (GitHubActions.HeadRef is { Length: > 0 } source && GitHubActions.BaseRef is { Length: > 0 } target)
    Console.WriteLine($"Validating {source} into {target}");
```

Each string property reads the current environment value and returns `null` if the variable is absent. `IsRunning` checks `GITHUB_ACTIONS`. Use `GitHubActions.GetVariable("BUILD_CONFIGURATION")` for a custom name exported through the workflow's `env`. The [GitHub Actions variables reference](https://docs.github.com/en/actions/reference/workflows-and-actions/variables#default-environment-variables) lists the defaults and when they are available.

## Paths and globs

```csharp
PathInfo root = PathInfo.CurrentDirectory;
PathInfo source = root / "src";
PathInfo project = source / "Marmotte.Build" / "Marmotte.Build.csproj";

Console.WriteLine(project.IsFile);

foreach (var file in (source / "**" / "*.cs").GetChildren())
    Console.WriteLine(file);
```

`PathInfo` converts implicitly to and from `string`. It normalizes the platform's alternate directory separator and trims a trailing separator, preserving filesystem roots. Combining paths follows `Path.Combine`: a rooted right-hand operand replaces the left-hand path.

| Pattern | Matches |
| --- | --- |
| `src / "*.cs"` | C# files directly under `src`. |
| `src / "?est.cs"` | Names with exactly one character before `est.cs`. |
| `src / "*" / "*.csproj"` | Project files one directory below `src`. |
| `src / "**" / "*.cs"` | C# files at any depth under `src`, including directly inside it. |
| `src / "**"` | All descendant files and directories. |

Here, `src` represents a `PathInfo` variable. `GetChildren()` enumerates lazily, returns direct children for an ordinary directory, and returns no entries for a file or a missing path. Results include files and directories where the pattern permits, include hidden entries, and have no guaranteed order. Relative patterns produce relative paths. Wildcard matching uses the platform's default case sensitivity.

## Run commands

```csharp
var version = Shell.Execute("dotnet", "--version");

var build = Shell.Execute(
    "dotnet",
    ["build", "My Project.csproj", "--configuration", "Release"],
    workingDirectory: PathInfo.CurrentDirectory / "src");
```

Each argument is passed separately, so a path containing spaces remains one argument. `Shell.Execute` starts the executable directly: shell operators such as `|`, `&&`, and `>` are not interpreted. Output goes to the current console; the API does not capture stdout or stderr.

Both `Shell` and `Dotnet` return `Shell.ShellExecuteResult`. Inspect its `.Value` with pattern matching:

| Result | Meaning |
| --- | --- |
| `ShellExecuteSuccess` | The process exited with code `0`. |
| `ShellExecuteFailure` | The process exited with a non-zero `ExitCode`. |
| `ShellExecuteNotStarted` | The process could not start; `Command` and `Reason` describe the problem. This also covers a missing working directory. |

Execution waits for the process to exit. Cancellation, timeouts, asynchronous execution, and output capture are not currently exposed.

## Use the .NET CLI

```csharp
var build = Dotnet.Build(["--configuration", "Release"]);
var pack = Dotnet.Pack(["--configuration", "Release", "--output", "artifacts"]);
var tools = Dotnet.Tool(["restore"]);
var info = Dotnet.Execute("--info");
```

These are independent examples; handle each result when composing a pipeline. Every wrapper accepts an optional argument collection and `workingDirectory`. Arguments are forwarded to the installed .NET CLI without validation or additional defaults. Use `Dotnet.Execute` for commands without a dedicated wrapper or for global options. See [Dotnet.cs](src/Marmotte.Build/Dotnet.cs) for the complete set of wrappers.

## Build and test the library

From the repository root:

```sh
dotnet restore Marmotte.Build.slnx
dotnet build Marmotte.Build.slnx --configuration Release --no-restore
dotnet test --solution Marmotte.Build.slnx --configuration Release --no-build --no-restore
```

The tests cover path composition and normalization, glob matching, process results, argument handling, working directories, and .NET command execution. Some process tests intentionally invoke failing commands, so their console output includes expected errors. Two tests use the Unix `test` executable and are skipped on Windows.

## Mascot and license

Marmotte.Build shares the turquoise mascot and red scarf used by [Marmotte.ChatUi](https://github.com/rflechner/Marmotte.ChatUi). Its banner adapts that visual identity to show the mascot coding a C# build pipeline above the Marmotte.Build sign. All images are stored locally; see [image provenance](docs/images/README.md).

Licensed under the [BSD 3-Clause License](LICENSE).
