using Xunit;

namespace Marmotte.Build.Tests;

/// <summary>
/// Each test gets its own temporary tree:
/// <code>
/// a.cs
/// b.txt
/// src/c.cs
/// src/lib/d.cs
/// src/lib/e.txt
/// </code>
/// </summary>
public sealed class PathInfoGlobTests : IDisposable
{
    private readonly PathInfo _root = Path.Combine(Path.GetTempPath(), "marmotte-" + Guid.NewGuid().ToString("N"));

    public PathInfoGlobTests()
    {
        Directory.CreateDirectory(_root / "src" / "lib");

        foreach (var file in new[] { "a.cs", "b.txt", Rel("src", "c.cs"), Rel("src", "lib", "d.cs"), Rel("src", "lib", "e.txt") })
            File.WriteAllText(_root / file, "");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Theory]
    [InlineData("*.cs", true)]
    [InlineData("a?.cs", true)]
    [InlineData("src/**", true)]
    [InlineData("src/a.cs", false)]
    public void IsGlob_detects_wildcards(string value, bool expected)
    {
        Assert.Equal(expected, new PathInfo(value).IsGlob);
    }

    [Fact]
    public void Directory_returns_its_direct_children()
    {
        Assert.Equal(["a.cs", "b.txt", "src"], Children(_root));
    }

    [Fact]
    public void File_has_no_children()
    {
        Assert.Empty((_root / "a.cs").GetChildren());
    }

    [Fact]
    public void Missing_path_has_no_children()
    {
        Assert.Empty((_root / "missing").GetChildren());
        Assert.Empty((_root / "missing" / "*.cs").GetChildren());
    }

    [Fact]
    public void Star_matches_within_a_segment()
    {
        Assert.Equal(["a.cs"], Children(_root / "*.cs"));
    }

    [Fact]
    public void Question_mark_matches_a_single_character()
    {
        Assert.Equal(["a.cs"], Children(_root / "?.cs"));
        Assert.Empty((_root / "??.cs").GetChildren());
    }

    [Fact]
    public void Star_in_a_middle_segment_only_matches_directories()
    {
        Assert.Equal([Rel("src", "c.cs")], Children(_root / "*" / "*.cs"));
    }

    [Fact]
    public void Literal_segment_after_a_wildcard()
    {
        Assert.Equal([Rel("src", "lib", "d.cs")], Children(_root / "src" / "*" / "d.cs"));
        Assert.Empty((_root / "src" / "*" / "missing.cs").GetChildren());
    }

    [Fact]
    public void Double_star_matches_zero_or_more_directories()
    {
        Assert.Equal(["a.cs", Rel("src", "c.cs"), Rel("src", "lib", "d.cs")], Children(_root / "**" / "*.cs"));
    }

    [Fact]
    public void Double_star_before_a_literal_finds_it_at_any_depth()
    {
        Assert.Equal([Rel("src", "lib")], Children(_root / "**" / "lib"));
    }

    [Fact]
    public void Trailing_double_star_returns_every_descendant()
    {
        Assert.Equal(
            [Rel("src", "c.cs"), Rel("src", "lib"), Rel("src", "lib", "d.cs"), Rel("src", "lib", "e.txt")],
            Children(_root / "src" / "**"));
    }

    [Fact]
    public void Consecutive_double_stars_do_not_duplicate_results()
    {
        Assert.Equal(
            ["a.cs", Rel("src", "c.cs"), Rel("src", "lib", "d.cs")],
            Children(_root / "**" / "**" / "*.cs"));
    }

    [Fact]
    public void Results_keep_the_pattern_prefix()
    {
        Assert.All((_root / "**" / "*.cs").GetChildren(), path => Assert.StartsWith(_root.Value, path.Value));
    }

    /// <summary>Children of <paramref name="path"/>, relative to the test root and sorted.</summary>
    private string[] Children(PathInfo path)
        => path.GetChildren()
            .Select(child => Path.GetRelativePath(_root, child))
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string Rel(params string[] parts) => Path.Combine(parts);
}
