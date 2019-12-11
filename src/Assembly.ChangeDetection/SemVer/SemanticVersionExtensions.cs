// -----------------------------------------------------------------------
// <copyright file="SemanticVersionExtensions.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.SemVer
{
    /// <summary>
    /// Extensions for <see cref="NuGet.Versioning.SemanticVersion"/>.
    /// </summary>
    internal static class SemanticVersionExtensions
    {
        /// <summary>
        /// Creates a new instance of <see cref="NuGet.Versioning.SemanticVersion"/> with the specific changes.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="major">The major changes.</param>
        /// <param name="minor">The minor changes.</param>
        /// <param name="patch">The patch changes.</param>
        /// <param name="releaseLabel">The release label changes.</param>
        /// <param name="metadata">The metadata changes.</param>
        /// <returns>A new instance of <see cref="NuGet.Versioning.SemanticVersion"/> with the specific changes.</returns>
        public static NuGet.Versioning.SemanticVersion Change(this NuGet.Versioning.SemanticVersion version, int? major = default, int? minor = default, int? patch = default, string? releaseLabel = default, string? metadata = default) => new NuGet.Versioning.SemanticVersion(major ?? version.Major, minor ?? version.Minor, patch ?? version.Patch, releaseLabel ?? version.Release, metadata ?? version.Metadata);
    }
}
