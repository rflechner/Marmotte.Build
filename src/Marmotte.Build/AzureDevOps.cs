namespace Marmotte.Build;

/// <summary>Reads Azure Pipelines variables from the current process environment.</summary>
/// <remarks>Variable properties return the original string, or <see langword="null"/> when unavailable.</remarks>
public static class AzureDevOps
{
    /// <summary>True when the agent sets <c>TF_BUILD</c> to true.</summary>
    public static bool IsRunning => bool.TryParse(GetVariable("TF_BUILD"), out var running) && running;

    /// <summary>Reads an exported pipeline variable by name, for example <c>Build.BuildId</c>.</summary>
    public static string? GetVariable(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var environmentName = name.Replace('.', '_').Replace(' ', '_').ToUpperInvariant();
        return global::System.Environment.GetEnvironmentVariable(environmentName);
    }

    /// <summary>Information about the agent executing the job.</summary>
    public static class Agent
    {
        public static string? BuildDirectory => GetVariable("Agent.BuildDirectory");
        public static string? ContainerMapping => GetVariable("Agent.ContainerMapping");
        public static string? DeploymentGroupId => GetVariable("Agent.DeploymentGroupId");
        public static string? Diagnostic => GetVariable("Agent.Diagnostic");
        public static string? HomeDirectory => GetVariable("Agent.HomeDirectory");
        public static string? Id => GetVariable("Agent.Id");
        public static string? JobName => GetVariable("Agent.JobName");
        public static string? JobStatus => GetVariable("Agent.JobStatus");
        public static string? MachineName => GetVariable("Agent.MachineName");
        public static string? Name => GetVariable("Agent.Name");

        /// <summary>The operating system of the agent host.</summary>
        public static string? OS => GetVariable("Agent.OS");
        public static string? OSArchitecture => GetVariable("Agent.OSArchitecture");
        public static string? ReleaseDirectory => GetVariable("Agent.ReleaseDirectory");
        public static string? RootDirectory => GetVariable("Agent.RootDirectory");
        public static string? TempDirectory => GetVariable("Agent.TempDirectory");
        public static string? ToolsDirectory => GetVariable("Agent.ToolsDirectory");
        public static string? Version => GetVariable("Agent.Version");
        public static string? WorkFolder => GetVariable("Agent.WorkFolder");
    }

    /// <summary>The current build, its source, and its trigger.</summary>
    public static class Build
    {
        public static string? ArtifactStagingDirectory => GetVariable("Build.ArtifactStagingDirectory");
        public static string? BinariesDirectory => GetVariable("Build.BinariesDirectory");

        /// <summary>The ID of this run, as a string.</summary>
        public static string? BuildId => GetVariable("Build.BuildId");

        /// <summary>The display number assigned to this run.</summary>
        public static string? BuildNumber => GetVariable("Build.BuildNumber");
        public static string? BuildUri => GetVariable("Build.BuildUri");
        public static string? ContainerId => GetVariable("Build.ContainerId");
        public static string? DefinitionName => GetVariable("Build.DefinitionName");
        public static string? DefinitionVersion => GetVariable("Build.DefinitionVersion");
        public static string? QueuedBy => GetVariable("Build.QueuedBy");
        public static string? QueuedById => GetVariable("Build.QueuedById");

        /// <summary>The trigger for this run, such as <c>PullRequest</c> or <c>Manual</c>.</summary>
        public static string? Reason => GetVariable("Build.Reason");
        public static string? RequestedFor => GetVariable("Build.RequestedFor");
        public static string? RequestedForEmail => GetVariable("Build.RequestedForEmail");
        public static string? RequestedForId => GetVariable("Build.RequestedForId");

        /// <summary>The triggering ref, such as <c>refs/heads/main</c> or a pull request merge ref.</summary>
        public static string? SourceBranch => GetVariable("Build.SourceBranch");

        /// <summary>The final segment of the triggering ref.</summary>
        public static string? SourceBranchName => GetVariable("Build.SourceBranchName");

        /// <summary>The directory the agent uses for checked-out sources.</summary>
        public static string? SourcesDirectory => GetVariable("Build.SourcesDirectory");
        public static string? SourceTfvcShelveset => GetVariable("Build.SourceTfvcShelveset");

        /// <summary>The commit or changeset selected for this run.</summary>
        public static string? SourceVersion => GetVariable("Build.SourceVersion");

