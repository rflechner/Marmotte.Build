using System.Reflection;
using Xunit;
using static Marmotte.Build.Dotnet;

namespace Marmotte.Build.Tests;

public class DotnetCommandsTests
{
    public static TheoryData<Type> OptionTypes()
        => new(typeof(Dotnet).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(CommandOptions)) && !type.IsAbstract)
            .OrderBy(type => type.FullName, StringComparer.Ordinal));

    [Theory]
    [MemberData(nameof(OptionTypes))]
    public void Default_options_pass_no_argument(Type type)
    {
        var defaults = type.GetProperty("Default", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

        var options = Assert.IsAssignableFrom<CommandOptions>(defaults);
        Assert.Empty(options.ToArguments());
    }

    [Fact]
    public void Every_command_of_the_sdk_has_options()
    {
        Assert.True(OptionTypes().Count >= 90, $"Only {OptionTypes().Count} options types.");
    }

    [Fact]
    public void Solution_is_passed_before_the_subcommand()
    {
        var options = Solution.AddOptions.Default with { Solution = "app.slnx", ProjectPaths = ["src/App.csproj"], InRoot = true };

        Assert.Equal(["app.slnx", "add", "src/App.csproj", "--in-root"], options.ToArguments(["add"]));
    }

    [Fact]
    public void Run_application_arguments_come_after_a_double_dash()
    {
        var options = RunOptions.Default with { Project = "app.csproj", ApplicationArguments = ["--port", "8080"] };

        Assert.Equal(["--project", "app.csproj", "--", "--port", "8080"], options.ToArguments());
    }

    [Fact]
    public void Repeatable_options_are_repeated()
    {
        var options = RestoreOptions.Default with { Sources = ["nuget.org", "local"] };

        Assert.Equal(["--source", "nuget.org", "--source", "local"], options.ToArguments());
    }

    [Fact]
    public void MSBuild_switches_are_joined_to_their_value()
    {
        var options = MSBuildOptions.Default.WithProperty("Configuration", "Release") with
        {
            Targets = ["Restore", "Pack"],
            Verbosity = Verbosity.Minimal,
            NodeReuse = false,
        };

        Assert.Equal(
            ["-target:Restore", "-target:Pack", "-verbosity:minimal", "-nodeReuse:false", "-p:Configuration=Release"],
            options.ToArguments());
    }

    [Fact]
    public void Fsi_script_and_its_arguments_come_after_the_options()
    {
        var options = FsiOptions.Default with { Script = "build.fsx", ScriptArguments = ["release"], Quiet = true };

        Assert.Equal(["--quiet", "build.fsx", "release"], options.ToArguments());
    }

    [Fact]
    public void WithProperty_keeps_the_options_type()
    {
        PackOptions options = PackOptions.Default.WithProperty("Version", "1.2.3");

        Assert.Equal(["-p:Version=1.2.3"], options.ToArguments());
    }
}
