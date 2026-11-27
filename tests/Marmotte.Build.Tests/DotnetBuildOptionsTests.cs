using Xunit;
using static Marmotte.Build.Dotnet;

namespace Marmotte.Build.Tests;

public class DotnetBuildOptionsTests
{
    [Fact]
    public void Default_passes_no_argument()
    {
        Assert.Empty(BuildOptions.Default.ToArguments());
    }

    [Fact]
    public void Project_comes_first()
    {
        var options = BuildOptions.Default with { Configuration = "Release", Project = "app.csproj" };

        Assert.Equal(["app.csproj", "--configuration", "Release"], options.ToArguments());
    }

    [Fact]
    public void Value_options_are_passed_with_their_value()
    {
        var options = BuildOptions.Default with
        {
            Framework = "net11.0",
            Runtime = "linux-x64",
            Arch = "arm64",
            Os = "linux",
            VersionSuffix = "beta",
            Output = "out",
            ArtifactsPath = "artifacts",
            Verbosity = Verbosity.Minimal,
        };

        // Options come in the order of `dotnet build --help`.
        Assert.Equal(
            [
                "--framework", "net11.0",
                "--version-suffix", "beta",
                "--verbosity", "minimal",
                "--output", "out",
                "--artifacts-path", "artifacts",
                "--runtime", "linux-x64",
                "--arch", "arm64",
                "--os", "linux",
            ],
            options.ToArguments());
    }

    [Fact]
    public void Flags_are_passed_only_when_enabled()
    {
        var options = BuildOptions.Default with
        {
            UseCurrentRuntime = true,
            NoRestore = true,
            NoIncremental = true,
            NoDependencies = true,
            Interactive = true,
            DisableBuildServers = true,
        };

        Assert.Equal(
            [
                "--use-current-runtime",
                "--no-restore",
                "--interactive",
                "--no-incremental",
                "--no-dependencies",
                "--disable-build-servers",
            ],
            options.ToArguments());
    }

    [Theory]
    [InlineData(true, "--self-contained")]
    [InlineData(false, "--no-self-contained")]
    public void SelfContained_maps_to_its_two_flags(bool selfContained, string expected)
    {
        var options = BuildOptions.Default with { SelfContained = selfContained };

        Assert.Equal([expected], options.ToArguments());
    }

    [Fact]
    public void Properties_are_passed_as_msbuild_properties_sorted_by_name()
    {
        var options = BuildOptions.Default
            .WithProperty("Version", "1.2.3")
            .WithProperty("ContinuousIntegrationBuild", "true");

        Assert.Equal(["-p:ContinuousIntegrationBuild=true", "-p:Version=1.2.3"], options.ToArguments());
    }

    [Fact]
    public void WithProperty_replaces_an_existing_value()
    {
        var options = BuildOptions.Default.WithProperty("Version", "1.0.0").WithProperty("Version", "2.0.0");

        Assert.Equal(["-p:Version=2.0.0"], options.ToArguments());
    }

    [Fact]
    public void Additional_arguments_come_last()
    {
        var options = BuildOptions.Default.WithProperty("Version", "1.0.0") with
        {
            AdditionalArguments = ["-bl"],
            Configuration = "Release",
        };

        Assert.Equal(["--configuration", "Release", "-p:Version=1.0.0", "-bl"], options.ToArguments());
    }

    [Fact]
    public void Default_is_not_modified_by_with()
    {
        _ = BuildOptions.Default.WithProperty("Version", "1.0.0") with { Configuration = "Release" };

        Assert.Empty(BuildOptions.Default.ToArguments());
    }
}
