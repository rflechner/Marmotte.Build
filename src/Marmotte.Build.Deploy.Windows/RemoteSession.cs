using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.Versioning;

namespace Marmotte.Build.Deploy.Windows;

/// <summary>
/// A synchronous, reusable PowerShell Remoting session over WSMan.
/// Uses the current Windows identity; does not change WinRM configuration.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class RemoteSession : IDisposable
{
    private readonly Runspace _runspace;

    private RemoteSession(Runspace runspace) => _runspace = runspace;

    /// <summary>
    /// Connects to the standard Windows PowerShell endpoint using integrated authentication.
    /// </summary>
    public static RemoteSession Connect(string computerName = "localhost", bool useSsl = false, int? port = null)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("WSMan remoting requires Windows for this helper.");

        ArgumentException.ThrowIfNullOrWhiteSpace(computerName);

        var endpoint = new UriBuilder(
            useSsl ? "https" : "http",
            computerName,
            port ?? (useSsl ? 5986 : 5985),
            "wsman").Uri;

        var connection = new WSManConnectionInfo(endpoint)
        {
            AuthenticationMechanism = AuthenticationMechanism.Negotiate,
            OpenTimeout = 30_000,
            OperationTimeout = 60_000
        };

        var runspace = RunspaceFactory.CreateRunspace(connection);
        try
        {
            runspace.Open();
            return new RemoteSession(runspace);
        }
        catch
        {
            runspace.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Invokes a command in the remote runspace and returns its PowerShell objects.
    /// Parameters are bound individually, not interpolated into scripts.
    /// Terminating and non-terminating PowerShell errors fail the invocation.
    /// </summary>
    public IReadOnlyList<PSObject> Invoke(
        string commandName,
        IReadOnlyDictionary<string, object?>? parameters = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);

        using var powerShell = PowerShell.Create();
        powerShell.Runspace = _runspace;
        powerShell.AddCommand(commandName);

        if (parameters is not null)
        {
            foreach (var (name, value) in parameters)
                powerShell.AddParameter(name, value);
        }

        var output = powerShell.Invoke();

        if (powerShell.HadErrors)
            throw new InvalidOperationException(
                $"Remote command '{commandName}' failed: {string.Join(Environment.NewLine, powerShell.Streams.Error)}");

        return output.ToArray();
    }

    public void Dispose() => _runspace.Dispose();
}
