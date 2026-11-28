#!/usr/bin/env dotnet
#:package Spectre.Console
#:property PublishAot=false
#:include "src/Marmotte.Build/Runtime.cs"
#:include "src/Marmotte.Build/PathInfo.cs"
#:include "src/Marmotte.Build/Shell.cs"

// SSH deployment smoke test. Uses the OpenSSH client (ssh, scp) shipped with
// Windows 10/11, Windows Server 2019+, Linux and macOS: no extra NuGet package.

using Marmotte.Build;
using Spectre.Console;
using static Marmotte.Build.Shell;

AnsiConsole.Write(new FigletText("Marmotte Deploy"));

string? destination = null;
string? port = null;
string? identity = null;

for (var i = 0; i < args.Length; i++)
{
    if (args[i] is "--port" or "-p" && i + 1 < args.Length && int.TryParse(args[i + 1], out _))
        port = args[++i];
    else if (args[i] is "--identity" or "-i" && i + 1 < args.Length)
        identity = args[++i];
    else if (destination is null && !args[i].StartsWith('-'))
        destination = args[i];
    else
        return Usage();
}

if (destination is null)
    return Usage();

// Expand ~ ourselves: not every shell does it for arguments of native commands.
if (identity is ['~', '/' or '\\', ..])
    identity = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), identity[2..]);

if (identity is not null && !File.Exists(identity))
{
    Console.Error.WriteLine($"Private key not found: {identity}");
    return 1;
}

// BatchMode: never prompt (fail instead), so a missing key or an unknown host key
// is reported as an error rather than blocking an unattended deployment.
string[] options = ["-o", "BatchMode=yes", "-o", "ConnectTimeout=15"];

// IdentitiesOnly: offer only this key, not every key loaded in ssh-agent.
if (identity is not null)
    options = [.. options, "-i", Path.GetFullPath(identity), "-o", "IdentitiesOnly=yes"];

string[] sshOptions = port is null ? options : [.. options, "-p", port];
string[] scpOptions = port is null ? options : [.. options, "-P", port];

// scp would read "C:\..." as a host named "C", so local files are passed as
// names relative to a working directory instead of absolute paths.
var localDirectory = Directory.CreateTempSubdirectory("marmotte-deploy-");

try
{
    // Use a unique name so an existing remote file is never overwritten.
    var id = Guid.NewGuid().ToString("N");
    var remoteFile = $"marmotte-deploy-{id}.txt"; // Relative: resolved in the remote user's home directory.
    var payload = $"Hello from Marmotte Build! {id}";

    File.WriteAllText(Path.Combine(localDirectory.FullName, "upload.txt"), payload);

    Step("Remote computer name", Execute("ssh", [.. sshOptions, destination, "hostname"], null));
    Step("Upload", Execute("scp", [.. scpOptions, "upload.txt", $"{destination}:{remoteFile}"], localDirectory.FullName));
    Step("Download", Execute("scp", [.. scpOptions, $"{destination}:{remoteFile}", "download.txt"], localDirectory.FullName));

    var actual = File.ReadAllText(Path.Combine(localDirectory.FullName, "download.txt"));
    if (!string.Equals(payload, actual, StringComparison.Ordinal))
        throw new InvalidOperationException("Remote file content did not match the original payload.");

    Console.WriteLine($"Remote file: ~/{remoteFile}");
    Console.WriteLine("Remote write/read verification succeeded.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"SSH deployment failed: {ex.Message}");
    return 1;
}
finally
{
    localDirectory.Delete(recursive: true);
}

static void Step(string name, ShellExecuteResult result)
{
    // ssh and scp exit with 255 when the connection or authentication fails.
    if (result is ShellExecuteFailure { ExitCode: 255 })
        throw new InvalidOperationException(
            $"{name}: SSH connection or authentication failed. Check that sshd runs on the target, " +
            "that your public key is authorized there, that a passphrase-protected key is loaded in " +
            "ssh-agent (ssh-add), and that the host key is in known_hosts (connect once with ssh to accept it).");

    try
    {
        result.ThrowIfNotFailed();
    }
    catch (InvalidOperationException ex)
    {
        throw new InvalidOperationException($"{name}: {ex.Message}", ex);
    }
}

static int Usage()
{
    Console.Error.WriteLine("Usage: deploy-ssh.cs <[user@]host> [--port <port>] [--identity <private-key-path>]");
    return 1;
}
