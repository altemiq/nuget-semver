// -----------------------------------------------------------------------
// <copyright file="WriteVersionToFile.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

/// <summary>
/// Writes the version to a file.
/// </summary>
public sealed class WriteVersionToFile : Microsoft.Build.Tasks.WriteLinesToFile
{
    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the version prefix.
    /// </summary>
    public string? VersionPrefix { get; set; }

    /// <summary>
    /// Gets or sets the version suffix.
    /// </summary>
    public string? VersionSuffix { get; set; }

    /// <summary>
    /// Gets or sets the repository commit.
    /// </summary>
    public string? RepositoryCommit { get; set; }

    /// <inheritdoc/>
    public override bool Execute()
    {
        this.Lines = new ITaskItem[]
        {
            new TaskItem(this.Version),
            new TaskItem(this.VersionPrefix),
            new TaskItem(this.VersionSuffix),
            new TaskItem(this.RepositoryCommit),
        };

        return base.Execute();
    }
}