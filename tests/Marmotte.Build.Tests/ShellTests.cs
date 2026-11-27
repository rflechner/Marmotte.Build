using Xunit;
using static Marmotte.Build.Shell;

namespace Marmotte.Build.Tests;

public class ShellTests
{
    [Fact]
    public void Zero_exit_code_is_a_success()
    {
        var result = Execute("dotnet", "--version");

        Assert.Equal(new ShellExecuteSuccess(0), result.Value);
    }

    [Fact]
    public void Non_zero_exit_code_is_a_failure()
    {
        var result = Execute("dotnet", "marmotte-unknown-command");

        Assert.IsType<ShellExecuteFailure>(result.Value);
    }

    [Fact]
    public void Missing_executable_is_not_started()
    {
        var result = Execute("marmotte-missing-executable");

        var notStarted = Assert.IsType<ShellExecuteNotStarted>(result.Value);
        Assert.Equal("marmotte-missing-executable", notStarted.Command);
        Assert.NotEmpty(notStarted.Reason);
    }

    [Fact]
    public void Argument_with_spaces_is_passed_as_a_single_argument()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Relies on the Unix `test` command.");

        var directory = Path.Combine(Path.GetTempPath(), "marmotte dir " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var result = Execute("test", "-d", directory);

            Assert.Equal(new ShellExecuteSuccess(0), result.Value);
        }
        finally
        {
            Directory.Delete(directory);
        }
    }

    [Fact]
    public void Runs_in_the_given_working_directory()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Relies on the Unix `test` command.");

        PathInfo directory = Path.Combine(Path.GetTempPath(), "marmotte-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(directory / "marker.txt", "");
        try
        {
            var result = Execute("test", ["-f", "marker.txt"], directory);

            Assert.Equal(new ShellExecuteSuccess(0), result.Value);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Missing_working_directory_is_not_started()
    {
        PathInfo directory = Path.Combine(Path.GetTempPath(), "marmotte-missing-" + Guid.NewGuid().ToString("N"));

        var result = Execute("dotnet", ["--version"], directory);

        var notStarted = Assert.IsType<ShellExecuteNotStarted>(result.Value);
        Assert.Contains(directory.Value, notStarted.Reason);
    }
}
