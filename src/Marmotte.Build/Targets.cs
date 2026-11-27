using System.Diagnostics;
using Spectre.Console;
using static Marmotte.Build.Shell;

namespace Marmotte.Build;

/// <summary>
/// A minimal target DSL: declare named targets with the names of the targets they depend on,
/// then run one of them with <see cref="RunTarget(string[], string)"/>.
/// </summary>
/// <remarks>
/// Target names are compared case-insensitively. Targets can be declared in any order: dependencies are resolved
/// and checked when a target runs.
/// </remarks>
/// <example>
/// <code>
/// using static Marmotte.Build.Targets;
///
/// Target("Clean", () => Dotnet.Clean());
/// Target("Build", ["Clean"], () => Dotnet.Build());
/// Target("Deploy", ["Build"], () => Console.WriteLine("Deploying"), when: () => args.Contains("--deploy"));
///
/// return RunTarget(args, defaultTarget: "Deploy");
/// </code>
/// </example>
public static class Targets
{
    private sealed record Definition(string Name, string[] DependsOn, Action? Action, Func<bool>? When);

    private static readonly List<Definition> Declared = [];

    /// <summary>Declares a target without dependencies.</summary>
    /// <param name="name">A unique name, compared case-insensitively.</param>
    /// <param name="action">The work to do. Throw an exception to fail the build.</param>
    /// <param name="when">
    /// An optional condition evaluated right before the target would run, after its dependencies.
    /// When it returns <see langword="false"/>, the target is skipped; its dependencies still run.
    /// </param>
    public static void Target(string name, Action action, Func<bool>? when = null)
        => Target(name, [], action, when);

    /// <summary>Declares a target without dependencies that runs a shell command.</summary>
    /// <param name="name">A unique name, compared case-insensitively.</param>
    /// <param name="action">The command to run. A failed or unstarted command fails the build.</param>
    /// <param name="when">
    /// An optional condition evaluated right before the target would run, after its dependencies.
    /// When it returns <see langword="false"/>, the target is skipped; its dependencies still run.
    /// </param>
    public static void Target(string name, Func<ShellExecuteResult> action, Func<bool>? when = null)
        => Target(name, [], action, when);

    /// <summary>Declares a target that runs a shell command after its dependencies.</summary>
    /// <param name="name">A unique name, compared case-insensitively.</param>
    /// <param name="dependsOn">Names of the targets to run first, in this order. Each target runs at most once per build.</param>
    /// <param name="action">The command to run. A failed or unstarted command fails the build.</param>
    /// <param name="when">
    /// An optional condition evaluated right before the target would run, after its dependencies.
    /// When it returns <see langword="false"/>, the target is skipped; its dependencies still run.
    /// </param>
    public static void Target(string name, IReadOnlyList<string> dependsOn, Func<ShellExecuteResult> action, Func<bool>? when = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        Target(name, dependsOn, () => action().ThrowIfNotFailed(), when);
    }

    /// <summary>Declares a target that runs after its dependencies, or only groups them when no action is given.</summary>
    /// <param name="name">A unique name, compared case-insensitively.</param>
    /// <param name="dependsOn">Names of the targets to run first, in this order. Each target runs at most once per build.</param>
    /// <param name="action">The work to do, or <see langword="null"/>. Throw an exception to fail the build.</param>
    /// <param name="when">
    /// An optional condition evaluated right before the target would run, after its dependencies.
    /// When it returns <see langword="false"/>, the target is skipped; its dependencies still run.
    /// </param>
    public static void Target(string name, IReadOnlyList<string> dependsOn, Action? action = null, Func<bool>? when = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(dependsOn);
        if (dependsOn.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Dependency names cannot be empty.", nameof(dependsOn));

        Declared.Add(new Definition(name, [.. dependsOn], action, when));
    }

    /// <summary>
    /// Runs the target named by <c>--target &lt;name&gt;</c> in <paramref name="args"/>, or <paramref name="defaultTarget"/>
    /// when the option is absent, after its dependencies. See <see cref="RunTarget(string)"/>.
    /// With <c>--plan</c>, only checks the targets and prints the dependency tree and execution order, without running anything.
    /// </summary>
    /// <param name="args">The script arguments. Arguments other than <c>--target</c> and <c>--plan</c> are ignored.</param>
    /// <param name="defaultTarget">The name of the target to run when <c>--target</c> is not given.</param>
    /// <returns>0 when every target succeeded or was skipped, or when the plan is valid; otherwise 1.</returns>
    public static int RunTarget(string[] args, string defaultTarget)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultTarget);

        var name = defaultTarget;
        var index = Array.IndexOf(args, "--target");
        if (index >= 0)
        {
            if (index + 1 >= args.Length || args[index + 1].StartsWith('-'))
            {
                AnsiConsole.MarkupLine("[red]--target requires a target name.[/]");
                return 1;
            }

            name = args[index + 1];
        }

