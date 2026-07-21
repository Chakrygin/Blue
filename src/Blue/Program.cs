using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
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

        string repoUrl;
        string? branch;

        {
            var atIndex = templateId.IndexOf('@');
            if (atIndex >= 0)
            {
                branch = templateId[(atIndex + 1)..];
                templateId = templateId[..atIndex];
            }
            else
            {
                branch = null;
            }

            if (templateId.StartsWith("https://"))
            {
                repoUrl = templateId;
            }
            else
            {
                var parts = templateId.Split('/');
                if (parts.Length != 2
                    || string.IsNullOrWhiteSpace(parts[0])
                    || string.IsNullOrWhiteSpace(parts[1])
                    || !IsValidName(parts[0])
                    || !IsValidName(parts[1]))
                {
                    Console.Error.WriteLine("Invalid template-id. Expected format: owner/repo or owner/repo@version or https://...");
                    return 1;
                }

                repoUrl = $"https://github.com/{parts[0]}/{parts[1]}.git";
            }
        }

        string runId;

        {
            var guid = Guid.NewGuid();
            var bytes = guid.ToByteArray();
            var value = new BigInteger(bytes, isUnsigned: true);

            const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
            var sb = new StringBuilder();

            while (value > 0)
            {
                value = BigInteger.DivRem(value, alphabet.Length, out var remainder);
                sb.Insert(0, alphabet[(int)remainder]);
            }

            runId = sb.ToString();
        }

        string cloneDir;

        {
            cloneDir = Path.Combine(Path.GetTempPath(), $"blue_{runId}_r");
        }

        return NewCommand.Execute(templateId, extraArgs);
    }

    private static bool IsValidName(string name)
    {
        return Regex.IsMatch(name, @"^[a-zA-Z0-9._-]+$");
    }
}