namespace Marmotte.Build;

/// <summary>
/// A file system path, normalized on construction:
/// alternate separators are converted to the platform separator
/// and the trailing separator is removed (except for a root such as <c>/</c> or <c>C:\</c>).
/// As a result, <c>"src/"</c> and <c>"src"</c> are equal paths.
/// </summary>
public record PathInfo(string Value)
{
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

    private static string Normalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var withPlatformSeparators = value.Replace(
            Path.AltDirectorySeparatorChar,
            Path.DirectorySeparatorChar);

        return Path.TrimEndingDirectorySeparator(withPlatformSeparators);
    }
}
