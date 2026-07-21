using Blue.Commands;

if (args.Length < 1)
{
    PrintUsage();
    return 1;
}

return args[0].ToLowerInvariant() switch
{
    "new" => args.Length < 2
        ? PrintUsage()
        : NewCommand.Execute(args[1], args[2..]),
    _ => PrintUsage()
};

static int PrintUsage()
{
    Console.Error.WriteLine("Usage: blue new <template-id> [template-args...]");
    Console.Error.WriteLine("Example: blue new owner/repo -n MyProject --output ./MyProject");
    return 1;
}
