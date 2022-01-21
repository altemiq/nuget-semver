// -----------------------------------------------------------------------
// <copyright file="PackageCommitIdentity.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning;

/// <summary>
/// The package commit identity.
/// </summary>
public class PackageCommitIdentity : NuGet.Packaging.Core.PackageIdentity
{
    /// <summary>
    /// Initialises a new instance of the <see cref="PackageCommitIdentity"/> class.
    /// </summary>
    /// <param name="id">The package name.</param>
    /// <param name="version">The package version.</param>
    /// <param name="commit">The package commit.</param>
    public PackageCommitIdentity(string id, NuGet.Versioning.NuGetVersion version, string? commit)
        : base(id, version) => this.Commit = commit;

    /// <summary>
    /// Gets the commit.
    /// </summary>
    public string? Commit { get; }

    /// <summary>
    /// Gets a value indicating whether the commit is non-null.
    /// </summary>
    public bool HasCommit => this.Commit is not null;
}