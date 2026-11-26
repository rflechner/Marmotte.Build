using Xunit;

namespace Marmotte.Build.Tests;

public class PathInfoTests
{
    [Fact]
    public void Slash_joins_segments_with_platform_separator()
    {
        PathInfo result = new PathInfo("src") / new PathInfo("a.cs");

        Assert.Equal(Path.Combine("src", "a.cs"), result.Value);
    }

    [Fact]
    public void Slash_does_not_duplicate_separator_when_left_ends_with_one()
    {
        string sep = Path.DirectorySeparatorChar.ToString();

        PathInfo result = new PathInfo("src" + sep) / new PathInfo("a.cs");

        Assert.Equal(Path.Combine("src", "a.cs"), result.Value);
    }

    [Fact]
    public void Slash_absolute_right_hand_side_wins()
    {
        string absolute = Path.GetTempPath();

        PathInfo result = new PathInfo("relative") / new PathInfo(absolute);

        Assert.Equal(new PathInfo(absolute), result);
    }

    [Fact]
    public void Slash_is_chainable()
    {
        PathInfo result = new PathInfo("a") / new PathInfo("b") / new PathInfo("c");

        Assert.Equal(Path.Combine("a", "b", "c"), result.Value);
    }

    [Fact]
    public void Slash_produces_equal_records_for_equal_paths()
    {
        Assert.Equal(new PathInfo(Path.Combine("a", "b")), new PathInfo("a") / new PathInfo("b"));
    }

    [Fact]
    public void Implicit_conversion_to_string_returns_value()
    {
        string value = new PathInfo("hello");

        Assert.Equal("hello", value);
    }

    [Fact]
    public void Implicit_conversion_from_string()
    {
        PathInfo path = "hello";

        Assert.Equal("hello", path.Value);
    }

    [Fact]
    public void ToString_returns_value()
    {
        Assert.Equal("hello", new PathInfo("hello").ToString());
    }

    [Theory]
    [InlineData("a.cs")]
    [InlineData("a/b/c")]
    public void IsRelative_true_and_IsAbsolute_false_for_relative_paths(string value)
    {
        Assert.True(new PathInfo(value).IsRelative);
        Assert.False(new PathInfo(value).IsAbsolute);
    }

    [Fact]
    public void IsRelative_false_and_IsAbsolute_true_for_absolute_path()
    {
        PathInfo path = new(Path.GetTempPath());

        Assert.False(path.IsRelative);
        Assert.True(path.IsAbsolute);
    }

    [Fact]
    public void IsDirectory_true_and_IsFile_false_for_existing_directory()
    {
        PathInfo path = new(Path.GetTempPath());

        Assert.True(path.IsDirectory);
        Assert.False(path.IsFile);
    }

    [Fact]
    public void IsFile_true_and_IsDirectory_false_for_existing_file()
    {
        string file = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".tmp");
        File.WriteAllText(file, "test");
        try
        {
            PathInfo path = new(file);

            Assert.True(path.IsFile);
            Assert.False(path.IsDirectory);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void IsFile_and_IsDirectory_false_for_missing_path()
    {
        PathInfo path = new(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        Assert.False(path.IsFile);
        Assert.False(path.IsDirectory);
    }

    [Fact]
    public void Trailing_separator_is_trimmed()
    {
        string sep = Path.DirectorySeparatorChar.ToString();

        Assert.Equal("src", new PathInfo("src" + sep).Value);
    }

    [Fact]
    public void Paths_differing_only_by_trailing_separator_are_equal()
    {
        string sep = Path.DirectorySeparatorChar.ToString();

        Assert.Equal(new PathInfo("src"), new PathInfo("src" + sep));
    }

    [Fact]
    public void Root_keeps_its_separator()
    {
        string root = Path.GetPathRoot(Path.GetTempPath())!;

        Assert.Equal(root, new PathInfo(root).Value);
    }

    [Fact]
    public void Alt_separator_is_converted_to_platform_separator()
    {
        string alt = Path.AltDirectorySeparatorChar.ToString();

        Assert.Equal(Path.Combine("a", "b"), new PathInfo("a" + alt + "b").Value);
    }

    [Fact]
    public void Null_value_throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PathInfo(null!));
    }
}
