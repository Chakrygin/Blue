using System.ComponentModel;
using System.Diagnostics;

namespace Blue.Tools.DotNet;

internal class DotNetTool
{
    public bool CheckAvailable()
    {
        Console.Error.WriteLine("Checking for dotnet...");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        psi.ArgumentList.Add("--version");

        using var process = new Process { StartInfo = psi };

        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            Console.Error.WriteLine("Oops... dotnet not found. Please install .NET SDK from https://dotnet.microsoft.com/download");
            return false;
        }

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            Console.Error.WriteLine($"dotnet found: {output.Trim()}");
            return true;
        }

        Console.Error.WriteLine("Oops... dotnet not found. Please install .NET SDK from https://dotnet.microsoft.com/download");
        return false;
    }
}