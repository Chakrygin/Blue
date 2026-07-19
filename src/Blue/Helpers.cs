using System.Diagnostics;

namespace Blue;

internal static class Helpers
{
    internal static bool RunProcess(
        string fileName,
        out int exitCode,
        out string stdOut,
        out string stdErr,
        params string[] arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = psi };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        process.WaitForExit();

        stdOut = outputTask.GetAwaiter().GetResult();
        stdErr = errorTask.GetAwaiter().GetResult();
        exitCode = process.ExitCode;
        return true;
    }

    internal static string CreateTempDirectory()
    {
        var blueDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".blue");
        var path = Path.Combine(blueDir, Guid.NewGuid().ToString("D"));
        Directory.CreateDirectory(path);
        return path;
    }

    internal static bool IsToolAvailable(string fileName, string testArgument)
    {
        try
        {
            RunProcess(fileName, out var exitCode, out _, out _, testArgument);
            return exitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
