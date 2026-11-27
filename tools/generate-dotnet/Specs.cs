// What Generate.cs cannot infer from `dotnet <command> --help`: the shape of the C# API of each command.

/// <summary>
/// A group of commands, generated in <c>Dotnet.&lt;File&gt;.cs</c>. With a <paramref name="Class"/>, the commands are
/// methods of a nested static class of <c>Dotnet</c> (commands with subcommands, such as <c>Dotnet.Tool</c>);
/// without, they are methods of <c>Dotnet</c> itself.
/// </summary>
record Group(string File, string? Class, string? Desc, Command[] Commands);

/// <summary>A dotnet command.</summary>
/// <param name="Help">
/// The name of its help dump (<c>&lt;Help&gt;.txt</c>), or <see langword="null"/> when every option is in <see cref="Options"/>.
/// </param>
/// <param name="Method">The name of the C# method; its options record is <c>&lt;Method&gt;Options</c>.</param>
/// <param name="Tokens">The command tokens, such as <c>["tool", "install"]</c>.</param>
record Command(string? Help, string Method, string[] Tokens)
{
    /// <summary>Tokens placed after the BeforeSubcommand arguments, such as <c>["add"]</c> in <c>dotnet solution [&lt;SLN&gt;] add</c>.</summary>
    public string[] Sub { get; init; } = [];

    /// <summary>Positional arguments; the required ones become parameters of the method.</summary>
    public Arg[] Args { get; init; } = [];

    /// <summary>Whether the command accepts MSBuild properties (<c>-p:Name=Value</c>).</summary>
    public bool MSBuild { get; init; }

    /// <summary>Whether to read the help with Microsoft.Testing.Platform selected in <c>global.json</c>.</summary>
    public bool Mtp { get; init; }

    /// <summary>The description of the command, when the help has none or a poor one.</summary>
    public string? Desc { get; init; }

    /// <summary>Options that can be repeated, when the help does not say so.</summary>
    public string[] Lists { get; init; } = [];

    /// <summary>Options that take a value, mapped to their type, for help without placeholders (McMaster style).</summary>
    public Dictionary<string, string> Values { get; init; } = [];

    /// <summary>Hand-written options, for help that is not in the System.CommandLine format (msbuild, fsi, vstest).</summary>
    public Opt[]? Options { get; init; }
}

/// <summary>A positional argument.</summary>
/// <param name="Position"><see langword="null"/> (before the options), <c>BeforeSubcommand</c> or <c>AfterOptions</c>.</param>
/// <param name="Separator">A token passed before the values, such as <c>--</c>.</param>
record Arg(
    string Name,
    string Type = "string",
    bool Required = false,
    bool Variadic = false,
    string? Position = null,
    string? Separator = null,
    string Desc = "");

/// <summary>A hand-written option.</summary>
record Opt(string Token, string Name, string Type, string Desc, string? FalseToken = null, bool Joined = false, bool List = false);

static class Specs
{
    static readonly Arg Project = new("Project", "PathInfo", Desc: "The project, solution or C# file to operate on. When unset, dotnet looks for a project or a solution in the working directory.");

    static readonly Arg Solution = new("Solution", "PathInfo", Position: "BeforeSubcommand", Desc: "The solution file. When unset, dotnet looks for one in the working directory.");

    // Value options of the McMaster-style help of user-jwts and user-secrets, which shows no placeholder.
    static readonly Dictionary<string, string> JwtsValues = new()
    {
        ["--project"] = "PathInfo", ["--file"] = "PathInfo", ["--output"] = "string", ["--scheme"] = "string",
        ["--name"] = "string", ["--audience"] = "string", ["--issuer"] = "string", ["--scope"] = "string",
        ["--role"] = "string", ["--claim"] = "string", ["--not-before"] = "string", ["--expires-on"] = "string",
        ["--valid-for"] = "string", ["--appsettings-file"] = "PathInfo",
    };

    static readonly Dictionary<string, string> SecretsValues = new() { ["--id"] = "string" };

