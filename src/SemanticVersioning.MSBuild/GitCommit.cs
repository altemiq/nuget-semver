// -----------------------------------------------------------------------
// <copyright file="GitCommit.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

/// <summary>
/// Represents a git commit.
/// </summary>
/// <param name="Sha">The SHA.</param>
/// <param name="AuthorDate">The author date.</param>
/// <param name="CommitterDate">The committer date.</param>
internal record GitCommit(string Sha, DateTimeOffset AuthorDate, DateTimeOffset CommitterDate)
{
    /// <summary>
    /// Parses a new instance of a <see cref="GitCommit"/> record.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <returns>The <see cref="GitCommit"/> record.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S6580:Use a format provider when parsing date and time", Justification = "This is correct")]
    public static GitCommit Parse(string line)
    {
        var split = line.Split(' ');
        var sha = split[0];
        var authorDate = DateTimeOffset.Parse(split[1], formatProvider: null, System.Globalization.DateTimeStyles.RoundtripKind);
        var committerDate = DateTimeOffset.Parse(split[2], formatProvider: null, System.Globalization.DateTimeStyles.RoundtripKind);
        return new GitCommit(sha, authorDate, committerDate);
    }
}