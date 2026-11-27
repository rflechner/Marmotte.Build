// #!/usr/bin/env dotnet
#:package Spectre.Console
#:include "src/Marmotte.Build/PathInfo.cs"
#:include "src/Marmotte.Build/CommandOptions.cs"
#:include "src/Marmotte.Build/Shell.cs"
#:include "src/Marmotte.Build/Dotnet/MSBuildCommandOptions.cs"
#:include "src/Marmotte.Build/Dotnet/Dotnet.cs"
#:include "src/Marmotte.Build/Dotnet/Dotnet.Clean.cs"
#:include "src/Marmotte.Build/Dotnet/Dotnet.Build.cs"
#:include "src/Marmotte.Build/Dotnet/Dotnet.Restore.cs"
#:include "src/Marmotte.Build/Dotnet/Dotnet.Pack.cs"

using Spectre.Console;
using Marmotte.Build;

AnsiConsole.Write(new FigletText("Build Marmotte.Build !").Color(Color.Green1));


PathInfo srcPath = PathInfo.CurrentDirectory / "src";
PathInfo marmotteBuildFolder = srcPath / "Marmotte.Build";

AnsiConsole.MarkupLine("Marmotte.Build [green1]folder[/]");
AnsiConsole.MarkupLine($"[green1]{marmotteBuildFolder}[/]");

var artefactsFolder = (PathInfo)"artefacts";
var artefactsNugetsFolder = (PathInfo)"artefacts" / "nugets";

Directory.Delete(artefactsFolder, recursive: true);

PathInfo marmotteBuildProjects = srcPath / "Marmotte.Build" / "*.csproj";
foreach (var child in marmotteBuildProjects.GetChildren())
{
    AnsiConsole.MarkupLine($"[green]{child}[/]");
    
    AnsiConsole.MarkupLine("Marmotte.Build [white]Building Marmotte.Build[/]");

    Dotnet.Clean(o => o with
    {
        Project = child
    });
    
    Dotnet.Restore(o => o with
    {
        Project = child
    });
    
    Dotnet.Build(o => o with
    {
        Project = child,
        Output = artefactsFolder / child.NameWithoutExtension
    });
    
    Dotnet.Pack(o => o with
    {
        Project = child,
        Output = artefactsNugetsFolder
    });
    
}

