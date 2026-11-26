#!/usr/bin/env dotnet
// Generates src/Marmotte.Build/Dotnet/Dotnet.<Group>.cs from the help of the installed .NET SDK and Specs.cs.
// Usage: dotnet run tools/generate-dotnet/Generate.cs [-- [--help-dir DIR] [--no-dump] [--output DIR]]
// See README.md next to this file.
#:include "Specs.cs"

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

var repository = Path.GetFullPath(Path.Combine(Generator.Directory(), "..", ".."));
string? helpDir = null;
var dump = true;
var output = Path.Combine(repository, "src", "Marmotte.Build", "Dotnet");

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--help-dir" when i + 1 < args.Length:
            helpDir = Path.GetFullPath(args[++i]);
            break;
        case "--no-dump":
            dump = false;
            break;
        case "--output" when i + 1 < args.Length:
            output = Path.GetFullPath(args[++i]);
            break;
        default:
            Console.WriteLine("""
                Usage: dotnet run tools/generate-dotnet/Generate.cs -- [options]

                  --help-dir DIR  where to write (or read, with --no-dump) the help dumps; a temporary directory by default
                  --no-dump       reuse the help dumps of --help-dir instead of running dotnet
                  --output DIR    the directory of the generated files; src/Marmotte.Build/Dotnet by default
                """);
            return args[i] is "-h" or "--help" ? 0 : 1;
    }
}

if (!dump && helpDir is null)
{
    Console.Error.WriteLine("--no-dump requires --help-dir.");
    return 1;
}

var version = Generator.SdkVersion();
var temporary = Directory.CreateTempSubdirectory("marmotte-help-");
try
{
    helpDir ??= temporary.FullName;
    if (dump)
    {
        Console.WriteLine($"Reading the help of the .NET SDK {version}...");
        Generator.DumpHelp(helpDir);
    }

    Console.WriteLine("Generating...");
    foreach (var group in Specs.Groups)
        Console.WriteLine("  " + Path.GetRelativePath(repository, Generator.EmitGroup(helpDir, output, group, version)));
}
finally
{
    temporary.Delete(recursive: true);
}

return 0;

/// <summary>An option read from a help: its aliases, its value placeholder and its description.</summary>
record HelpOption(List<string> Aliases, string? Placeholder, string Desc);

/// <summary>An option to generate.</summary>
record OptionModel(string Token, string Name, string Type, string Desc, string? FalseToken, bool Joined, bool List);

static class Generator
{
    // Help is read in English; some tools (format, watch) follow the locale instead of DOTNET_CLI_UI_LANGUAGE.
    static readonly Dictionary<string, string> HelpEnvironment = new()
    {
        ["DOTNET_CLI_UI_LANGUAGE"] = "en",
        ["LANG"] = "en_US.UTF-8",
        ["LC_ALL"] = "en_US.UTF-8",
        ["DOTNET_NOLOGO"] = "1",
        ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
        ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1",
    };

    const string MtpGlobalJson = """{ "test": { "runner": "Microsoft.Testing.Platform" } }""";

    static readonly Dictionary<string, string> Words = new()
    {
        ["configfile"] = "ConfigFile", ["nuget"] = "NuGet", ["binarylog"] = "BinaryLog", ["msbuild"] = "MSBuild",
        ["vbcscompiler"] = "VbcsCompiler", ["appsettings"] = "AppSettings", ["nologo"] = "NoLogo",
    };

    static readonly HashSet<string> IntNames = ["take", "skip", "timeout", "min-publish-age-hours"];

    static readonly HashSet<string> PathNames =
    [
        "output", "project", "file", "configfile", "tool-manifest", "tool-path", "manifest", "packages",
        "artifacts-path", "package-directory", "temp-dir", "working-dir", "working-directory", "path",
        "certificate-path", "export-path", "import", "binarylog", "report", "appsettings-file", "results-directory",
    ];

    static readonly Regex PathPlaceholder = new(@"(^|[_\- ])(path|dir|directory|file)($|[_\- ])", RegexOptions.IgnoreCase);

    static readonly Regex ListDescription = new(
        @"multiple times|repeat the option|multiple `--source`|specify once for each|^a list of|space separated list",
        RegexOptions.IgnoreCase);

    static readonly Dictionary<string, string> Plurals = new()
    {
        ["Source"] = "Sources", ["AddSource"] = "AddSources", ["Logger"] = "Loggers", ["Collect"] = "Collectors",
        ["Audience"] = "Audiences", ["Scope"] = "Scopes", ["Role"] = "Roles", ["Claim"] = "Claims", ["Framework"] = "Frameworks",
    };

    static readonly HashSet<string> PreferredAliases =
        ["--add-source", "--configfile", "--project", "--check-only", "--use-current-runtime", "--self-contained"];

