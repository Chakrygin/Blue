using System.ComponentModel;
using System.Diagnostics;

namespace Blue.Tools.Git;

internal class GitTool
{
    public bool CheckAvailable()
    {
        Console.Error.WriteLine("Checking for Git...");

        var psi = new ProcessStartInfo
        {
            FileName = "git",
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
            Console.Error.WriteLine("Oops... Git not found. Please install Git from https://git-scm.com/");
            return false;
        }

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            Console.Error.WriteLine($"Git found: {output.Trim()}");
            return true;
        }

        Console.Error.WriteLine("Oops... Git not found. Please install Git from https://git-scm.com/");
        return false;
    }
}