using LibGit2Sharp;
using Marmotte.Build.VersionControl;
using Xunit;

namespace Marmotte.Build.Tests;

/// <summary>Each test gets its own temporary repository, with a local identity so it ignores the global configuration.</summary>
public sealed class GitTests : IDisposable
{
    private static readonly GitIdentity Tester = new("Marmotte Tester", "tester@example.com");

    private readonly PathInfo _root = Path.Combine(Path.GetTempPath(), "marmotte-git-" + Guid.NewGuid().ToString("N"));
    private readonly Git _git;

    public GitTests()
    {
        Repository.Init(_root);
        _git = Git.Open(_root);
        _git.Repository.Config.Set("user.name", Tester.Name, ConfigurationLevel.Local);
        _git.Repository.Config.Set("user.email", Tester.Email, ConfigurationLevel.Local);
    }

    public void Dispose()
    {
        _git.Dispose();
        DeleteDirectory(_root);
    }

    [Fact]
    public void Open_discovers_the_repository_from_a_subdirectory()
    {
        Directory.CreateDirectory(_root / "src" / "lib");
        WriteFile("marker.txt");

        using var git = Git.Open(_root / "src" / "lib");

        Assert.True((git.RootDirectory / "marker.txt").IsFile);
    }

    [Fact]
    public void Open_fails_outside_a_repository()
    {
        PathInfo directory = Path.Combine(Path.GetTempPath(), "marmotte-no-git-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            Assert.Throws<InvalidOperationException>(() => Git.Open(directory));
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    [Fact]
    public void Head_fails_before_the_first_commit()
    {
        Assert.Throws<InvalidOperationException>(() => _git.Head);
        Assert.Empty(_git.TagsAtHead);
        Assert.Null(_git.LatestTag);
    }

    [Fact]
    public void Commit_records_staged_changes()
    {
        WriteFile("README.md", "hello");
        _git.Stage("README.md");

        var commit = _git.Commit("docs: add readme\n\nWith a body.");

        Assert.Equal(commit, _git.Head);
        Assert.Equal("docs: add readme", commit.MessageShort);
        Assert.Equal(commit.Sha[..7], commit.ShortSha);
        Assert.Equal(Tester.Name, commit.AuthorName);
        Assert.Equal(Tester.Email, commit.AuthorEmail);
        Assert.False(_git.IsDirty);
    }

    [Fact]
    public void Commit_uses_the_given_author()
    {
        WriteFile("a.txt");
        _git.StageAll();

        var commit = _git.Commit("chore: add a", new GitIdentity("Build Bot", "bot@example.com"));

        Assert.Equal("Build Bot", commit.AuthorName);
        Assert.Equal("bot@example.com", commit.AuthorEmail);
    }

    [Fact]
    public void Commit_fails_when_nothing_is_staged()
    {
        CommitFile("a.txt");

        Assert.Throws<EmptyCommitException>(() => _git.Commit("chore: nothing"));
    }

    [Fact]
    public void Stage_accepts_absolute_paths_and_patterns()
    {
        Directory.CreateDirectory(_root / "src");
        WriteFile("src/a.cs");
        WriteFile("src/b.cs");
        WriteFile("notes.txt");

        _git.Stage(_git.RootDirectory / "notes.txt", "src/*.cs");
        _git.Commit("chore: add files");

        Assert.False(_git.IsDirty);
    }

    [Fact]
    public void StageAll_includes_deleted_files()
    {
        CommitFile("a.txt");
        File.Delete(_root / "a.txt");

        _git.StageAll();
        _git.Commit("chore: remove a");

        Assert.False(_git.IsDirty);
        Assert.Null(_git.Repository.Head.Tip["a.txt"]);
    }

    [Fact]
    public void Stage_requires_a_path()
    {
        Assert.Throws<ArgumentException>(() => _git.Stage());
    }

    [Fact]
    public void IsDirty_detects_untracked_and_modified_files_but_not_ignored_ones()
    {
        WriteFile(".gitignore", "*.log");
        _git.Stage(".gitignore");
        CommitFile("a.txt");
        Assert.False(_git.IsDirty);

        WriteFile("build.log");
        Assert.False(_git.IsDirty);

        WriteFile("untracked.txt");
        Assert.True(_git.IsDirty);

        File.Delete(_root / "untracked.txt");
        WriteFile("a.txt", "changed");
        Assert.True(_git.IsDirty);
    }

    [Fact]
    public void Branch_is_the_checked_out_branch_or_null_when_detached()
    {
        var first = CommitFile("a.txt");
        Commands.Checkout(_git.Repository, _git.Repository.CreateBranch("feature/git"));

        Assert.Equal("feature/git", _git.Branch);

        Commands.Checkout(_git.Repository, first.Sha);

        Assert.Null(_git.Branch);
        Assert.Equal(first, _git.Head);
    }

    [Fact]
    public void CreateTag_creates_lightweight_and_annotated_tags()
    {
        var commit = CommitFile("a.txt");

        var lightweight = _git.CreateTag("v1.0.0");
        var annotated = _git.CreateTag("release-1", message: "Release 1");

        Assert.Equal(new GitTag("v1.0.0", commit.Sha, IsAnnotated: false, Message: null), lightweight);
        Assert.Equal("release-1", annotated.Name);
        Assert.Equal(commit.Sha, annotated.Sha);
        Assert.True(annotated.IsAnnotated);
        Assert.Equal("Release 1", annotated.Message?.TrimEnd());
        Assert.Equal(Tester.Name, _git.Repository.Tags["release-1"].Annotation.Tagger.Name);
        Assert.Equal(["release-1", "v1.0.0"], _git.Tags.Select(tag => tag.Name));
    }

    [Fact]
    public void CreateTag_fails_when_the_tag_exists()
    {
        CommitFile("a.txt");
        _git.CreateTag("v1.0.0");

        Assert.Throws<NameConflictException>(() => _git.CreateTag("v1.0.0"));
    }

    [Fact]
    public void TagsAtHead_only_returns_tags_on_head()
    {
        CommitFile("a.txt");
        _git.CreateTag("v1.0.0");
        CommitFile("b.txt");
        _git.CreateTag("v1.1.0", message: "Release 1.1.0");
        _git.CreateTag("latest");

        Assert.Equal(["latest", "v1.1.0"], _git.TagsAtHead.Select(tag => tag.Name));
    }

    [Fact]
    public void LatestTag_is_the_closest_reachable_tag()
    {
        CommitFile("a.txt");
        _git.CreateTag("v1.0.0");
        CommitFile("b.txt");
        _git.CreateTag("v1.1.0-rc.1");
        _git.CreateTag("v1.1.0");
        var main = _git.Repository.Head;

        Commands.Checkout(_git.Repository, _git.Repository.CreateBranch("other"));
        CommitFile("c.txt");
        _git.CreateTag("v9.0.0");
        Commands.Checkout(_git.Repository, main);
        CommitFile("d.txt");

        Assert.Equal("v1.1.0", _git.LatestTag?.Name);
    }

    [Fact]
    public void LatestTag_is_null_without_tags()
    {
        CommitFile("a.txt");

        Assert.Null(_git.LatestTag);
    }

    [Fact]
    public void Describe_follows_git_describe_tags()
    {
        var first = CommitFile("a.txt");
        Assert.Equal(first.ShortSha, _git.Describe());

        _git.CreateTag("v1.0.0");
        Assert.Equal("v1.0.0", _git.Describe());

        var second = CommitFile("b.txt");
        Assert.Equal($"v1.0.0-1-g{second.ShortSha}", _git.Describe());
    }

    private GitCommit CommitFile(string relativePath)
    {
        WriteFile(relativePath, Guid.NewGuid().ToString());
        _git.Stage(relativePath);
        return _git.Commit($"chore: update {relativePath}");
    }

    private void WriteFile(string relativePath, string content = "") => File.WriteAllText(_root / relativePath, content);

    /// <summary>Git marks object files read-only, which prevents deleting them on Windows.</summary>
    private static void DeleteDirectory(PathInfo directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);

        Directory.Delete(directory, recursive: true);
    }
}
