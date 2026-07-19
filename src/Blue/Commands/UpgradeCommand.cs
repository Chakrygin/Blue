using static Blue.Helpers;

namespace Blue.Commands;

internal static class UpgradeCommand
{
    internal static int Execute()
    {
        RunProcess("dotnet", out var exitCode, out var stdOut, out var stdErr,
            "tool", "update", "--global", "blue");

        Console.Error.Write(stdErr);
        Console.Out.Write(stdOut);

        return exitCode;
    }
}
