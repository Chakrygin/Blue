using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using Blue.Commands;

namespace Blue;

internal partial class Program
{
    private int Run(string templateId, string[] extraArgs)
    {
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
                return 1;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Console.Error.WriteLine($"Git found: {output.Trim()}");
            }
            else
            {
                Console.Error.WriteLine("Oops... Git not found. Please install Git from https://git-scm.com/");
                return 1;
            }
        }

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
                return 1;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Console.Error.WriteLine($"dotnet found: {output.Trim()}");
            }
            else
            {
                Console.Error.WriteLine("Oops... dotnet not found. Please install .NET SDK from https://dotnet.microsoft.com/download");
                return 1;
            }
        }

        return NewCommand.Execute(templateId, extraArgs);
    }
}