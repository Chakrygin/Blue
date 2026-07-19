using System.Reflection;
using static Blue.Helpers;

namespace Blue.Commands;

internal static class VersionCommand
{
    internal static int Execute()
    {
        var assembly = Assembly.GetEntryAssembly();
        var informationalVersion = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var blueVersion = informationalVersion ?? "unknown";
        RunProcess("dotnet", out _, out var dotnetOut, out _, "--version");
        var dotnetVersion = dotnetOut?.Trim() ?? "not found";
        RunProcess("git", out _, out var gitOut, out _, "--version");
        var gitVersion = gitOut?.Trim() ?? "not found";

        Console.Error.WriteLine($"Blue {blueVersion}");
        Console.Error.WriteLine($".NET {dotnetVersion}");
        Console.Error.WriteLine(gitVersion);

        return 0;
    }
}
