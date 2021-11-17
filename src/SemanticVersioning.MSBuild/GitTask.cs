// -----------------------------------------------------------------------
// <copyright file="GitTask.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

/// <summary>
/// The base GIT task.
/// </summary>
public abstract class GitTask : ToolTask
{
    /// <inheritdoc/>
    protected override string ToolName => "git";

    /// <summary>
    /// Gets the base GIT directory.
    /// </summary>
    /// <param name="directory">The directory.</param>
    /// <returns>The base GIT directory.</returns>
    protected static string? GetBaseDirectory(string? directory)
    {
        while (directory is not null)
        {
            var git = Path.Combine(directory, ".git");
            if (Directory.Exists(git))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory);
        }

        return default;
    }

    /// <inheritdoc/>
    protected override string GenerateFullPathToTool()
    {
        if (this.ToolPath is null)
        {
            return this.ToolExe;
        }

        return Path.Combine(this.ToolPath, this.ToolExe);
    }
}