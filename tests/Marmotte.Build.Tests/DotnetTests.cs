using Xunit;
using static Marmotte.Build.Shell;

namespace Marmotte.Build.Tests;

public sealed class DotnetTests : IDisposable
{
    private readonly PathInfo _directory = Path.Combine(Path.GetTempPath(), "marmotte-" + Guid.NewGuid().ToString("N"));

    public DotnetTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void Execute_runs_a_global_option()
    {
        Assert.Equal(new ShellExecuteSuccess(0), Dotnet.Execute("--version").Value);
    }

    [Fact]
    public void Command_runs_in_the_working_directory()
    {
        var result = Dotnet.New.Create("globaljson", workingDirectory: _directory);

        Assert.Equal(new ShellExecuteSuccess(0), result.Value);
        Assert.True((_directory / "global.json").IsFile);
    }

    [Fact]
    public void Failing_command_is_a_failure()
    {
        // No project or solution in an empty directory.
        var result = Dotnet.Build(workingDirectory: _directory);

        Assert.IsType<ShellExecuteFailure>(result.Value);
    }

    [Fact]
    public void Missing_working_directory_is_not_started()
    {
        var result = Dotnet.Build(workingDirectory: _directory / "missing");

        Assert.IsType<ShellExecuteNotStarted>(result.Value);
    }
}
