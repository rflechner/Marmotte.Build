using System.ComponentModel;
using System.Diagnostics;

namespace Marmotte.Build;

/// <summary>
/// Provides methods for executing shell commands.
/// </summary>
public static class Shell
{
    /// <summary>
    /// Represents the result of a shell command that completed successfully.
    /// </summary>
    /// <param name="ExitCode">The exit code returned by the completed process.</param>
    public record ShellExecuteSuccess(int ExitCode);

    /// <summary>
    /// Represents the result of a shell command that completed unsuccessfully.
    /// </summary>
    /// <param name="ExitCode">The non-zero exit code returned by the completed process.</param>
    public record ShellExecuteFailure(int ExitCode);

    /// <summary>
    /// Represents a shell command whose process could not be started,
    /// for example because the executable was not found or is not executable.
    /// </summary>
    /// <param name="Command">The executable name or path that could not be started.</param>
    /// <param name="Reason">The error reported by the operating system.</param>
    public record ShellExecuteNotStarted(string Command, string Reason);

    /// <summary>
    /// Represents the result of a shell command execution: completed successfully,
    /// completed unsuccessfully, or not started at all.
    /// </summary>
    public union ShellExecuteResult(ShellExecuteSuccess, ShellExecuteFailure, ShellExecuteNotStarted);

    /// <summary>
    /// Executes the specified command with the provided arguments and waits for the process to exit.
    /// </summary>
    /// <param name="command">The executable name or path to run.</param>
    /// <param name="arguments">
    /// The command-line arguments to pass to the executable. Each one reaches the process as a single
    /// argument, even if it contains spaces or quotes.
    /// </param>
    /// <returns>
    /// A result indicating whether the process completed with a zero exit code, completed with a
    /// non-zero exit code, or could not be started.
    /// </returns>
    public static ShellExecuteResult Execute(string command, params string[] arguments)
        => Execute(command, arguments, workingDirectory: null);

    /// <summary>
    /// Executes the specified command with the provided arguments in the given working directory,
    /// and waits for the process to exit.
    /// </summary>
    /// <param name="command">The executable name or path to run.</param>
    /// <param name="arguments">
    /// The command-line arguments to pass to the executable. Each one reaches the process as a single
    /// argument, even if it contains spaces or quotes.
    /// </param>
    /// <param name="workingDirectory">
    /// The directory to run the process in, or <see langword="null"/> for the current directory.
    /// </param>
    /// <returns>
    /// A result indicating whether the process completed with a zero exit code, completed with a
    /// non-zero exit code, or could not be started (including when the working directory does not exist).
    /// </returns>
    public static ShellExecuteResult Execute(string command, IEnumerable<string> arguments, PathInfo? workingDirectory)
    {
        if (workingDirectory is not null && !workingDirectory.IsDirectory)
            return new ShellExecuteNotStarted(command, $"Working directory not found: {workingDirectory}");

        using var process = new Process();
        process.StartInfo.FileName = command;

        if (workingDirectory is not null)
            process.StartInfo.WorkingDirectory = workingDirectory;

        foreach (var argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        try
        {
            process.Start();
        }
        catch (Win32Exception e)
        {
            return new ShellExecuteNotStarted(command, e.Message);
        }

        process.WaitForExit();

        return process.ExitCode == 0
            ? new ShellExecuteSuccess(process.ExitCode)
            : new ShellExecuteFailure(process.ExitCode);
    }
}
