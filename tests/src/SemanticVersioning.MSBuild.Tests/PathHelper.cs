// -----------------------------------------------------------------------
// <copyright file="PathHelper.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning.MSBuild;

internal static class PathHelper
{
    public static string GetProjectPath(string project)
    {
        var projectFolder = GetFolderAbove(GetTestDirectory(), "projects") ?? throw new DirectoryNotFoundException("projects");
        return Path.GetFullPath(Path.Combine(projectFolder, project, Path.GetFileName(project) + ".csproj"));
    }

    public static string? GetSolutionFolder()
    {
        return GetSolutionFolderCore(GetTestDirectory());

        static string? GetSolutionFolderCore(string? current)
        {
            while (current is not null)
            {
                if (Directory.EnumerateFiles(current, "*.slnx").Any())
                {
                    return current;
                }

                current = Path.GetDirectoryName(current);
            }

            return default;
        }
    }

    public static string GetSource(string source)
    {
        var nupkgFolder = GetFolderAbove(GetTestDirectory(), "nupkg") ?? throw new DirectoryNotFoundException("nupkg");
        return Path.GetFullPath(Path.Combine(nupkgFolder, source));
    }

    private static string GetTestDirectory() => Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(typeof(PathHelper).Assembly.Location)))) ?? throw new InvalidOperationException("Failed to get test directory");

    private static string? GetFolderAbove(string? current, string name)
    {
        while (current is not null)
        {
            var test = Path.Combine(current, name);
            if (Directory.Exists(test))
            {
                return test;
            }

            current = Path.GetDirectoryName(current);
        }

        return default;
    }
}
