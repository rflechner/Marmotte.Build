using System.Reflection;
using Xunit;

namespace Marmotte.Build.Tests;

public class CommandOptionsTests
{
    private sealed record SampleOptions : CommandOptions
    {
        [CommandArgument]
        public IReadOnlyList<PathInfo> Files { get; init; } = [];

        [CommandOption("--source")]
        public IReadOnlyList<string> Sources { get; init; } = [];

        [CommandOption("--level")]
        public Dotnet.Verbosity? Level { get; init; }

        [CommandOption("--count")]
        public int? Count { get; init; }

        [CommandOption("--enabled", FalseName = "--disabled")]
        public bool? Enabled { get; init; }

        [CommandOption("--force", FalseName = "--no-force")]
        public bool Force { get; init; }

        public string? NotAnOption { get; init; }
    }

    private sealed record PositionsOptions : CommandOptions
    {
        [CommandArgument(ArgumentPosition.AfterOptions, Separator = "--")]
        public IReadOnlyList<string> Forwarded { get; init; } = [];

        [CommandArgument(ArgumentPosition.BeforeSubcommand)]
        public PathInfo? Solution { get; init; }

        [CommandArgument]
        public PathInfo? Project { get; init; }

        [CommandOption("-target:", Joined = true)]
        public IReadOnlyList<string> Targets { get; init; } = [];

        [CommandOption("--environment")]
        public IReadOnlyDictionary<string, string> Environment { get; init; } = new Dictionary<string, string>();
    }

    [Fact]
    public void Arguments_follow_their_position()
    {
        var options = new PositionsOptions
        {
            Forwarded = ["--app-flag"],
            Solution = "app.slnx",
            Project = "app.csproj",
            Targets = ["Build"],
            AdditionalArguments = ["-bl"],
        };

        Assert.Equal(
            ["app.slnx", "add", "app.csproj", "-target:Build", "-bl", "--", "--app-flag"],
            options.ToArguments(["add"]));
    }

    [Fact]
    public void Separator_is_omitted_without_values()
    {
        Assert.Equal(["app.csproj"], new PositionsOptions { Project = "app.csproj" }.ToArguments());
    }

    [Fact]
    public void Joined_option_is_repeated_as_single_arguments()
    {
        var options = new PositionsOptions { Targets = ["Restore", "Build"] };

        Assert.Equal(["-target:Restore", "-target:Build"], options.ToArguments());
    }

    [Fact]
    public void Dictionary_option_passes_sorted_key_value_pairs()
    {
        var options = new PositionsOptions
        {
            Environment = new Dictionary<string, string> { ["B"] = "2", ["A"] = "value with spaces" },
        };

        Assert.Equal(["--environment", "A=value with spaces", "--environment", "B=2"], options.ToArguments());
    }

    [Fact]
    public void Unset_options_pass_no_argument()
    {
        Assert.Empty(new SampleOptions().ToArguments());
    }

    [Fact]
    public void Collection_argument_passes_each_item_first()
    {
        var options = new SampleOptions { Count = 1, Files = ["a.csproj", "b.csproj"] };

        Assert.Equal(["a.csproj", "b.csproj", "--count", "1"], options.ToArguments());
    }

    [Fact]
    public void Collection_option_is_repeated_for_each_item()
    {
        var options = new SampleOptions { Sources = ["nuget.org", "local"] };

        Assert.Equal(["--source", "nuget.org", "--source", "local"], options.ToArguments());
    }

    [Fact]
    public void Enum_member_is_passed_in_lowercase()
    {
        var options = new SampleOptions { Level = Dotnet.Verbosity.Diagnostic };

        Assert.Equal(["--level", "diagnostic"], options.ToArguments());
    }

    [Theory]
    [InlineData(true, "--enabled")]
    [InlineData(false, "--disabled")]
    public void Nullable_bool_passes_its_name_or_its_false_name(bool enabled, string expected)
    {
        Assert.Equal([expected], new SampleOptions { Enabled = enabled }.ToArguments());
    }

    [Fact]
    public void Non_nullable_bool_ignores_its_false_name()
    {
        Assert.Empty(new SampleOptions { Force = false }.ToArguments());
        Assert.Equal(["--force"], new SampleOptions { Force = true }.ToArguments());
    }

    [Fact]
    public void Property_without_attribute_is_ignored()
    {
        Assert.Empty(new SampleOptions { NotAnOption = "value" }.ToArguments());
    }

    [Fact]
    public void Additional_arguments_come_last()
    {
        var options = new SampleOptions { AdditionalArguments = ["-bl"], Files = ["a.csproj"] };

        Assert.Equal(["a.csproj", "-bl"], options.ToArguments());
    }

    [Fact]
    public void Every_option_of_the_library_is_mapped_to_an_argument()
    {
        // A property without attribute is silently ignored by ToArguments: catch forgotten attributes here.
        var unmapped =
            from type in typeof(CommandOptions).Assembly.GetTypes()
            where type.IsSubclassOf(typeof(CommandOptions))
            from property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            where property.Name != nameof(CommandOptions.AdditionalArguments)
            where !property.IsDefined(typeof(CommandArgumentAttribute))
                  && !property.IsDefined(typeof(CommandOptionAttribute))
                  && !property.IsDefined(typeof(MSBuildPropertiesAttribute))
            select $"{type.Name}.{property.Name}";

        Assert.Empty(unmapped);
    }
}
