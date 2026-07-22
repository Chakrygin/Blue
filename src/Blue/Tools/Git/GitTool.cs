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

    public bool Clone(string repoUrl, string? branch, string cloneDir)
    {
        var branchSuffix = branch != null ? $" ({branch})" : "";
        Console.Error.WriteLine($"Cloning {repoUrl}{branchSuffix}...");

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        psi.ArgumentList.Add("clone");
        psi.ArgumentList.Add("--depth");
        psi.ArgumentList.Add("1");

        if (branch != null)
        {
            psi.ArgumentList.Add("--branch");
            psi.ArgumentList.Add(branch);
        }

        psi.ArgumentList.Add(repoUrl);
        psi.ArgumentList.Add(cloneDir);

        using var process = new Process { StartInfo = psi };

        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            Console.Error.WriteLine("Failed to start git clone process.");
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
}