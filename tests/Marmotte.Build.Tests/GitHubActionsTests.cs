using Marmotte.Build.ContinuousIntegration;
using Xunit;

namespace Marmotte.Build.Tests;

[CollectionDefinition(DisableParallelization = true)]
public sealed class GitHubActionsEnvironmentCollection { }

[Collection<GitHubActionsEnvironmentCollection>]
public class GitHubActionsTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("false", false)]
    [InlineData("true", true)]
    [InlineData("True", true)]
    public void IsRunning_uses_the_GitHub_marker_even_when_CI_is_true(string? marker, bool expected)
    {
        WithVariable("CI", "true", () =>
            WithVariable("GITHUB_ACTIONS", marker, () => Assert.Equal(expected, GitHubActions.IsRunning)));
    }

    [Fact]
    public void RunNumber_preserves_the_exported_value_and_reads_it_on_each_access()
    {
        WithVariable("GITHUB_RUN_NUMBER", "0042", () =>
        {
            Assert.Equal("0042", GitHubActions.RunNumber);
            Environment.SetEnvironmentVariable("GITHUB_RUN_NUMBER", "0043");
            Assert.Equal("0043", GitHubActions.RunNumber);
        });
    }

    [Fact]
    public void Pull_request_branches_can_be_absent_or_contain_slashes()
    {
        WithVariable("GITHUB_HEAD_REF", null, () => Assert.Null(GitHubActions.HeadRef));
        WithVariable("GITHUB_HEAD_REF", "feature/build", () => Assert.Equal("feature/build", GitHubActions.HeadRef));
        WithVariable("GITHUB_BASE_REF", "release/1.x", () => Assert.Equal("release/1.x", GitHubActions.BaseRef));
    }

    [Fact]
    public void Runner_and_workflow_files_expose_the_exported_values()
    {
        WithVariable("RUNNER_DEBUG", "1", () => Assert.Equal("1", GitHubActions.Runner.Debug));
        WithVariable("RUNNER_OS", "macOS", () => Assert.Equal("macOS", GitHubActions.Runner.OS));
        WithVariable("GITHUB_OUTPUT", "/step/output file", () => Assert.Equal("/step/output file", GitHubActions.Output));
    }

    [Fact]
    public void GetVariable_accepts_the_exact_name_exported_by_a_workflow()
    {
        WithVariable("Marmotte_CustomFlag", "yes", () => Assert.Equal("yes", GitHubActions.GetVariable("Marmotte_CustomFlag")));
    }

    private static void WithVariable(string name, string? value, Action assertion)
    {
        var previous = Environment.GetEnvironmentVariable(name);
        try
        {
            Environment.SetEnvironmentVariable(name, value);
            assertion();
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, previous);
        }
    }
}