    /// <summary>The directory of this file.</summary>
    public static string Directory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

    // Help dumps

    /// <summary>Writes <c>dotnet &lt;command&gt; --help</c> of every command with a help to <c>&lt;helpDir&gt;/&lt;help&gt;.txt</c>.</summary>
    public static void DumpHelp(string helpDir)
    {
        System.IO.Directory.CreateDirectory(helpDir);
        var plain = System.IO.Directory.CreateTempSubdirectory("marmotte-help-plain-");
        var mtp = System.IO.Directory.CreateTempSubdirectory("marmotte-help-mtp-");
        try
        {
            File.WriteAllText(Path.Combine(mtp.FullName, "global.json"), MtpGlobalJson + "\n");
            foreach (var command in Specs.Groups.SelectMany(group => group.Commands).Where(command => command.Help is not null))
            {
                string[] tokens = [.. command.Tokens, .. command.Sub];
                var help = RunDotnet([.. tokens, "--help"], command.Mtp ? mtp.FullName : plain.FullName);
                File.WriteAllText(Path.Combine(helpDir, command.Help + ".txt"), help);
                Console.WriteLine($"  dotnet {string.Join(' ', tokens)} --help");
            }
        }
        finally
        {
            plain.Delete(recursive: true);
            mtp.Delete(recursive: true);
        }
    }

    public static string SdkVersion() => RunDotnet(["--version"], Environment.CurrentDirectory).Trim();

    static string RunDotnet(IEnumerable<string> arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        foreach (var (name, value) in HelpEnvironment)
            startInfo.Environment[name] = value;

        using var process = Process.Start(startInfo)!;
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return standardOutput.Result + standardError.Result;
    }

    // Help parsing

    /// <summary>Parses System.CommandLine or McMaster help.</summary>
    static (string Description, List<HelpOption> Options) ParseHelp(string helpDir, string name)
    {
        var description = new List<string>();
        var options = new List<HelpOption>();
        string? section = null;
        HelpOption? current = null;

        foreach (var rawLine in File.ReadAllText(Path.Combine(helpDir, name + ".txt")).Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var header = Regex.Match(line, @"^([A-Za-z][A-Za-z ]*):\s*$");
            if (header.Success)
            {
                (section, current) = (header.Groups[1].Value, null);
                continue;
            }

            if (section == "Description" && line.Trim().Length > 0)
                description.Add(line.Trim());
            if (section != "Options")
                continue;
            if (line.Trim().Length == 0)
            {
                current = null;
                continue;
            }

            var match = Regex.Match(line, @"^  (-\S.*?)(?:\s{2,}(.*))?$");
            if (match.Success)
            {
                var spec = match.Groups[1].Value;
                var placeholder = Regex.Match(spec, "<([^>]*)>");
                var aliases = Regex.Replace(spec, "<[^>]*>", "").Split(',').Select(alias => alias.Trim()).Where(alias => alias.Length > 0).ToList();
                current = new HelpOption(aliases, placeholder.Success ? placeholder.Groups[1].Value : null, match.Groups[2].Value);
                options.Add(current);
            }
            else if (current is not null)
            {
                options[^1] = current = current with { Desc = current.Desc + " " + line.Trim() };
            }
        }

        if (options.Count == 0 && description.Count == 0)
            throw new InvalidOperationException($"No option nor description found in {name}.txt: check the help dump.");
        return (string.Join(' ', description), options);
    }

    static string OptionToken(List<string> aliases)
    {
        var longAliases = aliases.Where(alias => alias.StartsWith("--")).ToList();
        if (longAliases.FirstOrDefault(PreferredAliases.Contains) is { } preferred)
            return preferred;
        if (longAliases.Count > 0)
            return longAliases.MaxBy(alias => alias.Length)!;
        return "-" + aliases.MaxBy(alias => alias.Length); // "-verbosity" is shown with a single dash
    }

    static string OptionType(string token, string? placeholder, Dictionary<string, string> values)
    {
        var name = token.TrimStart('-');
        if (values.TryGetValue(token, out var type))
            return type;
        if (placeholder is null)
            return "bool";
        if (name == "verbosity" || placeholder == "LEVEL")
            return "Verbosity?";
        if (name == "environment")
            return "ImmutableDictionary<string, string>";
        if (placeholder.Contains('|') || placeholder == "DURATION")
            return "string"; // enumerated values, or a duration such as "10m"
        if (IntNames.Contains(name) || placeholder == "NUMBER")
            return "int?";
        if (PathNames.Contains(name) || PathPlaceholder.IsMatch(placeholder))
            return "PathInfo";
        return "string";
    }

