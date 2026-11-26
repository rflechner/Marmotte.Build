using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Marmotte.Build;

/// <summary>
/// Base of the typed options of a command. <see cref="ToArguments()"/> turns the public properties marked with
/// <see cref="CommandArgumentAttribute"/>, <see cref="CommandOptionAttribute"/> or
/// <see cref="MSBuildPropertiesAttribute"/> into command-line arguments.
/// </summary>
/// <remarks>
/// Values are formatted by type: <see langword="null"/> passes nothing, a <see cref="bool"/> is a flag,
/// a collection repeats the option once per item, a key/value pair is passed as <c>Key=Value</c>,
/// an enum member is passed in lowercase, and any other value is passed with the invariant culture.
/// </remarks>
public abstract record CommandOptions
{
    /// <summary>Arguments without a dedicated option, passed after the options, such as <c>["-bl"]</c>.</summary>
    public IReadOnlyList<string> AdditionalArguments { get; init; } = [];

    /// <summary>
    /// The command-line arguments, in this order: positional arguments, options in declaration order,
    /// MSBuild properties sorted by name, <see cref="AdditionalArguments"/>, then the arguments placed
    /// <see cref="ArgumentPosition.AfterOptions"/>.
    /// </summary>
    public IReadOnlyList<string> ToArguments() => ToArguments([]);

    /// <summary>
    /// The command-line arguments of a subcommand: the arguments placed
    /// <see cref="ArgumentPosition.BeforeSubcommand"/>, the <paramref name="subcommand"/> tokens,
    /// then the same arguments as <see cref="ToArguments()"/>.
    /// </summary>
    /// <param name="subcommand">The subcommand tokens, such as <c>["add"]</c> for <c>dotnet solution [&lt;SLN&gt;] add</c>.</param>
    public IReadOnlyList<string> ToArguments(IReadOnlyList<string> subcommand)
    {
        var properties = GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(property => property.MetadataToken)
            .ToArray();

        var arguments = new List<string>();

        arguments.AddRange(PositionalArguments(properties, ArgumentPosition.BeforeSubcommand));
        arguments.AddRange(subcommand);
        arguments.AddRange(PositionalArguments(properties, ArgumentPosition.BeforeOptions));

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<CommandOptionAttribute>() is { } option)
                arguments.AddRange(OptionArguments(option, property, property.GetValue(this)));
        }

        foreach (var property in properties.Where(property => property.IsDefined(typeof(MSBuildPropertiesAttribute))))
        {
            if (property.GetValue(this) is IEnumerable<KeyValuePair<string, string>> msbuildProperties)
                arguments.AddRange(SortByKey(msbuildProperties).Select(msbuildProperty => "-p:" + Format(msbuildProperty)));
        }

        arguments.AddRange(AdditionalArguments);
        arguments.AddRange(PositionalArguments(properties, ArgumentPosition.AfterOptions));

        return arguments;
    }

    private IEnumerable<string> PositionalArguments(PropertyInfo[] properties, ArgumentPosition position)
    {
        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<CommandArgumentAttribute>() is not { } argument || argument.Position != position)
                continue;

            var values = Values(property.GetValue(this)).ToList();
            if (values.Count == 0)
                continue;

            if (argument.Separator is not null)
                yield return argument.Separator;

            foreach (var value in values)
                yield return value;
        }
    }

    private static IEnumerable<string> OptionArguments(CommandOptionAttribute option, PropertyInfo property, object? value)
    {
        switch (value)
        {
            case null:
                yield break;

            case true:
                yield return option.Name;
                yield break;

            case false:
                // A non-nullable bool is false by default, so only a bool? that is explicitly false passes FalseName.
                if (option.FalseName is not null && property.PropertyType == typeof(bool?))
                    yield return option.FalseName;
                yield break;
        }

        foreach (var item in Values(value))
        {
            if (option.Joined)
            {
                yield return option.Name + item;
            }
            else
            {
                yield return option.Name;
                yield return item;
            }
        }
    }

    private static IEnumerable<string> Values(object? value) => value switch
    {
        null => [],
        string text => [text],
        IEnumerable<KeyValuePair<string, string>> pairs => SortByKey(pairs).Select(pair => Format(pair)),
        IEnumerable items => items.Cast<object?>().OfType<object>().Select(Format),
        _ => [Format(value)],
    };

    private static IEnumerable<KeyValuePair<string, string>> SortByKey(IEnumerable<KeyValuePair<string, string>> pairs)
        => pairs.OrderBy(pair => pair.Key, StringComparer.Ordinal);

    private static string Format(object value) => value switch
    {
        KeyValuePair<string, string> pair => $"{pair.Key}={pair.Value}",
        Enum member => member.ToString().ToLowerInvariant(),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };
}

/// <summary>Where a positional argument goes on the command line.</summary>
public enum ArgumentPosition
{
    /// <summary>After the command (and its subcommand), before the options. The default.</summary>
    BeforeOptions,

    /// <summary>
    /// Between a command and its subcommand, such as the solution in <c>dotnet solution [&lt;SLN&gt;] add</c>.
    /// </summary>
    BeforeSubcommand,

    /// <summary>After every option, such as the application arguments of <c>dotnet run -- ...</c>.</summary>
    AfterOptions,
}

/// <summary>
/// Marks a property of <see cref="CommandOptions"/> as a positional argument.
/// A collection passes one argument per item.
/// </summary>
/// <param name="position">Where the argument goes on the command line.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CommandArgumentAttribute(ArgumentPosition position = ArgumentPosition.BeforeOptions) : Attribute
{
    /// <summary>Where the argument goes on the command line.</summary>
    public ArgumentPosition Position { get; } = position;

    /// <summary>A token passed before the values when there is at least one, such as <c>--</c>.</summary>
    public string? Separator { get; set; }
}

/// <summary>
/// Marks a property of <see cref="CommandOptions"/> as a named option, such as <c>--configuration</c>.
/// </summary>
/// <param name="name">The option passed when the value is set, or when a flag is <see langword="true"/>.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CommandOptionAttribute(string name) : Attribute
{
    /// <summary>The option, such as <c>--configuration</c>.</summary>
    public string Name { get; } = name;

    /// <summary>
    /// For a <see cref="Nullable{T}"/> of <see cref="bool"/>: the option passed when the value is
    /// <see langword="false"/>, such as <c>--no-self-contained</c>.
    /// </summary>
    public string? FalseName { get; set; }

    /// <summary>
    /// Passes the option and its value as a single argument, such as <c>-target:Build</c> for the name
    /// <c>-target:</c>, instead of two arguments.
    /// </summary>
    public bool Joined { get; set; }
}

/// <summary>
/// Marks a dictionary property of <see cref="CommandOptions"/> as MSBuild properties,
/// each one passed as <c>-p:Name=Value</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MSBuildPropertiesAttribute : Attribute;
