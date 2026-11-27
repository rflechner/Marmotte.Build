namespace Marmotte.Build.ContinuousIntergration;

/// <summary>Reads GitHub Actions variables from the current process environment.</summary>
/// <remarks>String properties return the exported value, or <see langword="null"/> when absent.</remarks>
public static class GitHubActions
{
    /// <summary>True when the runner sets <c>GITHUB_ACTIONS</c> to true.</summary>
    public static bool IsRunning => bool.TryParse(GetVariable("GITHUB_ACTIONS"), out var running) && running;

    /// <summary>Reads an environment variable by its exact name, such as <c>GITHUB_RUN_NUMBER</c>.</summary>
    public static string? GetVariable(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return Environment.GetEnvironmentVariable(name);
    }

    /// <summary>The CI marker exported to the process. (<c>CI</c>)</summary>
    public static string? CI => GetVariable("CI");

    /// <summary>The current action name or step identifier. (<c>GITHUB_ACTION</c>)</summary>
    public static string? Action => GetVariable("GITHUB_ACTION");

    /// <summary>The directory containing the current composite action. (<c>GITHUB_ACTION_PATH</c>)</summary>
    public static string? ActionPath => GetVariable("GITHUB_ACTION_PATH");

    /// <summary>The owner and repository of the action executing this step. (<c>GITHUB_ACTION_REPOSITORY</c>)</summary>
    public static string? ActionRepository => GetVariable("GITHUB_ACTION_REPOSITORY");

    /// <summary>The account that initiated the original run. (<c>GITHUB_ACTOR</c>)</summary>
    public static string? Actor => GetVariable("GITHUB_ACTOR");

    /// <summary>The account ID of the original actor. (<c>GITHUB_ACTOR_ID</c>)</summary>
    public static string? ActorId => GetVariable("GITHUB_ACTOR_ID");

    /// <summary>The REST API endpoint for this GitHub instance. (<c>GITHUB_API_URL</c>)</summary>
    public static string? ApiUrl => GetVariable("GITHUB_API_URL");

    /// <summary>Path to the current step's artifact declaration file. (<c>GITHUB_ARTIFACTS</c>)</summary>
    public static string? Artifacts => GetVariable("GITHUB_ARTIFACTS");

    /// <summary>Path to the read-only JSON file describing this job's artifacts. (<c>GITHUB_ARTIFACTS_LIST</c>)</summary>
    public static string? ArtifactsList => GetVariable("GITHUB_ARTIFACTS_LIST");

    /// <summary>The target branch of the pull request, when available. (<c>GITHUB_BASE_REF</c>)</summary>
    public static string? BaseRef => GetVariable("GITHUB_BASE_REF");

    /// <summary>Path to the file used to export environment variables to subsequent steps. (<c>GITHUB_ENV</c>)</summary>
    public static string? Env => GetVariable("GITHUB_ENV");

    /// <summary>The event that started the workflow. (<c>GITHUB_EVENT_NAME</c>)</summary>
    public static string? EventName => GetVariable("GITHUB_EVENT_NAME");

    /// <summary>Path to the JSON payload for the triggering event. (<c>GITHUB_EVENT_PATH</c>)</summary>
    public static string? EventPath => GetVariable("GITHUB_EVENT_PATH");

    /// <summary>The GraphQL endpoint for this GitHub instance. (<c>GITHUB_GRAPHQL_URL</c>)</summary>
    public static string? GraphQLUrl => GetVariable("GITHUB_GRAPHQL_URL");

    /// <summary>The source branch of the pull request, when available. (<c>GITHUB_HEAD_REF</c>)</summary>
    public static string? HeadRef => GetVariable("GITHUB_HEAD_REF");

    /// <summary>The identifier of the current job in the workflow. (<c>GITHUB_JOB</c>)</summary>
    public static string? Job => GetVariable("GITHUB_JOB");

    /// <summary>Path to the file used to declare outputs for the current step. (<c>GITHUB_OUTPUT</c>)</summary>
    public static string? Output => GetVariable("GITHUB_OUTPUT");

    /// <summary>Path to the file used to add directories to PATH for subsequent steps. (<c>GITHUB_PATH</c>)</summary>
    public static string? Path => GetVariable("GITHUB_PATH");

    /// <summary>The full Git ref. A pull request build can use a merge ref such as <c>refs/pull/42/merge</c>. (<c>GITHUB_REF</c>)</summary>
    public static string? Ref => GetVariable("GITHUB_REF");

    /// <summary>The short form of the triggering ref. (<c>GITHUB_REF_NAME</c>)</summary>
    public static string? RefName => GetVariable("GITHUB_REF_NAME");

    /// <summary>The runner's flag indicating whether protection rules apply to the ref. (<c>GITHUB_REF_PROTECTED</c>)</summary>
    public static string? RefProtected => GetVariable("GITHUB_REF_PROTECTED");