    /// <summary>Typed options of a command, from its help.</summary>
    static List<OptionModel> HelpOptions(string helpDir, Command command)
    {
        var options = new List<OptionModel>();
        foreach (var option in ParseHelp(helpDir, command.Help!).Options)
        {
            if (option.Aliases.Any(alias => alias is "--help" or "-h" or "-?"))
                continue;
            var token = OptionToken(option.Aliases);
            var type = OptionType(token, option.Placeholder, command.Values);
            if (token == "--version" && type == "bool")
                continue; // "show version" flag of the tool itself
            var desc = Clean(option.Desc);
            var list = (command.Lists.Contains(token) || ListDescription.IsMatch(desc)) && type is "string" or "PathInfo";
            var name = Pascal(token);
            options.Add(new(token, list ? Plurals.GetValueOrDefault(name, name) : name, type, desc, null, false, list));
        }

        // --x / --no-x pairs become a single bool?
        var byToken = options.ToDictionary(option => option.Token);
        foreach (var option in options.ToList())
        {
            if (option.Type != "bool" || !byToken.TryGetValue("--no-" + option.Token[2..], out var negated) || negated.Type != "bool")
                continue;
            var index = options.IndexOf(option);
            options[index] = option with
            {
                Type = "bool?",
                FalseToken = negated.Token,
                Desc = $"<see langword=\"true\"/> for <c>{option.Token}</c>, <see langword=\"false\"/> for <c>{negated.Token}</c>, "
                       + $"<see langword=\"null\"/> to let dotnet decide. {option.Desc}",
            };
            options.Remove(negated);
        }

        return options;
    }

    // C# emission

