using System.CommandLine;
using Blue.Commands;

namespace Blue;

internal partial class Program
{
    private int Run(string templateId, string[] extraArgs)
    {
        return NewCommand.Execute(templateId, extraArgs);
    }
}