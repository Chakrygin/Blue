using System.CommandLine;
using Blue.Commands;

var rootCommand = new RootCommand("Blue - project template scaffolding tool");

var templateArg = new Argument<string>("template-id")
{
    Description = "GitHub repository in owner/repo format, optionally with @version",
    Arity = ArgumentArity.ExactlyOne
};
var extraArgsArg = new Argument<string[]>("extra-args")
{
    Arity = ArgumentArity.ZeroOrMore,
    Description = "Additional arguments passed through to dotnet new"
};

var newCommand = new Command("new", "Create a new project from a GitHub template");
newCommand.Arguments.Add(templateArg);
newCommand.Arguments.Add(extraArgsArg);

newCommand.SetAction((parseResult) =>
{
    var templateId = parseResult.GetValue(templateArg);
    var extraArgs = parseResult.GetValue(extraArgsArg) ?? [];
    return NewCommand.Execute(templateId!, extraArgs);
});

rootCommand.Subcommands.Add(newCommand);

return rootCommand.Parse(args).Invoke();