        /// <summary>The message for the selected commit, when available after checkout.</summary>
        public static string? SourceVersionMessage => GetVariable("Build.SourceVersionMessage");
        public static string? StageRequestedBy => GetVariable("Build.StageRequestedBy");
        public static string? StageRequestedForId => GetVariable("Build.StageRequestedForId");
        public static string? StagingDirectory => GetVariable("Build.StagingDirectory");

        /// <summary>The YAML schedule that triggered the build.</summary>
        public static class CronSchedule
        {
            public static string? DisplayName => GetVariable("Build.CronSchedule.DisplayName");
        }

        /// <summary>The repository that triggered the build.</summary>
        public static class Repository
        {
            public static string? Clean => GetVariable("Build.Repository.Clean");
            public static string? Id => GetVariable("Build.Repository.Id");

            /// <summary>The path of the self repository on the agent.</summary>
            public static string? LocalPath => GetVariable("Build.Repository.LocalPath");
            public static string? Name => GetVariable("Build.Repository.Name");
            public static string? Provider => GetVariable("Build.Repository.Provider");
            public static string? Uri => GetVariable("Build.Repository.Uri");

            /// <summary>Git checkout settings.</summary>
            public static class Git
            {
                public static string? SubmoduleCheckout => GetVariable("Build.Repository.Git.SubmoduleCheckout");
            }

            /// <summary>Team Foundation Version Control checkout settings.</summary>
            public static class Tfvc
            {
                public static string? Workspace => GetVariable("Build.Repository.Tfvc.Workspace");
            }
        }

        /// <summary>The upstream build for a classic build completion trigger.</summary>
        public static class TriggeredBy
        {
            /// <summary>The ID of the upstream build.</summary>
            public static string? BuildId => GetVariable("Build.TriggeredBy.BuildId");
            public static string? BuildNumber => GetVariable("Build.TriggeredBy.BuildNumber");
            public static string? DefinitionId => GetVariable("Build.TriggeredBy.DefinitionId");
            public static string? DefinitionName => GetVariable("Build.TriggeredBy.DefinitionName");
            public static string? ProjectId => GetVariable("Build.TriggeredBy.ProjectId");
        }
    }

    /// <summary>Values available while a pipeline check runs.</summary>
    public static class Checks
    {
        public static string? StageAttempt => GetVariable("Checks.StageAttempt");
    }

    /// <summary>Locations shared by build tasks.</summary>
    public static class Common
    {
        public static string? TestResultsDirectory => GetVariable("Common.TestResultsDirectory");
    }

    /// <summary>The Azure Pipelines environment targeted by a deployment job.</summary>
    public static class Environment
    {
        public static string? Id => GetVariable("Environment.Id");
        public static string? Name => GetVariable("Environment.Name");
        public static string? ResourceId => GetVariable("Environment.ResourceId");
        public static string? ResourceName => GetVariable("Environment.ResourceName");
    }

    /// <summary>Locations associated with the pipeline run.</summary>
    public static class Pipeline
    {
        /// <summary>The pipeline workspace on the agent, also exposed as <c>Agent.BuildDirectory</c>.</summary>
        public static string? Workspace => GetVariable("Pipeline.Workspace");
    }

    /// <summary>Information about the current classic release.</summary>
    public static class Release
    {
        public static string? AttemptNumber => GetVariable("Release.AttemptNumber");
        public static string? DefinitionEnvironmentId => GetVariable("Release.DefinitionEnvironmentId");
        public static string? DefinitionId => GetVariable("Release.DefinitionId");
        public static string? DefinitionName => GetVariable("Release.DefinitionName");
        public static string? DeploymentId => GetVariable("Release.DeploymentId");
        public static string? DeployPhaseId => GetVariable("Release.DeployPhaseId");
        public static string? EnvironmentId => GetVariable("Release.EnvironmentId");
        public static string? EnvironmentName => GetVariable("Release.EnvironmentName");
        public static string? EnvironmentUri => GetVariable("Release.EnvironmentUri");
        public static string? PrimaryArtifactSourceAlias => GetVariable("Release.PrimaryArtifactSourceAlias");
        public static string? Reason => GetVariable("Release.Reason");
        public static string? ReleaseDescription => GetVariable("Release.ReleaseDescription");
        public static string? ReleaseId => GetVariable("Release.ReleaseId");
        public static string? ReleaseName => GetVariable("Release.ReleaseName");
        public static string? ReleaseUri => GetVariable("Release.ReleaseUri");
        public static string? ReleaseWebUrl => GetVariable("Release.ReleaseWebUrl");
        public static string? RequestedFor => GetVariable("Release.RequestedFor");
        public static string? RequestedForEmail => GetVariable("Release.RequestedForEmail");
        public static string? RequestedForId => GetVariable("Release.RequestedForId");
        public static string? SkipArtifactsDownload => GetVariable("Release.SkipArtifactsDownload");

