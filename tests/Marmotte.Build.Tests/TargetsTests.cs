using Xunit;
using static Marmotte.Build.Shell;
using static Marmotte.Build.Targets;

namespace Marmotte.Build.Tests;

// Targets are kept in a static registry: each test starts from an empty one.
// Tests of a class never run in parallel, and no other test class declares targets.
public sealed class TargetsTests : IDisposable
{
    private readonly List<string> _runs = [];

    public TargetsTests() => Targets.Clear();

    public void Dispose() => Targets.Clear();

    private void Recording(string name, IReadOnlyList<string>? dependsOn = null, Func<bool>? when = null)
        => Target(name, dependsOn ?? [], () => _runs.Add(name), when);

    [Fact]
    public void Dependencies_run_first_in_the_given_order()
    {
        Recording("Clean");
        Recording("Restore");
        Recording("Build", ["Clean", "Restore"]);

        Assert.Equal(0, RunTarget("Build"));
        Assert.Equal(["Clean", "Restore", "Build"], _runs);
    }

    [Fact]
    public void Dependencies_can_be_declared_later_and_named_in_any_case()
    {
        Recording("Build", ["restore"]);
        Recording("Restore");

        Assert.Equal(0, RunTarget("build"));
        Assert.Equal(["Restore", "Build"], _runs);
    }

    [Fact]
    public void Shared_dependency_runs_once()
    {
        Recording("Restore");
        Recording("Build", ["Restore"]);
        Recording("Test", ["Restore", "Build"]);

        Assert.Equal(0, RunTarget("Test"));
        Assert.Equal(["Restore", "Build", "Test"], _runs);
    }

    [Fact]
    public void False_condition_skips_the_target_but_not_its_dependencies()
    {
        Recording("Pack");
        Recording("Publish", ["Pack"], when: () => false);

        Assert.Equal(0, RunTarget("Publish"));
        Assert.Equal(["Pack"], _runs);
    }

    [Fact]
    public void Condition_is_evaluated_after_the_dependencies()
    {
        Recording("Pack");
        Recording("Publish", ["Pack"], when: () => _runs.Contains("Pack"));

        Assert.Equal(0, RunTarget("Publish"));
        Assert.Equal(["Pack", "Publish"], _runs);
    }

    [Fact]
    public void Exception_fails_the_build_and_stops_later_targets()
    {
        Target("Build", () => throw new InvalidOperationException("Broken build."));
        Recording("Test", ["Build"]);

        Assert.Equal(1, RunTarget("Test"));
        Assert.Empty(_runs);
    }

    [Fact]
    public void Failed_shell_command_fails_the_build()
    {
        Target("Build", () => new ShellExecuteFailure(3));

        Assert.Equal(1, RunTarget("Build"));
    }

    [Fact]
    public void Successful_shell_command_succeeds()
    {
        Target("Build", () => new ShellExecuteSuccess(0));

        Assert.Equal(0, RunTarget("Build"));
    }

    [Fact]
    public void Target_option_selects_a_target_case_insensitively()
    {
        Recording("Build");
        Recording("Test", ["Build"]);
        Recording("Pack", ["Test"]);

        Assert.Equal(0, RunTarget(["--configuration", "Debug", "--target", "TEST"], "Pack"));
        Assert.Equal(["Build", "Test"], _runs);
    }

    [Fact]
    public void Default_target_runs_without_target_option()
    {
        Recording("Build");

        Assert.Equal(0, RunTarget(["--publish"], "Build"));
        Assert.Equal(["Build"], _runs);
    }

    [Fact]
    public void Unknown_target_fails_without_running_anything()
    {
        Recording("Build");

        Assert.Equal(1, RunTarget(["--target", "Missing"], "Build"));
        Assert.Empty(_runs);
    }

    [Fact]
    public void Target_option_without_name_fails()
    {
        Recording("Build");

        Assert.Equal(1, RunTarget(["--target"], "Build"));
        Assert.Empty(_runs);
    }

    [Fact]
    public void Duplicate_name_fails_without_running_anything()
    {
        Recording("Build");
        Recording("BUILD");

        Assert.Equal(1, RunTarget("Build"));
        Assert.Empty(_runs);
    }

    [Fact]
    public void Unknown_dependency_in_any_target_fails_without_running_anything()
    {
        Recording("Build");
        Recording("Publish", ["Pakc"]);

        Assert.Equal(1, RunTarget("Build"));
        Assert.Empty(_runs);
    }

    [Fact]
    public void Dependency_cycle_fails_without_running_anything()
    {
        Recording("Restore");
        Recording("Build", ["Restore", "Test"]);
        Recording("Test", ["Build"]);

        Assert.Equal(1, RunTarget("Restore"));
        Assert.Empty(_runs);
    }

    [Fact]
    public void Plan_option_runs_nothing()
    {
        Recording("Clean");
        Recording("Restore");
        Recording("Build", ["Clean", "Restore"]);
        Recording("Publish", ["Build"], when: () => true);

        Assert.Equal(0, RunTarget(["--plan"], "Publish"));
        Assert.Equal(0, RunTarget(["--target", "Build", "--plan"], "Publish"));
        Assert.Empty(_runs);
    }

    [Fact]
    public void Plan_option_reports_invalid_targets()
    {
        Recording("Build", ["Missing"]);

        Assert.Equal(1, RunTarget(["--plan"], "Build"));
    }

    [Fact]
    public void Plan_option_with_unknown_target_fails()
    {
        Recording("Build");

        Assert.Equal(1, RunTarget(["--plan", "--target", "Missing"], "Build"));
    }

    [Fact]
    public void Self_dependency_is_a_cycle()
    {
        Recording("Build", ["Build"]);

        Assert.Equal(1, RunTarget("Build"));
        Assert.Empty(_runs);
    }
}
