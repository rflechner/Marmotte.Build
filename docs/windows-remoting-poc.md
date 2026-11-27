# Windows remoting proof of concept

This is an experimental, agentless Windows deployment building block. It uses
the Microsoft PowerShell SDK and a WSMan runspace, not a child process or an
Azure DevOps deployment agent. The default endpoint is Windows PowerShell
(`Microsoft.PowerShell`), so PowerShell 7 need not be installed on the target.

## Local smoke test

On a trusted private network, open **Windows PowerShell 5.1 as administrator**.
On a dedicated development machine, enable remoting if it is not already
configured:

```powershell
Enable-PSRemoting -Force
Test-WSMan localhost
$session = New-PSSession -ComputerName localhost
Remove-PSSession $session
```

Enabling remoting changes the machine's WinRM listeners, session configuration
and firewall rules. Do not run this on public networks or expose WinRM to the
internet. On managed machines, follow your administrator's policy.

From the repository root, still in the elevated terminal, with the .NET 11 SDK:

```powershell
dotnet run --file examples/windows-remoting.cs
```

Expected output (values depend on the machine):

```text
Remote computer: MY-PC
Remote user: my-user
WinRM service: Running
```

The example opens a real loopback remoting session and executes three read-only
commands in the same remote runspace. It closes the session even if a command
fails and exits nonzero on failure. No password is requested by the example;
the connection uses the Windows identity of the calling process.

For a trusted remote host with WinRM already configured and permissions granted,
you can pass its DNS name:

```powershell
dotnet run --file examples/windows-remoting.cs -- server.example.test
```

The helper supports HTTP (5985) or HTTPS (5986) with normal certificate
validation. It uses Negotiate authentication (Kerberos or NTLM as appropriate);
it does not enable Basic, CredSSP, TrustedHosts or certificate bypasses. Prefer
domain Kerberos or properly configured HTTPS for remote deployments. A localhost
test does not establish that domain authentication, firewall rules or a remote
server's permissions work.

## Scope

- Reusable synchronous session and parameterized command invocation.
- Windows only at runtime; the project is built with the solution.
- No publishing changes, no remote configuration changes by the example, no
  automatic deployment and no remoting requirement for the normal CI tests.
- The connection uses a 30-second open timeout and a 60-second WSMan operation
  timeout (not an overall command deadline).
- File transfers, service management, explicit credentials, cancellation and
  rollback are intentionally deferred until the transport is validated.

References:
- [PowerShell remoting requirements](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_remote_requirements?view=powershell-5.1)
- [Microsoft.PowerShell.SDK 7.7.0](https://www.nuget.org/packages/Microsoft.PowerShell.SDK/7.7.0)
