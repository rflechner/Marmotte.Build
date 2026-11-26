using System.Collections.Immutable;

namespace Marmotte.Build;

/// <summary>
/// Base of the options of the dotnet commands that run MSBuild, such as <c>build</c>, <c>pack</c> or <c>test</c>:
/// they accept MSBuild properties.
/// </summary>
public abstract record MSBuildCommandOptions : CommandOptions
{
    /// <summary>
    /// MSBuild properties, each one passed as <c>-p:Name=Value</c>.
    /// See <see cref="MSBuildCommandOptionsExtensions.WithProperty{TOptions}"/>.
    /// </summary>
    [MSBuildProperties]
    public ImmutableDictionary<string, string> Properties { get; init; } = ImmutableDictionary<string, string>.Empty;
}

/// <summary>Helpers for <see cref="MSBuildCommandOptions"/>.</summary>
public static class MSBuildCommandOptionsExtensions
{
    /// <summary>Returns a copy of <paramref name="options"/> with the MSBuild property <paramref name="name"/> set.</summary>
    public static TOptions WithProperty<TOptions>(this TOptions options, string name, string value)
        where TOptions : MSBuildCommandOptions
        => (TOptions)((MSBuildCommandOptions)options with { Properties = options.Properties.SetItem(name, value) });
}
