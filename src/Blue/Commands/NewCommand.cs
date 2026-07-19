using System.Text.Json;
using System.Text.Json.Nodes;
using static Blue.Helpers;

namespace Blue.Commands;

internal static class NewCommand
{
    internal static int Execute(string templateId, string[] extraArgs)
    {
        var atIndex = templateId.IndexOf('@');
        string ownerRepo;
        string? version = null;
        if (atIndex >= 0)
        {
            ownerRepo = templateId[..atIndex];
            version = templateId[(atIndex + 1)..];
        }
        else
        {
            ownerRepo = templateId;
        }

        var templateParts = ownerRepo.Split('/');
        if (templateParts.Length != 2
            || string.IsNullOrWhiteSpace(templateParts[0])
            || string.IsNullOrWhiteSpace(templateParts[1]))
        {
            Console.Error.WriteLine("Invalid template-id. Expected format: owner/repo or owner/repo@version");
            return 1;
        }

        var repoUrl = $"https://github.com/{templateParts[0]}/{templateParts[1]}.git";
        var templateRegistryId = Guid.NewGuid().ToString("D");

        if (!IsToolAvailable("git", "--version"))
        {
            Console.Error.WriteLine("Git is not available. Install Git and ensure it's in PATH.");
            return 1;
        }

        string? cloneDir = null;
        string? templateDir = null;
        var isInstalled = false;

        try
        {
            cloneDir = CreateTempDirectory();
            var cloneArgs = new List<string> { "clone", "--depth", "1" };
            if (version != null)
            {
                cloneArgs.Add("--branch");
                cloneArgs.Add(version);
            }
            cloneArgs.Add(repoUrl);
            cloneArgs.Add(cloneDir);
            Console.Error.WriteLine($"Cloning {repoUrl}{(version != null ? $" (branch/tag: {version})" : "")}...");
            RunProcess("git", out var gitExit, out _, out var gitErr, [.. cloneArgs]);
            if (gitExit != 0)
            {
                Console.Error.WriteLine($"Failed to clone repository:{Environment.NewLine}{gitErr}");
                return 1;
            }

            templateDir = CreateTempDirectory();
            CopyDirectoryExcluding(cloneDir, templateDir, ".git");

            var configDir = Path.Combine(templateDir, ".template.config");
            var configFile = Path.Combine(configDir, "template.json");

            if (File.Exists(configFile))
            {
                UpdateTemplateIdentity(configFile, templateRegistryId);
            }
            else
            {
                Directory.CreateDirectory(configDir);
                CreateDefaultTemplateConfig(configFile, templateRegistryId, repoUrl);
            }

            Console.Error.WriteLine("Installing template...");
            RunProcess("dotnet", out var installExit, out _, out var installErr,
                "new", "install", templateDir);
            if (installExit != 0)
            {
                Console.Error.WriteLine($"Failed to install template:{Environment.NewLine}{installErr}");
                return 1;
            }
            isInstalled = true;

            Console.Error.WriteLine("Creating project from template...");
            var newArgs = new List<string> { "new", templateRegistryId };
            newArgs.AddRange(extraArgs);

            RunProcess("dotnet", out var createExit, out _, out var createErr,
                [.. newArgs]);
            if (createExit != 0)
            {
                Console.Error.WriteLine($"Failed to create project:{Environment.NewLine}{createErr}");
                return 1;
            }

            return 0;
        }
        finally
        {
            if (isInstalled && templateDir != null)
            {
                try
                {
                    RunProcess("dotnet", out _, out _, out _,
                        "new", "uninstall", templateDir);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            if (cloneDir != null)
            {
                try { Directory.Delete(cloneDir, recursive: true); }
                catch { /* Ignore */ }
            }

            if (templateDir != null)
            {
                try { Directory.Delete(templateDir, recursive: true); }
                catch { /* Ignore */ }
            }
        }
    }

    private static void CopyDirectoryExcluding(string sourceDir, string destDir, string excludeName)
    {
        foreach (var dirPath in Directory.EnumerateDirectories(
            sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, dirPath);
            var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (parts.Any(p => string.Equals(p, excludeName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            Directory.CreateDirectory(Path.Combine(destDir, relative));
        }

        foreach (var filePath in Directory.EnumerateFiles(
            sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, filePath);
            var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (parts.Any(p => string.Equals(p, excludeName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var destFile = Path.Combine(destDir, relative);
            var destParent = Path.GetDirectoryName(destFile);
            if (destParent != null)
            {
                Directory.CreateDirectory(destParent);
            }

            File.Copy(filePath, destFile, overwrite: true);
        }
    }

    private static void UpdateTemplateIdentity(string configFilePath, string identity)
    {
        var json = JsonNode.Parse(File.ReadAllText(configFilePath));
        if (json is JsonObject obj)
        {
            obj["shortName"] = identity;
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(configFilePath, obj.ToJsonString(options));
        }
    }

    private static void CreateDefaultTemplateConfig(string configFilePath, string identity, string repoUrl)
    {
        var config = new JsonObject
        {
            ["$schema"] = "http://json.schemastore.org/template",
            ["identity"] = identity,
            ["name"] = repoUrl,
            ["shortName"] = identity
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(configFilePath, config.ToJsonString(options));
    }
}
