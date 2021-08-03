// -----------------------------------------------------------------------
// <copyright file="NuGetVersion.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Methods for dealing with <see cref="NuGet.Versioning.NuGetVersion"/>.
    /// </summary>
    public static class NuGetVersion
    {
        /// <summary>
        /// The default alpha release.
        /// </summary>
        public const string DefaultAlphaRelease = "alpha";

        /// <summary>
        /// The default beta release.
        /// </summary>
        public const string DefaultBetaRelease = "beta";

        /// <summary>
        /// Calculates the version.
        /// </summary>
        /// <param name="semanticVersionChange">The semantic version change.</param>
        /// <param name="previousVersions">The previous versions.</param>
        /// <param name="prerelease">The prerelease tag.</param>
        /// <returns>The semantic version.</returns>
        public static NuGet.Versioning.SemanticVersion CalculateVersion(SemanticVersionChange semanticVersionChange, IEnumerable<string> previousVersions, string? prerelease)
        {
            var previousSemanticVersions = previousVersions.ToSemanticVersions();

            return CalculateVersion(semanticVersionChange, previousSemanticVersions, prerelease);
        }

        /// <summary>
        /// Calculates the version.
        /// </summary>
        /// <param name="semanticVersionChange">The semantic version change.</param>
        /// <param name="previousVersions">The previous versions.</param>
        /// <param name="prerelease">The prerelease tag.</param>
        /// <returns>The semantic version.</returns>
        public static NuGet.Versioning.SemanticVersion CalculateVersion(SemanticVersionChange semanticVersionChange, IEnumerable<NuGet.Versioning.SemanticVersion> previousVersions, string? prerelease) =>
            CalculateVersion(semanticVersionChange, previousVersions, GetReleaseVersion(previousVersions), prerelease);

        /// <summary>
        /// Calculates the version.
        /// </summary>
        /// <param name="semanticVersionChange">The semantic version change.</param>
        /// <param name="previousVersions">The previous versions.</param>
        /// <param name="previousVersion">The previous version.</param>
        /// <param name="prerelease">The prerelease tag.</param>
        /// <returns>The semantic version.</returns>
        public static NuGet.Versioning.SemanticVersion CalculateVersion(SemanticVersionChange semanticVersionChange, IEnumerable<NuGet.Versioning.SemanticVersion> previousVersions, NuGet.Versioning.SemanticVersion previousVersion, string? prerelease) =>
            CalculateNextVersion(previousVersion, GetLastestVersion(semanticVersionChange, previousVersions, previousVersion), prerelease);

        /// <summary>
        /// Calculates the next version.
        /// </summary>
        /// <param name="releaseVersion">The release version.</param>
        /// <param name="patchVersion">The patch version.</param>
        /// <param name="prerelease">The prerelease.</param>
        /// <returns>The next version.</returns>
        public static NuGet.Versioning.SemanticVersion CalculateNextVersion(NuGet.Versioning.SemanticVersion releaseVersion, NuGet.Versioning.SemanticVersion? patchVersion, string? prerelease)
        {
            if (patchVersion is null)
            {
                return releaseVersion.With(releaseLabel: prerelease ?? DefaultAlphaRelease);
            }

            return patchVersion.With(patch: patchVersion.Patch + 1, releaseLabel: prerelease ?? patchVersion.Release);
        }

        /// <summary>
        /// Gets the last version.
        /// </summary>
        /// <param name="semanticVersionChange">The semantic version change.</param>
        /// <param name="previousVersions">The previous versions.</param>
        /// <returns>The last version.</returns>
        public static NuGet.Versioning.SemanticVersion? GetLastestVersion(SemanticVersionChange semanticVersionChange, IEnumerable<string> previousVersions) =>
            GetLastestVersion(semanticVersionChange, previousVersions.ToSemanticVersions());

        /// <summary>
        /// Gets the last version.
        /// </summary>
        /// <param name="semanticVersionChange">The semantic version change.</param>
        /// <param name="previousVersions">The previous versions.</param>
        /// <returns>The last version.</returns>
        public static NuGet.Versioning.SemanticVersion? GetLastestVersion(SemanticVersionChange semanticVersionChange, IEnumerable<NuGet.Versioning.SemanticVersion> previousVersions) =>
            GetLastestVersion(semanticVersionChange, previousVersions, GetReleaseVersion(previousVersions));

        /// <summary>
        /// Gets the last version.
        /// </summary>
        /// <param name="semanticVersionChange">The semantic version change.</param>
        /// <param name="previousVersions">The previous versions.</param>
        /// <param name="previousVersion">The previous version.</param>
        /// <returns>The last version.</returns>
        public static NuGet.Versioning.SemanticVersion? GetLastestVersion(SemanticVersionChange semanticVersionChange, IEnumerable<NuGet.Versioning.SemanticVersion> previousVersions, NuGet.Versioning.SemanticVersion previousVersion)
        {
            return semanticVersionChange switch
            {
                SemanticVersionChange.Major => GetPatchVersion(previousVersions, previousVersion.With(major: previousVersion.Major + 1, minor: 0, patch: 0)),
                SemanticVersionChange.Minor => GetPatchVersion(previousVersions, previousVersion.With(minor: previousVersion.Minor + 1, patch: 0)),
                SemanticVersionChange.Patch => GetPatchVersion(previousVersions, previousVersion.With(patch: previousVersion.Patch + 1)),
                _ => previousVersion,
            };

            static NuGet.Versioning.SemanticVersion GetPatchVersion(
                IEnumerable<NuGet.Versioning.SemanticVersion> versions,
                NuGet.Versioning.SemanticVersion previousVersion)
            {
                // find the one with the same major/minor
                return versions.Where(version => version.Major == previousVersion.Major && version.Minor == previousVersion.Minor).Max();
            }
        }

        /// <summary>
        /// Gets the last version.
        /// </summary>
        /// <param name="semanticVersionChange">The semantic version change.</param>
        /// <param name="previousPackages">The previous versions.</param>
        /// <param name="previousPackage">The previous version.</param>
        /// <returns>The last version.</returns>
        public static NuGet.Packaging.Core.PackageIdentity? GetLastestPackage(SemanticVersionChange semanticVersionChange, IEnumerable<NuGet.Packaging.Core.PackageIdentity> previousPackages, NuGet.Packaging.Core.PackageIdentity previousPackage) =>
            semanticVersionChange switch
            {
                SemanticVersionChange.Major => GetPatchPackage(previousPackages, previousPackage.Version.With(major: previousPackage.Version.Major + 1, minor: 0, patch: 0)),
                SemanticVersionChange.Minor => GetPatchPackage(previousPackages, previousPackage.Version.With(minor: previousPackage.Version.Minor + 1, patch: 0)),
                SemanticVersionChange.Patch => GetPatchPackage(previousPackages, previousPackage.Version.With(patch: previousPackage.Version.Patch + 1)),
                _ => previousPackage,
            };

        /// <summary>
        /// Gets the patch version.
        /// </summary>
        /// <param name="packages">The packages.</param>
        /// <param name="previousVersion">The previous version.</param>
        /// <returns>The patch version.</returns>
        public static NuGet.Packaging.Core.PackageIdentity GetPatchPackage(
            IEnumerable<NuGet.Packaging.Core.PackageIdentity> packages,
            NuGet.Versioning.SemanticVersion previousVersion) => packages.Where(package => package.Version.Major == previousVersion.Major && package.Version.Minor == previousVersion.Minor).Max();

        /// <summary>
        /// Gets the current released version.
        /// </summary>
        /// <param name="previousVersions">The previous versions.</param>
        /// <returns>The current released version.</returns>
        public static NuGet.Versioning.SemanticVersion GetReleaseVersion(IEnumerable<NuGet.Versioning.SemanticVersion> previousVersions)
        {
            var previousVersion = previousVersions.Where(lastSemanticVersion => !lastSemanticVersion.IsPrerelease).Max() ?? previousVersions.Max();
            if (previousVersion is null)
            {
                throw new ArgumentException("Failed to find previous version", nameof(previousVersions));
            }

            return previousVersion;
        }

        /// <summary>
        /// Gets the current released package.
        /// </summary>
        /// <param name="previousPackages">The previous versions.</param>
        /// <returns>The current released version.</returns>
        public static NuGet.Packaging.Core.PackageIdentity GetReleasePackage(IEnumerable<NuGet.Packaging.Core.PackageIdentity> previousPackages)
        {
            var previousPackage = previousPackages
                .Where(info => info.HasVersion && !info.Version.IsPrerelease)
                .OrderByDescending(info => info.Version)
                .FirstOrDefault();

            previousPackage ??= previousPackages
                .Where(info => info.HasVersion && !info.Version.IsPrerelease)
                .OrderByDescending(info => info.Version)
                .FirstOrDefault();
            if (previousPackage is null)
            {
                throw new ArgumentException("Failed to find previous package", nameof(previousPackages));
            }

            return previousPackage;
        }

        private static IEnumerable<NuGet.Versioning.SemanticVersion> ToSemanticVersions(this IEnumerable<string> previousVersions)
        {
            return previousVersions is null
                ? Enumerable.Empty<NuGet.Versioning.SemanticVersion>()
                : WhereNotNull(previousVersions.Select(SafeParse)).ToArray();

            static NuGet.Versioning.SemanticVersion? SafeParse(string lastVersion)
            {
                if (NuGet.Versioning.SemanticVersion.TryParse(lastVersion, out var version))
                {
                    return version;
                }

                return default;
            }

            static IEnumerable<T> WhereNotNull<T>(IEnumerable<T?> source)
            {
                foreach (var item in source)
                {
                    if (item is not null)
                    {
                        yield return item;
                    }
                }
            }
        }
    }
}