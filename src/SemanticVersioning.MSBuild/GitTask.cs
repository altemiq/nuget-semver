// -----------------------------------------------------------------------
// <copyright file="GitTask.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    /// <summary>
    /// The base GIT task.
    /// </summary>
    public abstract class GitTask : Microsoft.Build.Utilities.ToolTask
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
                var git = System.IO.Path.Combine(directory, ".git");
                if (System.IO.Directory.Exists(git))
                {
                    return directory;
                }

                directory = System.IO.Path.GetDirectoryName(directory);
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

            return System.IO.Path.Combine(this.ToolPath, this.ToolExe);
        }
    }
}