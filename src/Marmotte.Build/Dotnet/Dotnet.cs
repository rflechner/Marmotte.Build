namespace Marmotte.Build;

/// <summary>
/// Runs .NET CLI commands (<c>dotnet ...</c>) through <see cref="Shell"/>.
/// </summary>
/// <remarks>
/// <para>
/// Every command has a method that takes its required arguments, a function that customizes its typed
/// options, and an optional working directory (the current directory when omitted). Commands with
/// subcommands are grouped in a nested class, such as <see cref="Tool"/>:
/// </para>
/// <code>
/// Dotnet.Build(o => o with { Configuration = "Release" }, workingDirectory: root / "src");
/// Dotnet.Test(o => o with { NoBuild = true, MaximumFailedTests = 1 });
/// Dotnet.Tool.Install("dotnet-ef", o => o with { Global = true });
/// Dotnet.Execute("--list-sdks");
/// </code>
/// <para>
/// <see cref="Execute"/> runs any other command or global option with raw arguments, and every options record
/// has <see cref="CommandOptions.AdditionalArguments"/> for an argument without a dedicated option.
/// </para>
/// </remarks>
public static partial class Dotnet
{
    /// <summary>MSBuild verbosity level, passed with <c>--verbosity</c>.</summary>
    public enum Verbosity
    {
        Quiet,
        Minimal,
        Normal,
        Detailed,
        Diagnostic,
    }

    /// <summary>
    /// Runs <c>dotnet &lt;command&gt; &lt;arguments&gt;</c>. Use it for a command without a dedicated
    /// method, or for a global option such as <c>--info</c> or <c>--list-sdks</c>.
    /// </summary>
    /// <param name="command">The dotnet command or global option, such as <c>build</c>.</param>
    /// <param name="arguments">The arguments passed after the command.</param>
    /// <param name="workingDirectory">
    /// The directory to run in, or <see langword="null"/> for the current directory.
    /// </param>
    public static Shell.ShellExecuteResult Execute(
        string command,
        IEnumerable<string>? arguments = null,
        PathInfo? workingDirectory = null)
        => Shell.Execute("dotnet", [command, .. arguments ?? []], workingDirectory);

    /// <summary>
    /// Runs <c>dotnet &lt;command&gt; [arguments before the subcommand] &lt;subcommand&gt; &lt;arguments&gt;</c>
    /// with the options customized by <paramref name="configure"/>.
    /// </summary>
    private static Shell.ShellExecuteResult RunCommand<TOptions>(
        IReadOnlyList<string> command,
        IReadOnlyList<string> subcommand,
        TOptions defaults,
        Func<TOptions, TOptions>? configure,
        PathInfo? workingDirectory)
        where TOptions : CommandOptions
    {
        var options = configure is null ? defaults : configure(defaults);
        return Execute(command[0], [.. command.Skip(1), .. options.ToArguments(subcommand)], workingDirectory);
    }
}
