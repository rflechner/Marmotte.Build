#!/usr/bin/env dotnet
#:project src/Marmotte.Build.Deploy.Windows/Marmotte.Build.Deploy.Windows.csproj
#:property PublishAot=false

using Marmotte.Build.Deploy.Windows;

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("This proof of concept requires Windows.");
    return 1;
}

if (args.Length > 1)
{
    Console.Error.WriteLine("Usage: deploy.cs [computer-name]");
    return 1;
}

var computerName = args.Length == 0 ? "localhost" : args[0];

try
{
    using var session = RemoteSession.Connect(computerName);

    var remoteComputer = session.Invoke("Get-Item", new Dictionary<string, object?>
    {
        ["LiteralPath"] = "Env:COMPUTERNAME"
    }).Single().Properties["Value"].Value;

    var remoteTemp = session.Invoke("Get-Item", new Dictionary<string, object?>
    {
        ["LiteralPath"] = "Env:TEMP"
    }).Single().Properties["Value"].Value?.ToString()
        ?? throw new InvalidOperationException("The remote TEMP variable is missing.");

    // Use a unique name, and never overwrite an existing file.
    var id = Guid.NewGuid().ToString("N");
    var remotePath = Path.Combine(remoteTemp, $"marmotte-deploy-{id}.txt");
    var payload = $"Hello from Marmotte Build! {id}";

    session.Invoke("New-Item", new Dictionary<string, object?>
    {
        ["Path"] = remotePath,
        ["ItemType"] = "File",
        ["Value"] = payload
    });

    var actual = session.Invoke("Get-Content", new Dictionary<string, object?>
    {
        ["LiteralPath"] = remotePath,
        ["Raw"] = true
    }).Single().BaseObject?.ToString();

    if (!string.Equals(payload, actual, StringComparison.Ordinal))
        throw new InvalidOperationException("Remote file content did not match the original payload.");

    Console.WriteLine($"Remote computer: {remoteComputer}");
    Console.WriteLine($"Remote file: {remotePath}");
    Console.WriteLine("Remote write/read verification succeeded.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"PowerShell remoting failed: {ex.Message}");
    return 1;
}