        /// <summary>The identity that requested the classic deployment.</summary>
        public static class Deployment
        {
            public static string? RequestedFor => GetVariable("Release.Deployment.RequestedFor");
            public static string? RequestedForEmail => GetVariable("Release.Deployment.RequestedForEmail");
            public static string? RequestedForId => GetVariable("Release.Deployment.RequestedForId");
        }

        /// <summary>The artifact that triggered the classic release.</summary>
        public static class TriggeringArtifact
        {
            public static string? Alias => GetVariable("Release.TriggeringArtifact.Alias");
        }
    }

    /// <summary>The resource that triggered the YAML pipeline.</summary>
    public static class Resources
    {
        public static string? TriggeringAlias => GetVariable("Resources.TriggeringAlias");
        public static string? TriggeringCategory => GetVariable("Resources.TriggeringCategory");
    }

    /// <summary>The strategy and current phase of a deployment job.</summary>
    public static class Strategy
    {
        public static string? CycleName => GetVariable("Strategy.CycleName");
        public static string? Name => GetVariable("Strategy.Name");
    }

    /// <summary>The project, job, stage, and pull request for this run.</summary>
    public static class System
    {
        /// <summary>The job's OAuth token, when explicitly exposed to the script.</summary>
        public static string? AccessToken => GetVariable("System.AccessToken");
        public static string? ArtifactsDirectory => GetVariable("System.ArtifactsDirectory");
        public static string? CollectionId => GetVariable("System.CollectionId");
        public static string? CollectionUri => GetVariable("System.CollectionUri");
        public static string? Debug => GetVariable("System.Debug");
        public static string? DefaultWorkingDirectory => GetVariable("System.DefaultWorkingDirectory");
        public static string? DefinitionId => GetVariable("System.DefinitionId");
        public static string? HostType => GetVariable("System.HostType");
        public static string? JobAttempt => GetVariable("System.JobAttempt");
        public static string? JobDisplayName => GetVariable("System.JobDisplayName");
        public static string? JobId => GetVariable("System.JobId");
        public static string? JobName => GetVariable("System.JobName");
        public static string? OidcRequestUri => GetVariable("System.OidcRequestUri");
        public static string? PhaseAttempt => GetVariable("System.PhaseAttempt");
        public static string? PhaseDisplayName => GetVariable("System.PhaseDisplayName");
        public static string? PhaseName => GetVariable("System.PhaseName");
        public static string? PlanId => GetVariable("System.PlanId");
        public static string? StageAttempt => GetVariable("System.StageAttempt");
        public static string? StageDisplayName => GetVariable("System.StageDisplayName");
        public static string? StageName => GetVariable("System.StageName");
        public static string? TeamFoundationCollectionUri => GetVariable("System.TeamFoundationCollectionUri");
        public static string? TeamProject => GetVariable("System.TeamProject");
        public static string? TeamProjectId => GetVariable("System.TeamProjectId");
        public static string? TimelineId => GetVariable("System.TimelineId");
        public static string? WorkFolder => GetVariable("System.WorkFolder");

        /// <summary>Pull request metadata available to the validation build.</summary>
        public static class PullRequest
        {
            public static string? IsFork => GetVariable("System.PullRequest.IsFork");

            /// <summary>The pull request identifier supplied by the repository provider.</summary>
            public static string? PullRequestId => GetVariable("System.PullRequest.PullRequestId");

            /// <summary>The GitHub pull request number when it differs from the provider's ID.</summary>
            public static string? PullRequestNumber => GetVariable("System.PullRequest.PullRequestNumber");

            /// <summary>The source branch of the pull request being validated.</summary>
            public static string? SourceBranch => GetVariable("System.PullRequest.SourceBranch");
            public static string? SourceCommitId => GetVariable("System.PullRequest.SourceCommitId");
            public static string? SourceRepositoryUri => GetVariable("System.PullRequest.SourceRepositoryUri");

            /// <summary>The pull request destination, in the format supplied by the provider.</summary>
            public static string? TargetBranch => GetVariable("System.PullRequest.TargetBranch");
            public static string? TargetBranchName => GetVariable("System.PullRequest.TargetBranchName");
        }
    }
}
