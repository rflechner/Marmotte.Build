#!/usr/bin/env dotnet
#:project ../src/Marmotte.Build.Deploy.Windows/Marmotte.Build.Deploy.Windows.csproj
#:property PublishAot=false

using Marmotte.Build.Deploy.Windows;

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("This example requires Windows.");
    return 1;
}

if (args.Length > 1)
{
    Console.Error.WriteLine("Usage: windows-remoting.cs [computer-name]");
    return 1;
}

var computerName = args.Length == 0 ? "localhost" : args[0];

try
{
    using var session = RemoteSession.Connect(computerName);

    var computer = session.Invoke("Get-Item", new Dictionary<string, object?>
    {
        ["LiteralPath"] = "Env:COMPUTERNAME"
    }).Single();

    var user = session.Invoke("Get-Item", new Dictionary<string, object?>
    {
        ["LiteralPath"] = "Env:USERNAME"
    }).Single();

    var service = session.Invoke("Get-Service", new Dictionary<string, object?>
    {
        ["Name"] = "WinRM"
    }).Single();

    Console.WriteLine($"Remote computer: {computer.Properties["Value"].Value}");
    Console.WriteLine($"Remote user: {user.Properties["Value"].Value}");
    Console.WriteLine($"WinRM service: {service.Properties["Status"].Value}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"PowerShell remoting failed: {ex.Message}");
    return 1;
}