    public static readonly Group[] Groups =
    [
        new("Build", null, null,
        [
            new("build", "Build", ["build"]) { MSBuild = true, Args = [Project] },
        ]),
        new("BuildServer", "BuildServer", "Runs <c>dotnet build-server</c> commands: manages the servers started by a build.",
        [
            new("build-server_shutdown", "Shutdown", ["build-server", "shutdown"]),
        ]),
        new("Clean", null, null,
        [
            new("clean", "Clean", ["clean"]) { MSBuild = true, Args = [Project] },
        ]),
        new("DevCerts", "DevCerts", "Runs <c>dotnet dev-certs</c> commands: creates and manages development certificates.",
        [
            new("dev-certs_https", "Https", ["dev-certs", "https"])
            {
                Desc = "Creates, exports, checks, trusts or cleans the HTTPS development certificate.",
                Values = new() { ["--export-path"] = "PathInfo", ["--password"] = "string", ["--import"] = "PathInfo", ["--format"] = "string" },
            },
        ]),
        new("Format", null, null,
        [
            new("format", "Format", ["format"])
            {
                Args = [Project],
                Desc = "Formats code to match editorconfig settings: whitespace, code style and third-party analyzers.",
            },
            new("format_whitespace", "FormatWhitespace", ["format", "whitespace"]) { Args = [Project] },
            new("format_style", "FormatStyle", ["format", "style"]) { Args = [Project] },
            new("format_analyzers", "FormatAnalyzers", ["format", "analyzers"]) { Args = [Project] },
        ]),
        new("Fsi", null, null,
        [
            new(null, "Fsi", ["fsi"])
            {
                Desc = "Starts F# Interactive, or runs an F# script.",
                Args =
                [
                    new("Script", "PathInfo", Position: "AfterOptions", Desc: "The F# script to run. When unset, F# Interactive starts in interactive mode."),
                    new("ScriptArguments", Variadic: true, Position: "AfterOptions", Desc: "The arguments passed to the script."),
                ],
                Options =
                [
                    new("--use:", "Use", "PathInfo", "Uses the given files on startup as initial input.", Joined: true, List: true),
                    new("--load:", "Load", "PathInfo", "Loads the given files on startup, as with <c>#load</c>.", Joined: true, List: true),
                    new("--reference:", "References", "PathInfo", "References assemblies.", Joined: true, List: true),
                    new("--lib:", "IncludePaths", "PathInfo", "Directories searched for source files and referenced assemblies.", Joined: true, List: true),
                    new("--define:", "Defines", "string", "Defines conditional compilation symbols.", Joined: true, List: true),
                    new("--nowarn:", "NoWarn", "string", "Disables specific warnings.", Joined: true, List: true),
                    new("--warnaserror+", "WarnAsError", "bool?", "Reports all warnings as errors.", FalseToken: "--warnaserror-"),
                    new("--langversion:", "LangVersion", "string", "The F# language version, such as <c>latest</c> or <c>preview</c>.", Joined: true),
                    new("--optimize+", "Optimize", "bool?", "Enables optimizations.", FalseToken: "--optimize-"),
                    new("--debug+", "Debug", "bool?", "Emits debug information.", FalseToken: "--debug-"),
                    new("--checked+", "Checked", "bool?", "Generates overflow checks.", FalseToken: "--checked-"),
                    new("--targetprofile:", "TargetProfile", "string", "The target framework profile, such as <c>netcore</c>.", Joined: true),
                    new("--exec", "Exec", "bool", "Exits F# Interactive after loading the files or running the script."),
                    new("--quiet", "Quiet", "bool", "Suppresses F# Interactive writing to stdout."),
                    new("--nologo", "NoLogo", "bool", "Suppresses the copyright message."),
                    new("--typecheck-only", "TypeCheckOnly", "bool", "Type checks only, without running."),
                ],
            },
        ]),
        new("Help", null, null,
        [
            new("help", "Help", ["help"])
            {
                Args = [new("Command", Variadic: true, Desc: "The command to open the documentation of, such as <c>build</c>. When empty, opens the general documentation.")],
            },
        ]),
        new("MSBuild", null, null,
        [
            new(null, "MSBuild", ["msbuild"])
            {
                MSBuild = true,
                Desc = "Runs MSBuild directly.",
                Args = [new("Project", "PathInfo", Desc: "The project or solution to build. When unset, MSBuild looks for one in the working directory.")],
                Options =
                [
                    new("-target:", "Targets", "string", "The targets to build, such as <c>Build</c> or <c>Pack</c>.", Joined: true, List: true),
                    new("-restore", "Restore", "bool", "Runs the <c>Restore</c> target before building the other targets."),
                    new("-restoreProperty:", "RestoreProperties", "ImmutableDictionary<string, string>", "MSBuild properties used only during the restore, each one passed as <c>-restoreProperty:Name=Value</c>.", Joined: true),
                    new("-verbosity:", "Verbosity", "Verbosity?", "The MSBuild verbosity level.", Joined: true),
                    new("-maxCpuCount:", "MaxCpuCount", "int?", "The maximum number of concurrent build processes.", Joined: true),
                    new("-nodeReuse:true", "NodeReuse", "bool?", "Whether MSBuild nodes stay alive after the build to be reused.", FalseToken: "-nodeReuse:false"),
                    new("-graphBuild", "GraphBuild", "bool", "Builds the project graph first, then schedules the builds from it."),
                    new("-isolateProjects", "IsolateProjects", "bool", "Builds each project in isolation."),
                    new("-binaryLogger:", "BinaryLog", "PathInfo", "Writes a binary log (<c>.binlog</c>) of the build to this file.", Joined: true),
                    new("-terminalLogger:", "TerminalLogger", "string", "Whether to use the terminal logger: <c>auto</c>, <c>on</c> or <c>off</c>.", Joined: true),
                    new("-warnAsError", "WarnAsError", "bool", "Treats all warnings as errors."),
                    new("-warnAsMessage:", "WarnAsMessage", "string", "Warning codes to treat as low-importance messages.", Joined: true, List: true),
                    new("-getProperty:", "GetProperties", "string", "Properties whose values are written to the output instead of building.", Joined: true, List: true),
                    new("-getItem:", "GetItems", "string", "Items whose values are written to the output instead of building.", Joined: true, List: true),
                    new("-getTargetResult:", "GetTargetResults", "string", "Targets whose results are written to the output.", Joined: true, List: true),
                    new("-detailedSummary", "DetailedSummary", "bool", "Shows detailed information about the build at the end."),
                    new("-interactive", "Interactive", "bool", "Lets the build wait for user input, for example to authenticate."),
                    new("-noAutoResponse", "NoAutoResponse", "bool", "Does not include any <c>MSBuild.rsp</c> file automatically."),
                    new("-noLogo", "NoLogo", "bool", "Does not display the startup banner and the copyright message."),
                ],
            },
        ]),
        new("New", "New", "Runs <c>dotnet new</c> commands: creates projects and files from templates, and manages templates.",
        [
            new("new_create", "Create", ["new", "create"])
            {
                Desc = "Creates a project or a file from a template.",
                Args =
                [
                    new("Template", Required: true, Desc: "The short name of the template, such as <c>console</c> or <c>classlib</c>."),
                    new("TemplateArguments", Variadic: true, Desc: "The template-specific options, such as <c>[\"--framework\", \"net11.0\"]</c>."),
                ],
            },
            new("new_install", "Install", ["new", "install"])
            {
                Args = [new("Packages", Variadic: true, Required: true, Desc: "The NuGet packages or folders of the templates to install, such as <c>Contoso.Templates@1.0.0</c>.")],
                Lists = ["--add-source"],
            },
            new("new_uninstall", "Uninstall", ["new", "uninstall"])
            {
                Args = [new("Packages", Variadic: true, Desc: "The template packages to uninstall. When empty, lists the installed template packages.")],
            },
            new("new_update", "Update", ["new", "update"]) { Lists = ["--add-source"] },
            new("new_search", "Search", ["new", "search"])
            {
                Args = [new("TemplateName", Desc: "The name of the templates to search for.")],
            },
            new("new_list", "List", ["new", "list"])
            {
                Args = [new("TemplateName", Desc: "The name of the templates to list.")],
            },
            new("new_details", "Details", ["new", "details"])
            {
                Args = [new("PackageIdentifier", Required: true, Desc: "The identifier of the template package.")],
                Lists = ["--add-source"],
            },
        ]),
        new("NuGet", "NuGet", "Runs <c>dotnet nuget</c> commands: pushes, signs and verifies packages, and manages NuGet sources, certificates and configuration.",
        [
            new("nuget_push", "Push", ["nuget", "push"])
            {
                Args = [new("PackagePaths", "PathInfo", Variadic: true, Required: true, Desc: "The packages to push. Wildcards are supported, such as <c>artifacts/*.nupkg</c>.")],
            },
            new("nuget_delete", "Delete", ["nuget", "delete"])
            {
                Args =
                [
                    new("PackageId", Required: true, Desc: "The identifier of the package to delete."),
                    new("PackageVersion", Required: true, Desc: "The version of the package to delete."),
                ],
            },
            new("nuget_locals", "Locals", ["nuget", "locals"])
            {
                Args = [new("CacheLocation", Desc: "The cache to list or clear: <c>all</c>, <c>http-cache</c>, <c>global-packages</c>, <c>temp</c> or <c>plugins-cache</c>.")],
            },
            new("nuget_verify", "Verify", ["nuget", "verify"])
            {
                Args = [new("PackagePaths", "PathInfo", Variadic: true, Required: true, Desc: "The packages to verify.")],
            },
            new("nuget_sign", "Sign", ["nuget", "sign"])
            {
                Args = [new("PackagePaths", "PathInfo", Variadic: true, Required: true, Desc: "The packages to sign.")],
            },
            new("nuget_why", "Why", ["nuget", "why"])
            {
                Args =
                [
                    new("Project", "PathInfo", Desc: "The project or solution to analyze. When unset, dotnet looks for one in the working directory."),
                    new("Package", Required: true, Desc: "The package to explain the dependency on."),
                ],
            },
            new("nuget_add_source", "AddSource", ["nuget", "add", "source"])
            {
                Args = [new("Source", Required: true, Desc: "The URL or folder of the package source.")],
            },
            new("nuget_add_client-cert", "AddClientCert", ["nuget", "add", "client-cert"]),
            new("nuget_update_source", "UpdateSource", ["nuget", "update", "source"])
            {
                Args = [new("Name", Required: true, Desc: "The name of the package source.")],
            },
            new("nuget_update_client-cert", "UpdateClientCert", ["nuget", "update", "client-cert"]),
            new("nuget_remove_source", "RemoveSource", ["nuget", "remove", "source"])
            {
                Args = [new("Name", Required: true, Desc: "The name of the package source.")],
            },
            new("nuget_remove_client-cert", "RemoveClientCert", ["nuget", "remove", "client-cert"]),
            new("nuget_enable_source", "EnableSource", ["nuget", "enable", "source"])
            {
                Args = [new("Name", Required: true, Desc: "The name of the package source.")],
            },
            new("nuget_disable_source", "DisableSource", ["nuget", "disable", "source"])
            {
                Args = [new("Name", Required: true, Desc: "The name of the package source.")],
            },
            new("nuget_list_source", "ListSource", ["nuget", "list", "source"]),
            new("nuget_list_client-cert", "ListClientCert", ["nuget", "list", "client-cert"]),
            new("nuget_config_get", "ConfigGet", ["nuget", "config", "get"])
            {
                Args = [new("Key", Required: true, Desc: "The configuration key to get, or <c>all</c> for every merged setting.")],
            },
            new("nuget_config_set", "ConfigSet", ["nuget", "config", "set"])
            {
                Args =
                [
                    new("Key", Required: true, Desc: "The configuration key to set."),
                    new("Value", Required: true, Desc: "The value to set."),
                ],
            },
            new("nuget_config_unset", "ConfigUnset", ["nuget", "config", "unset"])
            {
                Args = [new("Key", Required: true, Desc: "The configuration key to remove.")],
            },
            new("nuget_config_paths", "ConfigPaths", ["nuget", "config", "paths"]),
            new("nuget_trust_list", "TrustList", ["nuget", "trust", "list"]),
            new("nuget_trust_author", "TrustAuthor", ["nuget", "trust", "author"])
            {
                Args =
                [
                    new("Name", Required: true, Desc: "The name of the trusted signer."),
                    new("Package", "PathInfo", Required: true, Desc: "The signed package to trust the author signature of."),
                ],
            },
            new("nuget_trust_repository", "TrustRepository", ["nuget", "trust", "repository"])
            {
                Args =
                [
                    new("Name", Required: true, Desc: "The name of the trusted signer."),
                    new("Package", "PathInfo", Required: true, Desc: "The signed package to trust the repository signature of."),
                ],
            },
            new("nuget_trust_certificate", "TrustCertificate", ["nuget", "trust", "certificate"])
            {
                Args =
                [
                    new("Name", Required: true, Desc: "The name of the trusted signer."),
                    new("Fingerprint", Required: true, Desc: "The fingerprint of the certificate."),
                ],
            },
            new("nuget_trust_source", "TrustSource", ["nuget", "trust", "source"])
            {
                Args = [new("Name", Required: true, Desc: "The name of the trusted source.")],
            },
            new("nuget_trust_sync", "TrustSync", ["nuget", "trust", "sync"])
            {
                Args = [new("Name", Required: true, Desc: "The name of the trusted signer.")],
            },
            new("nuget_trust_remove", "TrustRemove", ["nuget", "trust", "remove"])
            {
                Args = [new("Name", Required: true, Desc: "The name of the trusted signer.")],
            },
        ]),
        new("Pack", null, null,
        [
            new("pack", "Pack", ["pack"]) { MSBuild = true, Args = [Project] },
        ]),
        new("Package", "Package", "Runs <c>dotnet package</c> commands: searches, adds, removes, updates or lists package references.",
        [
            new("package_add", "Add", ["package", "add"])
            {
                Args = [new("PackageId", Required: true, Desc: "The package to add, such as <c>Newtonsoft.Json</c> or <c>Newtonsoft.Json@13.0.3</c>.")],
                Lists = ["--source"],
            },
            new("package_remove", "Remove", ["package", "remove"])
            {
                Args = [new("PackageNames", Variadic: true, Required: true, Desc: "The packages to remove.")],
            },
            new("package_update", "Update", ["package", "update"])
            {
                Args = [new("Packages", Variadic: true, Desc: "The packages to update, optionally with a version such as <c>Newtonsoft.Json@13.0.3</c>. When empty, updates every package.")],
            },
            new("package_list", "List", ["package", "list"]) { Lists = ["--source"] },
            new("package_search", "Search", ["package", "search"])
            {
                Args = [new("SearchTerm", Desc: "The term to search for.")],
            },
            new("package_download", "Download", ["package", "download"])
            {
                Args = [new("Packages", Variadic: true, Required: true, Desc: "The packages to download, optionally with a version such as <c>Newtonsoft.Json@13.0.3</c>.")],
                Lists = ["--source"],
            },
            new("package_stage_push", "StagePush", ["package", "stage", "push"])
            {
                Args = [new("PackagePath", "PathInfo", Required: true, Desc: "The package to stage.")],
            },
        ]),
        new("Publish", null, null,
        [
            new("publish", "Publish", ["publish"]) { MSBuild = true, Args = [Project] },
        ]),
        new("Reference", "Reference", "Runs <c>dotnet reference</c> commands: adds, removes or lists project references.",
        [
            new("reference_add", "Add", ["reference", "add"])
            {
                Args = [new("ProjectPaths", "PathInfo", Variadic: true, Required: true, Desc: "The projects to reference.")],
                Values = new() { ["--project"] = "PathInfo" },
            },
            new("reference_remove", "Remove", ["reference", "remove"])
            {
                Args = [new("ProjectPaths", "PathInfo", Variadic: true, Required: true, Desc: "The project references to remove.")],
                Values = new() { ["--project"] = "PathInfo" },
            },
            new("reference_list", "List", ["reference", "list"]) { Values = new() { ["--project"] = "PathInfo" } },
        ]),
        new("Restore", null, null,
        [
            new("restore", "Restore", ["restore"]) { MSBuild = true, Args = [Project], Lists = ["--source"] },
        ]),
        new("Run", null, null,
        [
            new("run", "Run", ["run"])
            {
                MSBuild = true,
                Args = [new("ApplicationArguments", Variadic: true, Position: "AfterOptions", Separator: "--", Desc: "The arguments passed to the application, after <c>--</c>.")],
            },
        ]),
        new("Sdk", "Sdk", "Runs <c>dotnet sdk</c> commands: manages the .NET SDK installation.",
        [
            new("sdk_check", "Check", ["sdk", "check"]) { Desc = "Lists the installed SDKs and runtimes, and whether updates are available." },
        ]),
        new("Solution", "Solution", "Runs <c>dotnet solution</c> commands: adds, removes or lists the projects of a solution.",
        [
            new("solution_add", "Add", ["solution"])
            {
                Sub = ["add"],
                Args = [Solution, new("ProjectPaths", "PathInfo", Variadic: true, Required: true, Desc: "The projects to add.")],
            },
            new("solution_remove", "Remove", ["solution"])
            {
                Sub = ["remove"],
                Args = [Solution, new("ProjectPaths", "PathInfo", Variadic: true, Required: true, Desc: "The projects to remove.")],
            },
            new("solution_list", "List", ["solution"]) { Sub = ["list"], Args = [Solution] },
            new("solution_migrate", "Migrate", ["solution"])
            {
                Sub = ["migrate"],
                Desc = "Generates a <c>.slnx</c> file from a <c>.sln</c> file.",
                Args = [Solution],
            },
        ]),
        new("Store", null, null,
        [
            new("store", "Store", ["store"]),
        ]),
        new("Test", null, null,
        [
            new("test_mtp", "Test", ["test"])
            {
                MSBuild = true,
                Mtp = true,
                Desc = "Runs the tests of a project or solution with Microsoft.Testing.Platform, the mode set by "
                       + "<c>\"test\": { \"runner\": \"Microsoft.Testing.Platform\" }</c> in <c>global.json</c>. Pass the platform and "
                       + "extension options, such as <c>--report-trx</c>, in <see cref=\"CommandOptions.AdditionalArguments\"/>.",
            },
            new("test", "TestWithVSTest", ["test"])
            {
                MSBuild = true,
                Desc = "Runs the tests of a project or solution with VSTest, the mode used when <c>global.json</c> does not "
                       + "select Microsoft.Testing.Platform.",
                Args =
                [
                    new("Project", "PathInfo", Desc: "The project or solution to test. When unset, dotnet looks for one in the working directory."),
                    new("RunSettingsArguments", Variadic: true, Position: "AfterOptions", Separator: "--", Desc: "RunSettings arguments, after <c>--</c>, such as <c>[\"MSTest.DeploymentEnabled=false\"]</c>."),
                ],
                Lists = ["--logger", "--collect"],
            },
        ]),
        new("Tool", "Tool", "Runs <c>dotnet tool</c> commands: installs, runs or manages .NET tools.",
        [
            new("tool_install", "Install", ["tool", "install"])
            {
                Args = [new("PackageId", Required: true, Desc: "The tool package to install, such as <c>dotnetsay</c> or <c>dotnetsay@2.1.0</c>.")],
                Lists = ["--add-source"],
            },
            new("tool_uninstall", "Uninstall", ["tool", "uninstall"])
            {
                Args = [new("PackageId", Required: true, Desc: "The tool package to uninstall.")],
            },
            new("tool_update", "Update", ["tool", "update"])
            {
                Args = [new("PackageId", Desc: "The tool package to update. When unset, use <c>All</c> to update every tool.")],
                Lists = ["--add-source"],
            },
            new("tool_list", "List", ["tool", "list"])
            {
                Args = [new("PackageId", Desc: "The tool package to show. When unset, lists every tool.")],
            },
            new("tool_run", "Run", ["tool", "run"])
            {
                Args =
                [
                    new("CommandName", Required: true, Desc: "The command of the local tool to run."),
                    new("ToolArguments", Variadic: true, Position: "AfterOptions", Separator: "--", Desc: "The arguments passed to the tool, after <c>--</c>."),
                ],
            },
            new("tool_search", "Search", ["tool", "search"])
            {
                Args = [new("SearchTerm", Required: true, Desc: "The term to search for.")],
            },
            new("tool_restore", "Restore", ["tool", "restore"]) { Lists = ["--add-source"] },
            new("tool_exec", "Execute", ["tool", "execute"])
            {
                Desc = "Runs a tool package without installing it.",
                Args =
                [
                    new("PackageId", Required: true, Desc: "The tool package to run, such as <c>dotnetsay</c> or <c>dotnetsay@2.1.0</c>."),
                    new("CommandArguments", Variadic: true, Position: "AfterOptions", Separator: "--", Desc: "The arguments passed to the tool, after <c>--</c>."),
                ],
                Lists = ["--add-source"],
            },
        ]),
        new("UserJwts", "UserJwts", "Runs <c>dotnet user-jwts</c> commands: manages JSON Web Tokens for development.",
        [
            new("user-jwts_create", "Create", ["user-jwts", "create"])
            {
                Desc = "Creates a JWT.",
                Values = JwtsValues,
                Lists = ["--audience", "--scope", "--role", "--claim"],
            },
            new("user-jwts_list", "List", ["user-jwts", "list"]) { Desc = "Lists the JWTs of the project.", Values = JwtsValues },
            new("user-jwts_print", "Print", ["user-jwts", "print"])
            {
                Desc = "Shows the details of a JWT.",
                Values = JwtsValues,
                Args = [new("Id", Required: true, Desc: "The ID of the JWT to print.")],
            },
            new("user-jwts_remove", "Remove", ["user-jwts", "remove"])
            {
                Desc = "Deletes a JWT.",
                Values = JwtsValues,
                Args = [new("Id", Required: true, Desc: "The ID of the JWT to delete.")],
            },
            new("user-jwts_clear", "Clear", ["user-jwts", "clear"]) { Desc = "Deletes all the JWTs of the project.", Values = JwtsValues },
            new("user-jwts_key", "Key", ["user-jwts", "key"]) { Desc = "Shows or resets the signing key used to issue JWTs.", Values = JwtsValues },
        ]),
        new("UserSecrets", "UserSecrets", "Runs <c>dotnet user-secrets</c> commands: manages development user secrets.",
        [
            new("user-secrets_set", "Set", ["user-secrets", "set"])
            {
                Desc = "Sets a user secret.",
                Values = SecretsValues,
                Args =
                [
                    new("Name", Required: true, Desc: "The name of the secret."),
                    new("Value", Required: true, Desc: "The value of the secret."),
                ],
            },
            new("user-secrets_remove", "Remove", ["user-secrets", "remove"])
            {
                Desc = "Removes a user secret.",
                Values = SecretsValues,
                Args = [new("Name", Required: true, Desc: "The name of the secret.")],
            },
            new("user-secrets_list", "List", ["user-secrets", "list"]) { Desc = "Lists the user secrets.", Values = SecretsValues },
            new("user-secrets_clear", "Clear", ["user-secrets", "clear"]) { Desc = "Deletes all the user secrets.", Values = SecretsValues },
            new("user-secrets_init", "Init", ["user-secrets", "init"]) { Desc = "Adds a user secrets ID to the project.", Values = SecretsValues },
        ]),
        new("VSTest", null, null,
        [
            new(null, "VSTest", ["vstest"])
            {
                Desc = "Runs tests with VSTest directly.",
                Args = [new("TestFiles", "PathInfo", Variadic: true, Desc: "The test assemblies to run.")],
                Options =
                [
                    new("--Settings:", "Settings", "PathInfo", "The <c>.runsettings</c> file to use.", Joined: true),
                    new("--Tests:", "Tests", "string", "Runs only the tests with these names, separated by commas.", Joined: true),
                    new("--TestCaseFilter:", "TestCaseFilter", "string", "Runs only the tests matching this expression.", Joined: true),
                    new("--Framework:", "Framework", "string", "The target .NET framework version of the tests.", Joined: true),
                    new("--Platform:", "Platform", "string", "The target platform architecture: <c>x86</c>, <c>x64</c>, <c>ARM</c> or <c>ARM64</c>.", Joined: true),
                    new("--logger:", "Loggers", "string", "The loggers of the test results, such as <c>trx</c>.", Joined: true, List: true),
                    new("--ResultsDirectory:", "ResultsDirectory", "PathInfo", "The directory of the test results.", Joined: true),
                    new("--TestAdapterPath:", "TestAdapterPath", "PathInfo", "The directory of custom test adapters.", Joined: true),
                    new("--Collect:", "Collectors", "string", "The data collectors, such as <c>Code Coverage</c>.", Joined: true, List: true),
                    new("--Diag:", "Diag", "PathInfo", "Writes diagnostic logs to this file.", Joined: true),
                    new("--Blame", "Blame", "bool", "Runs the tests in blame mode, to find the test that crashes the test host."),
                    new("--Parallel", "Parallel", "bool", "Runs the test assemblies in parallel."),
                    new("--InIsolation", "InIsolation", "bool", "Runs the tests in a process isolated from VSTest."),
                    new("--ListTests", "ListTests", "bool", "Lists the discovered tests instead of running them."),
                ],
            },
        ]),
        new("Watch", null, null,
        [
            new("watch", "Watch", ["watch"])
            {
                Desc = "Runs a dotnet command, by default <c>run</c>, and runs it again when files change.",
                Args = [new("CommandArguments", Variadic: true, Position: "AfterOptions", Separator: "--", Desc: "The dotnet command and its arguments, after <c>--</c>, such as <c>[\"test\"]</c>.")],
            },
        ]),
        new("Workload", "Workload", "Runs <c>dotnet workload</c> commands: installs and manages optional workloads.",
        [
            new("workload_install", "Install", ["workload", "install"])
            {
                Args = [new("WorkloadIds", Variadic: true, Required: true, Desc: "The workloads to install, such as <c>maui</c>.")],
                Lists = ["--source"],
            },
            new("workload_update", "Update", ["workload", "update"]) { Lists = ["--source"] },
            new("workload_list", "List", ["workload", "list"]),
            new("workload_search", "Search", ["workload", "search"])
            {
                Args = [new("SearchString", Desc: "The workloads to search for. When unset, lists every available workload.")],
            },
            new("workload_search_version", "SearchVersion", ["workload", "search"])
            {
                Sub = ["version"],
                Args =
                [
                    new("SearchString", Position: "BeforeSubcommand", Desc: "Only lists the workload versions containing this workload."),
                    new("WorkloadVersions", Variadic: true, Desc: "The workload versions to show the workloads of."),
                ],
            },
            new("workload_uninstall", "Uninstall", ["workload", "uninstall"])
            {
                Args = [new("WorkloadIds", Variadic: true, Required: true, Desc: "The workloads to uninstall.")],
            },
            new("workload_repair", "Repair", ["workload", "repair"]) { Lists = ["--source"] },
            new("workload_restore", "Restore", ["workload", "restore"])
            {
                Args = [new("Project", "PathInfo", Desc: "The project or solution to restore the workloads of. When unset, dotnet looks for one in the working directory.")],
                Lists = ["--source"],
            },
            new("workload_clean", "Clean", ["workload", "clean"]),
            new("workload_config", "Config", ["workload", "config"]),
            new("workload_history", "History", ["workload", "history"]),
        ]),
    ];
}
