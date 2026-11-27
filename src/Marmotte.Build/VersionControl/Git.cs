using LibGit2Sharp;

namespace Marmotte.Build.VersionControl;

/// <summary>A snapshot of a Git commit.</summary>
/// <param name="Sha">The full commit identifier.</param>
/// <param name="ShortSha">The first seven characters of <paramref name="Sha"/>.</param>
/// <param name="Message">The full commit message, as stored by Git.</param>
/// <param name="MessageShort">The first line of the commit message.</param>
/// <param name="AuthorName">The author's name.</param>
/// <param name="AuthorEmail">The author's email address.</param>
/// <param name="AuthorDate">When the commit was authored.</param>
public sealed record GitCommit(
    string Sha,
    string ShortSha,
    string Message,
    string MessageShort,
    string AuthorName,
    string AuthorEmail,
    DateTimeOffset AuthorDate);

/// <summary>A snapshot of a Git tag.</summary>
/// <param name="Name">The short tag name, such as <c>v1.2.3</c>.</param>
/// <param name="Sha">The identifier of the commit the tag points to, even for an annotated tag.</param>
/// <param name="IsAnnotated">True for an annotated tag, false for a lightweight tag.</param>
/// <param name="Message">The annotation message, or <see langword="null"/> for a lightweight tag.</param>
public sealed record GitTag(string Name, string Sha, bool IsAnnotated, string? Message);

/// <summary>The name and email address recorded as a commit author or a tagger.</summary>
public sealed record GitIdentity(string Name, string Email);

/// <summary>
/// Reads and updates a Git repository with <see href="https://github.com/libgit2/libgit2sharp">LibGit2Sharp</see>,
/// without requiring the <c>git</c> executable.
/// </summary>
/// <remarks>
/// <para>
/// Open the repository once and dispose it at the end of the script. Properties read the repository each time,
/// so they reflect commits and tags created in the meantime:
/// </para>
/// <code>
/// using var git = Git.Open(root);
///
/// Console.WriteLine($"{git.Branch ?? "detached HEAD"} at {git.Head.ShortSha}");
///
/// git.Stage("CHANGELOG.md");
/// git.Commit("chore: update changelog");
/// git.CreateTag("v1.2.3", message: "Release 1.2.3");
/// </code>
/// <para>
/// Pushing and fetching are not wrapped: they need credentials, and LibGit2Sharp does not support SSH remotes.
/// Run <c>git push</c> with <see cref="Shell.Execute(string, string[])"/> instead, or use <see cref="Repository"/>
/// for anything else.
/// </para>
/// </remarks>
public sealed class Git : IDisposable
{
    private Git(Repository repository) => Repository = repository;

    /// <summary>The underlying LibGit2Sharp repository, for operations this helper does not cover.</summary>
    public Repository Repository { get; }

    /// <summary>
    /// Opens the Git repository containing <paramref name="path"/>, searching its parent directories like
    /// <c>git</c> does. Dispose the result to release the repository.
    /// </summary>
    /// <param name="path">A directory inside the repository, or <see langword="null"/> for the current directory.</param>
    /// <exception cref="InvalidOperationException">No repository was found, or the repository is bare.</exception>
    public static Git Open(PathInfo? path = null)
    {
        var start = path ?? PathInfo.CurrentDirectory;
        var gitDirectory = LibGit2Sharp.Repository.Discover(start)
            ?? throw new InvalidOperationException($"No Git repository found in {start} or its parent directories.");

        var repository = new Repository(gitDirectory);
        if (repository.Info.IsBare)
        {
            repository.Dispose();
            throw new InvalidOperationException($"{gitDirectory} is a bare repository; a working directory is required.");
        }

        return new Git(repository);
    }
    
    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<Git> _currentRepository = new Lazy<Git>(() => Open(PathInfo.CurrentDirectory));
    
    /// <summary>
    /// The current Git repository.
    /// </summary>
    public static Git CurrentRepository => _currentRepository.Value;

    /// <summary>The root of the working directory.</summary>
    public PathInfo RootDirectory => Repository.Info.WorkingDirectory;

    /// <summary>
    /// The name of the checked-out branch, such as <c>main</c>, or <see langword="null"/> when HEAD is detached.
    /// </summary>
    /// <remarks>
    /// CI systems often check out a detached commit for tags and pull requests. Read the branch from the CI
    /// variables in that case, such as <c>GitHubActions.HeadRef</c>.
    /// </remarks>
    public string? Branch => Repository.Info.IsHeadDetached ? null : Repository.Head.FriendlyName;

    /// <summary>The commit checked out by HEAD.</summary>
    /// <exception cref="InvalidOperationException">The repository has no commits yet.</exception>
    public GitCommit Head => ToGitCommit(HeadTip);

    /// <summary>
    /// True when the index or the working directory differs from HEAD, including untracked files.
    /// Ignored files do not count.
    /// </summary>
    public bool IsDirty => Repository.RetrieveStatus(new StatusOptions { IncludeIgnored = false }).IsDirty;

    /// <summary>All tags, sorted by name.</summary>
    public IReadOnlyList<GitTag> Tags => Repository.Tags
        .Select(ToGitTag)
        .OrderBy(tag => tag.Name, StringComparer.Ordinal)
        .ToArray();

