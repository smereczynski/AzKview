using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace AzKview.Core.Tests;

internal static class TestEnv
{
    [ModuleInitializer]
    public static void Initialize()
    {
        try
        {
            var repoRoot = GetRepoRoot();
            var envPath = Path.Combine(repoRoot, ".env.local");
            if (File.Exists(envPath))
            {
                LoadDotEnv(envPath);
            }
        }
        catch
        {
            // Non-fatal for tests; proceed with whatever env is present
        }
    }

    private static string GetRepoRoot()
    {
        // tests/AzKview.Core.Tests/bin/.../ -> ascend until we find the .git or solution file
        var dir = AppContext.BaseDirectory;
        DirectoryInfo? d = new DirectoryInfo(dir);
        while (d != null)
        {
            if (File.Exists(Path.Combine(d.FullName, "AzKview.sln")) || Directory.Exists(Path.Combine(d.FullName, ".git")))
            {
                return d.FullName;
            }
            d = d.Parent;
        }
        // Fallback to current base dir
        return AppContext.BaseDirectory;
    }

    private static void LoadDotEnv(string path)
    {
        foreach (var rawLine in File.ReadAllLines(path, Encoding.UTF8))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#"))
                continue;
            var idx = line.IndexOf('=');
            if (idx <= 0)
                continue;
            var key = line.Substring(0, idx).Trim();
            var value = line.Substring(idx + 1).Trim();
            if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'")))
            {
                value = value.Substring(1, value.Length - 2);
            }
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