        return args.Contains("--plan") ? ShowPlan(name) : RunTarget(name);
    }

    /// <summary>
    /// Checks every declared target, then runs the named target after its dependencies, depth first,
    /// each target at most once. The first exception stops the build.
    /// </summary>
    /// <remarks>
    /// Nothing runs when a name is declared twice, a dependency is not declared, dependencies form a cycle,
    /// or <paramref name="name"/> is unknown.
    /// </remarks>
    /// <param name="name">The name of the target to run, compared case-insensitively.</param>
    /// <returns>0 when every target succeeded or was skipped, otherwise 1.</returns>
    public static int RunTarget(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (Resolve(name) is not { } resolved)
            return 1;

        var plan = GetExecutionOrder(resolved.Target, resolved.Targets);
        AnsiConsole.MarkupLine($"[gray]Targets: {Markup.Escape(string.Join(", ", plan.Select(planned => planned.Name)))}[/]");

        var stopwatch = Stopwatch.StartNew();
        foreach (var current in plan)
        {
            var currentName = Markup.Escape(current.Name);
            try
            {
                if (current.When?.Invoke() == false)
                {
                    AnsiConsole.MarkupLine($"[gray]Skipping target {currentName}: condition not met.[/]");
                    continue;
                }

                AnsiConsole.MarkupLine($"[gold3]Running target {currentName}...[/]");
                current.Action?.Invoke();
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[red]Target {currentName} failed.[/]");
                AnsiConsole.WriteException(e, ExceptionFormats.ShortenEverything);
                return 1;
            }
        }

        AnsiConsole.MarkupLine($"[green]Build succeeded in {stopwatch.Elapsed.TotalSeconds:0.0}s.[/]");
        return 0;
    }

    /// <summary>Forgets every declared target. Lets tests start from an empty set of targets.</summary>
    internal static void Clear() => Declared.Clear();

    /// <summary>Checks the targets, then prints the dependency tree of the named target and its execution order.</summary>
    private static int ShowPlan(string name)
    {
        if (Resolve(name) is not { } resolved)
            return 1;

        var (target, targets) = resolved;
        var tree = new Tree(Label(target));
        AddDependencies(tree, target);
        AnsiConsole.Write(tree);

        var plan = GetExecutionOrder(target, targets);
        AnsiConsole.MarkupLine($"[gray]Execution order: {Markup.Escape(string.Join(", ", plan.Select(planned => planned.Name)))}[/]");
        return 0;

        void AddDependencies(IHasTreeNodes parent, Definition current)
        {
            foreach (var dependency in current.DependsOn)
            {
                var next = targets[dependency];
                AddDependencies(parent.AddNode(Label(next)), next);
            }
        }

        static string Label(Definition target)
            => Markup.Escape(target.Name) + (target.When is null ? "" : " [gray](conditional)[/]");
    }

    /// <summary>Checks every declared target and finds the named one, printing the errors when there are any.</summary>
    private static (Definition Target, Dictionary<string, Definition> Targets)? Resolve(string name)
    {
        var errors = new List<string>();
        var targets = Check(errors);
        if (errors.Count > 0)
        {
            AnsiConsole.MarkupLine("[red]Invalid targets:[/]");
            foreach (var error in errors)
                AnsiConsole.MarkupLine($"[red]  - {Markup.Escape(error)}[/]");
            return null;
        }

        if (!targets.TryGetValue(name, out var target))
        {
            var available = string.Join(", ", targets.Keys);
            AnsiConsole.MarkupLine($"[red]Unknown target {Markup.Escape(name)}. Available targets: {Markup.Escape(available)}.[/]");
            return null;
        }

        return (target, targets);
    }

    private static List<Definition> GetExecutionOrder(Definition target, Dictionary<string, Definition> targets)
    {
        var plan = new List<Definition>();
        AddWithDependencies(target, targets, plan);
        return plan;
    }

    /// <summary>Indexes the declared targets by name and adds an error for each duplicate, unknown dependency and cycle.</summary>
    private static Dictionary<string, Definition> Check(List<string> errors)
    {
        var targets = new Dictionary<string, Definition>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in Declared)
        {
            if (!targets.TryAdd(target.Name, target))
                errors.Add($"Target {target.Name} is declared more than once.");
        }

        foreach (var target in targets.Values)
        foreach (var dependency in target.DependsOn.Where(dependency => !targets.ContainsKey(dependency)))
            errors.Add($"Target {target.Name} depends on unknown target {dependency}.");

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = new List<string>();
        foreach (var target in targets.Values)
            FindCycles(target);

        return targets;

        void FindCycles(Definition target)
        {
            var start = path.FindIndex(name => string.Equals(name, target.Name, StringComparison.OrdinalIgnoreCase));
            if (start >= 0)
            {
                errors.Add($"Dependency cycle: {string.Join(" -> ", path.Skip(start).Append(target.Name))}.");
                return;
            }

            if (!visited.Add(target.Name))
                return;

            path.Add(target.Name);
            foreach (var dependency in target.DependsOn)
            {
                if (targets.TryGetValue(dependency, out var next))
                    FindCycles(next);
            }
            path.RemoveAt(path.Count - 1);
        }
    }

    private static void AddWithDependencies(Definition target, Dictionary<string, Definition> targets, List<Definition> plan)
    {
        if (plan.Contains(target))
            return;

        foreach (var dependency in target.DependsOn)
            AddWithDependencies(targets[dependency], targets, plan);

        plan.Add(target);
    }
}
