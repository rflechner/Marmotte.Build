namespace Marmotte.Build;

/// <summary>
/// A file system path, normalized on construction:
/// alternate separators are converted to the platform separator
/// and the trailing separator is removed (except for a root such as <c>/</c> or <c>C:\</c>).
/// As a result, <c>"src/"</c> and <c>"src"</c> are equal paths.
/// A path can also be a glob pattern, see <see cref="GetChildren"/>.
/// </summary>
public record PathInfo(string Value)
{
    private static readonly EnumerationOptions MatchOptions = new()
    {
        MatchType = MatchType.Simple,
        MatchCasing = MatchCasing.PlatformDefault,
        IgnoreInaccessible = true,
        AttributesToSkip = 0,
    };

    private static readonly EnumerationOptions DescendantOptions = new()
    {
        MatchType = MatchType.Simple,
        IgnoreInaccessible = true,
        AttributesToSkip = 0,
        RecurseSubdirectories = true,
    };

    private static readonly EnumerationOptions SubdirectoryOptions = new()
    {
        MatchType = MatchType.Simple,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint,
    };

    public string Value { get; } = Normalize(Value);

    /// <summary>
    /// Combines two paths using <see cref="Path.Combine(string, string)"/>.
    /// If <paramref name="right"/> is absolute, it replaces <paramref name="left"/>:
    /// <c>new PathInfo("src") / "/etc"</c> returns <c>/etc</c>.
    /// </summary>
    public static PathInfo operator /(PathInfo left, PathInfo right)
        => new(Path.Combine(left.Value, right.Value));

    public static implicit operator string(PathInfo path) => path.Value;

    public static implicit operator PathInfo(string path) => new PathInfo(path);

    public override string ToString() => Value;

    public bool IsRelative => !Path.IsPathRooted(Value);

    public bool IsAbsolute => !IsRelative;

    /// <summary>True if a directory exists at this path (hits the file system).</summary>
    public bool IsDirectory => Directory.Exists(Value);

    /// <summary>True if a file exists at this path (hits the file system).</summary>
    public bool IsFile => File.Exists(Value);

    /// <summary>True if this path is a glob pattern, i.e. it contains <c>*</c> or <c>?</c>.</summary>
    public bool IsGlob => HasWildcard(Value);

    public static PathInfo CurrentDirectory => new(Environment.CurrentDirectory);

    /// <summary>
    /// Lazily enumerates file system entries (hits the file system):
    /// every file and directory matching the pattern if this path is a glob,
    /// the direct children if it is a directory, and nothing otherwise.
    /// </summary>
    /// <remarks>
    /// Glob syntax, per path segment: <c>*</c> matches any characters, <c>?</c> matches exactly one,
    /// and a <c>**</c> segment matches zero or more directories. As the last segment,
    /// <c>**</c> matches every descendant, so <c>src / "**"</c> lists the whole tree under <c>src</c>.
    /// Matching follows the platform's case sensitivity, includes hidden entries,
    /// and <c>**</c> does not follow directory links.
    /// Results are relative when this path is relative, and come in no particular order.
    /// </remarks>
    public IEnumerable<PathInfo> GetChildren()
    {
        if (IsGlob)
            return Glob();

        return IsDirectory ? (this / "*").Glob() : [];
    }

    private IEnumerable<PathInfo> Glob()
    {
        var root = Path.GetPathRoot(Value) ?? string.Empty;
        if (root.Length > 0 && !Directory.Exists(root))
            return [];

        var segments = Value[root.Length..].Split(
            Path.DirectorySeparatorChar,
            StringSplitOptions.RemoveEmptyEntries);

        // Several "**" segments can reach the same entry through different paths.
        return Match(root, segments, 0).Distinct().Select(path => new PathInfo(path));
    }

    private static IEnumerable<string> Match(string current, string[] segments, int index)
    {
        if (index == segments.Length)
        {
            yield return current;
            yield break;
        }

        var segment = segments[index];
        var isLast = index == segments.Length - 1;
        var directory = current.Length == 0 ? "." : current;

        if (segment == "**")
        {
            if (isLast)
            {
                foreach (var entry in Directory.EnumerateFileSystemEntries(directory, "*", DescendantOptions))
                    yield return Join(current, Path.GetRelativePath(directory, entry));

                yield break;
            }

            // Zero directories: the rest of the pattern applies here.
            foreach (var match in Match(current, segments, index + 1))
                yield return match;

            // One or more directories: "**" stays active in each subdirectory.
            foreach (var subdirectory in Directory.EnumerateDirectories(directory, "*", SubdirectoryOptions))
            foreach (var match in Match(Join(current, Path.GetFileName(subdirectory)), segments, index))
                yield return match;

            yield break;
        }

        if (!HasWildcard(segment))
        {
            var next = Join(current, segment);
            var exists = isLast ? Path.Exists(next) : Directory.Exists(next);
            if (!exists)
                yield break;

            foreach (var match in Match(next, segments, index + 1))
                yield return match;

            yield break;
        }

        var entries = isLast
            ? Directory.EnumerateFileSystemEntries(directory, segment, MatchOptions)
            : Directory.EnumerateDirectories(directory, segment, MatchOptions);

        foreach (var entry in entries)
        foreach (var match in Match(Join(current, Path.GetFileName(entry)), segments, index + 1))
            yield return match;
    }

    private static string Join(string directory, string name)
        => directory.Length == 0 ? name : Path.Combine(directory, name);

    private static bool HasWildcard(string value) => value.AsSpan().IndexOfAny('*', '?') >= 0;

    private static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var withPlatformSeparators = value.Replace(
            Path.AltDirectorySeparatorChar,
            Path.DirectorySeparatorChar);

        return Path.TrimEndingDirectorySeparator(withPlatformSeparators);
    }
}