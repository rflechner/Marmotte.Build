// #!/usr/bin/env dotnet
#:package Spectre.Console
#:include "src/Marmotte.Build/PathInfo.cs"

using Spectre.Console;
using Marmotte.Build;

AnsiConsole.Write(new FigletText("Build Marmotte.Build !").Color(Color.Green1));


PathInfo srcPath = PathInfo.CurrentDirectory / "src";
PathInfo marmotteBuildFolder = srcPath / "Marmotte.Build";

AnsiConsole.MarkupLine("Marmotte.Build [green1]folder[/]");
AnsiConsole.MarkupLine($"[green1]{marmotteBuildFolder}[/]");


PathInfo marmotteBuildChildren = srcPath / "Marmotte.Build" / "*.csproj";
foreach (var child in marmotteBuildChildren.GetChildren())
{
    AnsiConsole.MarkupLine($"[green]{child}[/]");
}
