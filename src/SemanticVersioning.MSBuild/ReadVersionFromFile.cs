// -----------------------------------------------------------------------
// <copyright file="ReadVersionFromFile.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

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

    /// <summary>
    /// Gets the package ID.
    /// </summary>
    [Output]
    public string? PackageId { get; private set; }

    /// <inheritdoc/>
    public override bool Execute()
    {
        const char NonBreakingSpace = ' ';

        if (base.Execute())
        {
            if (this.Lines.Length > 0)
            {
                this.Version = GetValue(this.Lines[0].ItemSpec);
            }

            if (this.Lines.Length > 1)
            {
                this.VersionPrefix = GetValue(this.Lines[1].ItemSpec);
            }

            if (this.Lines.Length > 2)
            {
                this.VersionSuffix = GetValue(this.Lines[2].ItemSpec);
            }

            if (this.Lines.Length > 3)
            {
                this.RepositoryCommit = GetValue(this.Lines[3].ItemSpec);
            }

            if (this.Lines.Length > 4)
            {
                this.PackageId = GetValue(this.Lines[4].ItemSpec);
            }

            return true;

            static string? GetValue(string input)
            {
                var trim = input.Trim(NonBreakingSpace);
                return string.IsNullOrWhiteSpace(trim) ? default : trim;
            }
        }

        return false;
    }
}