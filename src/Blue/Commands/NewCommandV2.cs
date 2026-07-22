using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Blue.Tools.DotNet;
using Blue.Tools.Git;

namespace Blue.Commands;

internal class NewCommandV2
{
    private readonly GitTool _gitTool = new();
    private readonly DotNetTool _dotnetTool = new();

    public int Run(string templateId, string[] extraArgs)
    {
        if (!_gitTool.CheckAvailable())
            return 1;

        if (!_dotnetTool.CheckAvailable())
            return 1;

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
        string templateDir;

        {
            cloneDir = Path.Combine(Path.GetTempPath(), $"blue_{runId}_r");
            templateDir = Path.Combine(Path.GetTempPath(), $"blue_{runId}_t");
        }

        var isTemplateInstalled = false;

        try
        {
            if (!_gitTool.Clone(repoUrl, branch, cloneDir))
                return 1;

            {
                Console.Error.WriteLine("Copying template files...");
                Directory.CreateDirectory(templateDir);

                foreach (var dirPath in Directory.EnumerateDirectories(
                    cloneDir, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(cloneDir, dirPath);
                    var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (parts.Any(p => string.Equals(p, ".git", StringComparison.OrdinalIgnoreCase)))
                        continue;

                    Console.Error.WriteLine($"  {relative.Replace('\\', '/')}/");
                    Directory.CreateDirectory(Path.Combine(templateDir, relative));
                }

                foreach (var filePath in Directory.EnumerateFiles(
                    cloneDir, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(cloneDir, filePath);
                    var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (parts.Any(p => string.Equals(p, ".git", StringComparison.OrdinalIgnoreCase)))
                        continue;

                    var destFile = Path.Combine(templateDir, relative);
                    var destParent = Path.GetDirectoryName(destFile);
                    if (destParent != null)
                        Directory.CreateDirectory(destParent);

                    Console.Error.WriteLine($"  {relative.Replace('\\', '/')}");
                    File.Copy(filePath, destFile, overwrite: true);
                }
            }

            {
                var configDir = Path.Combine(templateDir, ".template.config");
                var configFile = Path.Combine(configDir, "template.json");

                if (File.Exists(configFile))
                {
                    Console.Error.WriteLine("Modifying template.json...");
                    var json = JsonNode.Parse(File.ReadAllText(configFile));
                    if (json is JsonObject obj)
                    {
                        obj["shortName"] = runId;
                        var options = new JsonSerializerOptions { WriteIndented = true };
                        File.WriteAllText(configFile, obj.ToJsonString(options));
                    }
                }
                else
                {
                    Console.Error.WriteLine("Generating template.json...");
                    Directory.CreateDirectory(configDir);

                    var sourceName = ComputeSourceName(templateDir);

                    var config = new JsonObject
                    {
                        ["$schema"] = "http://json.schemastore.org/template",
                        ["identity"] = runId,
                        ["name"] = repoUrl,
                        ["shortName"] = runId
                    };

                    if (sourceName != null)
                    {
                        config["sourceName"] = sourceName;
                    }

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(configFile, config.ToJsonString(options));
                }
            }

            if (!_dotnetTool.InstallTemplate(templateDir))
                return 1;

            isTemplateInstalled = true;

            if (!_dotnetTool.CreateProject(runId, extraArgs))
                return 1;
        }
        finally
        {
            if (isTemplateInstalled)
                _dotnetTool.UninstallTemplate(templateDir);

            ForceDeleteDirectory(cloneDir);
            ForceDeleteDirectory(templateDir);

            void ForceDeleteDirectory(string path)
            {
                if (!Directory.Exists(path)) return;

                try
                {
                    foreach (var file in Directory.GetFiles(path))
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }

                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        ForceDeleteDirectory(dir);
                    }

                    Directory.Delete(path);
                }
                catch { }
            }
        }

        return 0;
    }

    private static string? ComputeSourceName(string dir)
    {
        var solutionFiles = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .ToList();

        if (solutionFiles.Count == 0)
            return null;

        if (solutionFiles.Count == 1)
            return solutionFiles[0];

        var segments = solutionFiles.Select(f => f.Split('.')).ToList();
        var prefix = segments[0];
        for (var i = 1; i < segments.Count; i++)
        {
            var minLen = Math.Min(prefix.Length, segments[i].Length);
            var j = 0;
            while (j < minLen && string.Equals(prefix[j], segments[i][j], StringComparison.OrdinalIgnoreCase))
                j++;
            prefix = prefix[..j];
        }

        return prefix.Length > 0 ? string.Join(".", prefix) : null;
    }

    private static bool IsValidName(string name)
    {
        return Regex.IsMatch(name, @"^[a-zA-Z0-9._-]+$");
    }
}