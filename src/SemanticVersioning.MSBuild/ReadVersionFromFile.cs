// -----------------------------------------------------------------------
// <copyright file="ReadVersionFromFile.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning;

/// <summary>
/// Reads the version from the file.
/// </summary>
public sealed class ReadVersionFromFile : Microsoft.Build.Tasks.ReadLinesFromFile
{
    /// <summary>
    /// Gets the version.
    /// </summary>
    [Output]
    public string? Version { get; private set; }

    /// <summary>
    /// Gets the version prefix.
    /// </summary>
    [Output]
    public string? VersionPrefix { get; private set; }

    /// <summary>
    /// Gets the version suffix.
    /// </summary>
    [Output]
    public string? VersionSuffix { get; private set; }

    /// <summary>
    /// Gets the repository commit.
    /// </summary>
    [Output]
    public string? RepositoryCommit { get; private set; }

    /// <inheritdoc/>
    public override bool Execute()
    {
        if (base.Execute())
        {
            if (this.Lines.Length > 0)
            {
                this.Version = this.Lines[0].ItemSpec;
            }

            if (this.Lines.Length > 1)
            {
                this.VersionPrefix = this.Lines[1].ItemSpec;
            }

            if (this.Lines.Length > 2)
            {
                this.VersionSuffix = this.Lines[2].ItemSpec;
            }

            if (this.Lines.Length > 3)
            {
                this.RepositoryCommit = this.Lines[3].ItemSpec;
            }

            return true;
        }

        return false;
    }
}