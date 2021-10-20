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
internal record GitCommit(string Sha, System.DateTimeOffset AuthorDate, System.DateTimeOffset CommitterDate)
{
    /// <summary>
    /// Parses a new instance of a <see cref="GitCommit"/> record.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <returns>The <see cref="GitCommit"/> record.</returns>
    public static GitCommit Parse(string line)
    {
        var split = line.Split(' ');
        var sha = split[0];
        var authorDate = System.DateTimeOffset.Parse(split[1], formatProvider: null, System.Globalization.DateTimeStyles.RoundtripKind);
        var committerDate = System.DateTimeOffset.Parse(split[2], formatProvider: null, System.Globalization.DateTimeStyles.RoundtripKind);
        return new GitCommit(sha, authorDate, committerDate);
    }
}