    /// <summary>The tags pointing to the HEAD commit, sorted by name. Empty when the repository has no commits.</summary>
    public IReadOnlyList<GitTag> TagsAtHead => Repository.Head.Tip is { } tip
        ? Tags.Where(tag => tag.Sha == tip.Sha).ToArray()
        : [];

    /// <summary>
    /// The tag on the closest commit reachable from HEAD, or <see langword="null"/> when there is none.
    /// When several tags point to that commit, the first by name is returned.
    /// </summary>
    /// <remarks>
    /// A shallow clone may not contain the tagged commit. With <c>actions/checkout</c>, set <c>fetch-depth: 0</c>
    /// to fetch the whole history and its tags.
    /// </remarks>
    public GitTag? LatestTag
    {
        get
        {
            var tagsByCommit = Tags.ToLookup(tag => tag.Sha);
            if (tagsByCommit.Count == 0 || Repository.Head.Tip is null)
                return null;

            var history = Repository.Commits.QueryBy(new CommitFilter
            {
                IncludeReachableFrom = Repository.Head,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time,
            });

            return history
                .Select(commit => tagsByCommit[commit.Sha].FirstOrDefault())
                .FirstOrDefault(tag => tag is not null);
        }
    }

    /// <summary>
    /// Describes HEAD like <c>git describe --tags</c>: <c>v1.2.3</c> on a tagged commit, <c>v1.2.3-4-gabc1234</c>
    /// four commits later, or the abbreviated commit identifier when no tag is reachable.
    /// </summary>
    /// <exception cref="InvalidOperationException">The repository has no commits yet.</exception>
    public string Describe() => Repository.Describe(HeadTip, new DescribeOptions
    {
        Strategy = DescribeStrategy.Tags,
        UseCommitIdAsFallback = true,
    });

    /// <summary>
    /// Adds changes to the index, including deleted files, like <c>git add</c>.
    /// </summary>
    /// <param name="paths">
    /// Files or patterns, relative to <see cref="RootDirectory"/> or absolute inside it, such as
    /// <c>"CHANGELOG.md"</c> or <c>"src/*.cs"</c>.
    /// </param>
    public void Stage(params PathInfo[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (paths.Length == 0)
            throw new ArgumentException("Specify at least one path, or call StageAll.", nameof(paths));

        Commands.Stage(Repository, paths.Select(path => path.Value));
    }

    /// <summary>Adds every change in the working directory to the index, like <c>git add --all</c>.</summary>
    public void StageAll() => Commands.Stage(Repository, "*");

    /// <summary>Commits the staged changes and returns the new HEAD commit.</summary>
    /// <param name="message">The commit message.</param>
    /// <param name="author">
    /// The author and committer, or <see langword="null"/> to use <c>user.name</c> and <c>user.email</c>
    /// from the Git configuration.
    /// </param>
    /// <exception cref="InvalidOperationException">No identity is given and none is configured.</exception>
    /// <exception cref="EmptyCommitException">Nothing is staged.</exception>
    public GitCommit Commit(string message, GitIdentity? author = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var signature = CreateSignature(author);
        return ToGitCommit(Repository.Commit(message, signature, signature));
    }

    /// <summary>Creates a tag on HEAD and returns it.</summary>
    /// <param name="name">The tag name, such as <c>v1.2.3</c>.</param>
    /// <param name="message">
    /// The annotation message for an annotated tag, or <see langword="null"/> for a lightweight tag.
    /// </param>
    /// <param name="tagger">
    /// The tagger of an annotated tag, or <see langword="null"/> to use <c>user.name</c> and <c>user.email</c>
    /// from the Git configuration. Ignored for a lightweight tag.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The repository has no commits yet, or an annotated tag has no tagger and none is configured.
    /// </exception>
    /// <exception cref="NameConflictException">A tag with this name already exists.</exception>
    public GitTag CreateTag(string name, string? message = null, GitIdentity? tagger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var tag = message is null
            ? Repository.Tags.Add(name, HeadTip)
            : Repository.Tags.Add(name, HeadTip, CreateSignature(tagger), message);

        return ToGitTag(tag);
    }

    /// <summary>Releases the repository.</summary>
    public void Dispose() => Repository.Dispose();

    private LibGit2Sharp.Commit HeadTip => Repository.Head.Tip
        ?? throw new InvalidOperationException("The repository has no commits yet.");

    private Signature CreateSignature(GitIdentity? identity)
    {
        if (identity is not null)
            return new Signature(identity.Name, identity.Email, DateTimeOffset.Now);

        return Repository.Config.BuildSignature(DateTimeOffset.Now)
            ?? throw new InvalidOperationException(
                "Git user.name and user.email are not configured. Configure them, or pass a GitIdentity.");
    }

    private static GitCommit ToGitCommit(LibGit2Sharp.Commit commit) => new(
        commit.Sha,
        commit.Sha[..7],
        commit.Message,
        commit.MessageShort,
        commit.Author.Name,
        commit.Author.Email,
        commit.Author.When);

    private static GitTag ToGitTag(LibGit2Sharp.Tag tag) => new(
        tag.FriendlyName,
        tag.PeeledTarget.Sha,
        tag.IsAnnotated,
        tag.Annotation?.Message);
}