    static string Pascal(string token)
        => string.Concat(token.TrimStart('-').Split('-').Select(part => Words.GetValueOrDefault(part, part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..])));

    static string Camel(string name) => char.ToLowerInvariant(name[0]) + name[1..];

    static string Xml(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    static string Clean(string text)
    {
        text = Regex.Replace(text, @"\s*\[default: [^\]]*\]", "");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        if (text.Length > 0 && !".!?".Contains(text[^1]))
            text += ".";
        return Xml(text);
    }

    static List<string> Doc(string tag, string text, string indent, string attributes = "")
    {
        var prefix = indent + "/// ";
        var oneLine = $"{prefix}<{tag}{attributes}>{text}</{tag}>";
        if (oneLine.Length <= 120)
            return [oneLine];
        return [$"{prefix}<{tag}{attributes}>", .. Wrap(text, 116 - indent.Length).Select(line => prefix + line), $"{prefix}</{tag}>"];
    }

    /// <summary>Greedy word wrap; a word longer than <paramref name="width"/> stays on its own line.</summary>
    static IEnumerable<string> Wrap(string text, int width)
    {
        var line = "";
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                yield return line;
                line = "";
            }

            line = line.Length == 0 ? word : line + " " + word;
        }

        if (line.Length > 0)
            yield return line;
    }

    static List<string> PropertyLines(string type, string name, bool list, string desc, string attribute, string indent)
    {
        var declaration = type switch
        {
            _ when list => $"public IReadOnlyList<{type}> {name} {{ get; init; }} = [];",
            _ when type.StartsWith("ImmutableDictionary") => $"public {type} {name} {{ get; init; }} = {type}.Empty;",
            "bool" or "bool?" or "int?" or "Verbosity?" => $"public {type} {name} {{ get; init; }}",
            _ => $"public {type}? {name} {{ get; init; }}",
        };
        return [.. Doc("summary", desc, indent), indent + attribute, indent + declaration];
    }

    static string ArgumentAttribute(Arg argument)
    {
        if (argument.Position is null)
            return "[CommandArgument]";
        var separator = argument.Separator is null ? "" : $", Separator = \"{argument.Separator}\"";
        return $"[CommandArgument(ArgumentPosition.{argument.Position}{separator})]";
    }

    static string OptionAttribute(OptionModel option)
    {
        var extra = option.FalseToken is null ? "" : $", FalseName = \"{option.FalseToken}\"";
        if (option.Joined)
            extra += ", Joined = true";
        return $"[CommandOption(\"{option.Token}\"{extra})]";
    }

    static (List<string> Lines, List<OptionModel> Options) EmitCommand(string helpDir, Command command, string indent)
    {
        var record = command.Method + "Options";
        List<OptionModel> options;
        string description;
        if (command.Help is null)
        {
            options = [.. command.Options!.Select(o => new OptionModel(o.Token, o.Name, o.Type, o.Desc, o.FalseToken, o.Joined, o.List))];
            description = command.Desc!;
        }
        else
        {
            options = HelpOptions(helpDir, command);
            description = command.Desc ?? Clean(ParseHelp(helpDir, command.Help).Description);
        }

        var baseRecord = command.MSBuild ? "MSBuildCommandOptions" : "CommandOptions";
        var text = Xml("dotnet " + string.Join(' ', [.. command.Tokens, .. command.Sub]));
        var required = command.Args.Where(argument => argument.Required).ToList();

        var lines = Doc("summary", $"Runs <c>{text}</c>. {description}", indent);
        var parameters = new List<string>();
        var assignments = new List<string>();
        foreach (var argument in required)
        {
            var parameter = Camel(argument.Name);
            lines.AddRange(Doc("param", argument.Desc, indent, $" name=\"{parameter}\""));
            parameters.Add(argument.Variadic ? $"IEnumerable<{argument.Type}> {parameter}" : $"{argument.Type} {parameter}");
            assignments.Add(argument.Variadic ? $"{argument.Name} = [.. {parameter}]" : $"{argument.Name} = {parameter}");
        }

        lines.AddRange(Doc("param", $"Customizes the options, starting from <see cref=\"{record}.Default\"/>"
                                    + (required.Count > 0 ? " with the arguments above" : "")
                                    + ". When omitted, the options are used as they are.", indent, " name=\"configure\""));
        lines.AddRange(Doc("param", "The directory to run in, or <see langword=\"null\"/> for the current directory.", indent, " name=\"workingDirectory\""));
        parameters.AddRange([$"Func<{record}, {record}>? configure = null", "PathInfo? workingDirectory = null"]);
        var defaults = $"{record}.Default" + (assignments.Count > 0 ? $" with {{ {string.Join(", ", assignments)} }}" : "");
        var tokens = string.Join(", ", command.Tokens.Select(token => $"\"{token}\""));
        var sub = string.Join(", ", command.Sub.Select(token => $"\"{token}\""));
        lines.Add($"{indent}public static Shell.ShellExecuteResult {command.Method}(");
        lines.AddRange(parameters.Select((parameter, i) => $"{indent}    {parameter}{(i < parameters.Count - 1 ? "," : ")")}"));
        lines.Add($"{indent}    => RunCommand([{tokens}], [{sub}], {defaults}, configure, workingDirectory);");
        lines.Add("");

        var inner = indent + "    ";
        lines.AddRange(Doc("summary", $"Options of <c>{text}</c>. Every option is unset by default, so dotnet applies its own default.", indent));
        lines.AddRange([$"{indent}public sealed record {record} : {baseRecord}", indent + "{"]);
        lines.AddRange(Doc("summary", "The default options, which set nothing.", inner));
        lines.Add($"{inner}public static {record} Default {{ get; }} = new();");

        var members = command.Args.Select(a => (a.Name, Lines: PropertyLines(a.Type, a.Name, a.Variadic, a.Desc, ArgumentAttribute(a), inner)))
            .Concat(options.Select(o => (o.Name, Lines: PropertyLines(o.Type, o.Name, o.List, o.Desc, OptionAttribute(o), inner))));
        var names = new HashSet<string>();
        foreach (var (name, memberLines) in members)
        {
            if (!names.Add(name))
                throw new InvalidOperationException($"{text}: duplicate member {name}");
            lines.Add("");
            lines.AddRange(memberLines);
        }

        lines.Add(indent + "}");
        return (lines, options);
    }

    /// <summary>Writes <c>Dotnet.&lt;File&gt;.cs</c> and returns its path.</summary>
    public static string EmitGroup(string helpDir, string outputDir, Group group, string version)
    {
        var indent = group.Class is null ? "    " : "        ";
        var body = new List<string>();
        var usesImmutable = false;
        foreach (var command in group.Commands)
        {
            var (lines, options) = EmitCommand(helpDir, command, indent);
            usesImmutable |= options.Any(option => option.Type.StartsWith("ImmutableDictionary"));
            if (body.Count > 0)
                body.Add("");
            body.AddRange(lines);
        }

        List<string> output =
        [
            $"// Generated by tools/generate-dotnet/Generate.cs from the help of the .NET SDK {version}.",
            "// To change this file, edit tools/generate-dotnet/Specs.cs and run the generator again.",
            "",
        ];
        if (usesImmutable)
            output.AddRange(["using System.Collections.Immutable;", ""]);
        output.AddRange(["namespace Marmotte.Build;", "", "public static partial class Dotnet", "{"]);
        if (group.Class is null)
        {
            output.AddRange(body);
        }
        else
        {
            output.AddRange(Doc("summary", group.Desc!, "    "));
            output.AddRange(["    public static class " + group.Class, "    {", .. body, "    }"]);
        }

        output.Add("}");
        var path = Path.Combine(outputDir, $"Dotnet.{group.File}.cs");
        File.WriteAllText(path, string.Join('\n', output.Select(line => line.TrimEnd())) + "\n");
        return path;
    }
}
