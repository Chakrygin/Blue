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

    public bool InstallTemplate(string templateDir)
    {
        Console.Error.WriteLine("Installing template...");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        psi.ArgumentList.Add("new");
        psi.ArgumentList.Add("install");
        psi.ArgumentList.Add(templateDir);

        using var process = new Process { StartInfo = psi };

        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            Console.Error.WriteLine("Failed to start dotnet process.");
            return false;
        }

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) Console.Error.WriteLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) Console.Error.WriteLine(e.Data);
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
            return false;

        return true;
    }

    public bool CreateProject(string runId, string[] extraArgs)
    {
        Console.Error.WriteLine("Creating project...");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        psi.ArgumentList.Add("new");
        psi.ArgumentList.Add(runId);

        foreach (var arg in extraArgs)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = psi };

        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            Console.Error.WriteLine("Failed to start dotnet process.");
            return false;
        }

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) Console.Error.WriteLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) Console.Error.WriteLine(e.Data);
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
            return false;

        return true;
    }

    public void UninstallTemplate(string templateDir)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            psi.ArgumentList.Add("new");
            psi.ArgumentList.Add("uninstall");
            psi.ArgumentList.Add(templateDir);

            using var process = new Process { StartInfo = psi };
            process.Start();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to uninstall template: {ex.Message}");
        }
    }
}