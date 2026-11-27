using Marmotte.Build.ContinuousIntegration;
using Xunit;

namespace Marmotte.Build.Tests;

[CollectionDefinition(DisableParallelization = true)]
public sealed class ProcessEnvironmentCollection { }

[Collection<ProcessEnvironmentCollection>]
public class AzureDevOpsTests
{
    [Fact]
    public void Build_properties_read_the_current_value_without_parsing_or_caching()
    {
        WithVariable("BUILD_BUILDNUMBER", "00042", () =>
        {
            Assert.Equal("00042", AzureDevOps.Build.BuildNumber);
            Environment.SetEnvironmentVariable("BUILD_BUILDNUMBER", "1.2.0-preview");
            Assert.Equal("1.2.0-preview", AzureDevOps.Build.BuildNumber);
        });
    }

    [Fact]
    public void Nested_properties_read_the_agents_exported_names()
    {
        WithVariable("BUILD_REPOSITORY_ID", "repo-id",
            () => Assert.Equal("repo-id", AzureDevOps.Build.Repository.Id));
        WithVariable("SYSTEM_PULLREQUEST_TARGETBRANCH", "refs/heads/main",
            () => Assert.Equal("refs/heads/main", AzureDevOps.System.PullRequest.TargetBranch));
        WithVariable("AGENT_OS", "Linux",
            () => Assert.Equal("Linux", AzureDevOps.Agent.OS));
        WithVariable("PIPELINE_WORKSPACE", "/agent/work/1",
            () => Assert.Equal("/agent/work/1", AzureDevOps.Pipeline.Workspace));
        WithVariable("RELEASE_DEPLOYMENT_REQUESTEDFOR", "Jane Doe",
            () => Assert.Equal("Jane Doe", AzureDevOps.Release.Deployment.RequestedFor));
    }

    [Fact]
    public void Pull_request_variables_can_be_absent()
    {
        WithVariable("SYSTEM_PULLREQUEST_PULLREQUESTID", null,
            () => Assert.Null(AzureDevOps.System.PullRequest.PullRequestId));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("False", false)]
    [InlineData("True", true)]
    [InlineData("true", true)]
    public void IsRunning_reads_the_agent_marker(string? marker, bool expected)
    {
        WithVariable("TF_BUILD", marker, () => Assert.Equal(expected, AzureDevOps.IsRunning));
    }

    [Fact]
    public void GetVariable_accepts_pipeline_resource_aliases()
    {
        WithVariable("RESOURCES_PIPELINE_UPSTREAM-CI_RUNID", "0123",
            () => Assert.Equal("0123", AzureDevOps.GetVariable("resources.pipeline.upstream-ci.runID")));
    }

    private static void WithVariable(string name, string? value, Action assert)
    {
        var previous = Environment.GetEnvironmentVariable(name);
        try
        {
            Environment.SetEnvironmentVariable(name, value);
            assert();
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, previous);
        }
    }
}