    /// <summary>The kind of ref, such as <c>branch</c> or <c>tag</c>. (<c>GITHUB_REF_TYPE</c>)</summary>
    public static string? RefType => GetVariable("GITHUB_REF_TYPE");

    /// <summary>The workflow repository in <c>owner/repository</c> format. (<c>GITHUB_REPOSITORY</c>)</summary>
    public static string? Repository => GetVariable("GITHUB_REPOSITORY");

    /// <summary>The ID of the workflow repository. (<c>GITHUB_REPOSITORY_ID</c>)</summary>
    public static string? RepositoryId => GetVariable("GITHUB_REPOSITORY_ID");

    /// <summary>The login of the account that owns the repository. (<c>GITHUB_REPOSITORY_OWNER</c>)</summary>
    public static string? RepositoryOwner => GetVariable("GITHUB_REPOSITORY_OWNER");

    /// <summary>The account ID of the repository owner. (<c>GITHUB_REPOSITORY_OWNER_ID</c>)</summary>
    public static string? RepositoryOwnerId => GetVariable("GITHUB_REPOSITORY_OWNER_ID");

    /// <summary>The retention period for logs and artifacts, in days. (<c>GITHUB_RETENTION_DAYS</c>)</summary>
    public static string? RetentionDays => GetVariable("GITHUB_RETENTION_DAYS");

    /// <summary>The attempt number for this run, starting at one. (<c>GITHUB_RUN_ATTEMPT</c>)</summary>
    public static string? RunAttempt => GetVariable("GITHUB_RUN_ATTEMPT");

    /// <summary>The ID shared by all attempts of this workflow run. (<c>GITHUB_RUN_ID</c>)</summary>
    public static string? RunId => GetVariable("GITHUB_RUN_ID");

    /// <summary>The sequence number of this run within its workflow. (<c>GITHUB_RUN_NUMBER</c>)</summary>
    public static string? RunNumber => GetVariable("GITHUB_RUN_NUMBER");

    /// <summary>The base URL of this GitHub instance. (<c>GITHUB_SERVER_URL</c>)</summary>
    public static string? ServerUrl => GetVariable("GITHUB_SERVER_URL");

    /// <summary>The commit SHA selected for the triggering event. (<c>GITHUB_SHA</c>)</summary>
    public static string? Sha => GetVariable("GITHUB_SHA");

    /// <summary>Path to the file that accepts Markdown for the step summary. (<c>GITHUB_STEP_SUMMARY</c>)</summary>
    public static string? StepSummary => GetVariable("GITHUB_STEP_SUMMARY");

    /// <summary>The account that started this attempt, including a re-run. (<c>GITHUB_TRIGGERING_ACTOR</c>)</summary>
    public static string? TriggeringActor => GetVariable("GITHUB_TRIGGERING_ACTOR");

    /// <summary>The name of the workflow being executed. (<c>GITHUB_WORKFLOW</c>)</summary>
    public static string? Workflow => GetVariable("GITHUB_WORKFLOW");

    /// <summary>The repository, path, and ref identifying the workflow file. (<c>GITHUB_WORKFLOW_REF</c>)</summary>
    public static string? WorkflowRef => GetVariable("GITHUB_WORKFLOW_REF");

    /// <summary>The commit SHA of the workflow file. (<c>GITHUB_WORKFLOW_SHA</c>)</summary>
    public static string? WorkflowSha => GetVariable("GITHUB_WORKFLOW_SHA");

    /// <summary>The runner's workspace directory for this job. (<c>GITHUB_WORKSPACE</c>)</summary>
    public static string? Workspace => GetVariable("GITHUB_WORKSPACE");

    /// <summary>Variables describing the runner assigned to the current job.</summary>
    public static class Runner
    {
        /// <summary>The processor architecture of the runner. (<c>RUNNER_ARCH</c>)</summary>
        public static string? Arch => GetVariable("RUNNER_ARCH");

        /// <summary>The debug logging flag. The runner exports <c>1</c> when enabled. (<c>RUNNER_DEBUG</c>)</summary>
        public static string? Debug => GetVariable("RUNNER_DEBUG");

        /// <summary>The runner hosting environment, such as <c>github-hosted</c> or <c>self-hosted</c>. (<c>RUNNER_ENVIRONMENT</c>)</summary>
        public static string? Environment => GetVariable("RUNNER_ENVIRONMENT");

        /// <summary>The runner name assigned to this job. (<c>RUNNER_NAME</c>)</summary>
        public static string? Name => GetVariable("RUNNER_NAME");

        /// <summary>The operating system of the runner. (<c>RUNNER_OS</c>)</summary>
        public static string? OS => GetVariable("RUNNER_OS");

        /// <summary>The runner's temporary directory for the job. (<c>RUNNER_TEMP</c>)</summary>
        public static string? Temp => GetVariable("RUNNER_TEMP");

        /// <summary>The runner directory containing cached tool installations. (<c>RUNNER_TOOL_CACHE</c>)</summary>
        public static string? ToolCache => GetVariable("RUNNER_TOOL_CACHE");
    }
}
