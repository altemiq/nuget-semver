// -----------------------------------------------------------------------
// <copyright file="WriteVersionToFile.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning;

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

    /// <summary>
    /// Gets or sets the package ID.
    /// </summary>
    public string? PackageId { get; set; }

    /// <inheritdoc/>
    public override bool Execute()
    {
        const string NonBreakingSpace = " ";

        this.Lines = GetValues()
            .Select(s => new TaskItem(s))
            .ToArray();

        return base.Execute();

        IEnumerable<string> GetValues()
        {
            yield return this.Version ?? NonBreakingSpace;
            yield return this.VersionPrefix ?? NonBreakingSpace;
            yield return this.VersionSuffix ?? NonBreakingSpace;
            yield return this.RepositoryCommit ?? NonBreakingSpace;
            yield return this.PackageId ?? NonBreakingSpace;
        }
    }
}