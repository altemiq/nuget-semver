// -----------------------------------------------------------------------
// <copyright file="NuGetVersion.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning;

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

        return CalculateVersion(semanticVersionChange, previousSemanticVersions.ToList(), prerelease);
    }

    /// <summary>
    /// Calculates the version.
    /// </summary>
    /// <param name="semanticVersionChange">The semantic version change.</param>
    /// <param name="previousVersions">The previous versions.</param>
    /// <param name="prerelease">The prerelease tag.</param>
    /// <returns>The semantic version.</returns>
    public static NuGet.Versioning.SemanticVersion CalculateVersion(SemanticVersionChange semanticVersionChange, IList<NuGet.Versioning.SemanticVersion> previousVersions, string? prerelease) =>
        CalculateVersion(semanticVersionChange, previousVersions, GetReleaseVersion(previousVersions), prerelease);

    private static NuGet.Versioning.SemanticVersion CalculateVersion(SemanticVersionChange semanticVersionChange, IList<NuGet.Versioning.SemanticVersion> previousVersions, NuGet.Versioning.SemanticVersion? previousVersion, string? prerelease)
    {
        var nextVersion = previousVersion is null
            ? new NuGet.Versioning.SemanticVersion(0, 1, 0)
            : CalculateNextVersion(previousVersion, semanticVersionChange);
        var latestVersion = GetLatestVersion(previousVersions, nextVersion);
        if (latestVersion is null && previousVersions.Count > 0)
        {
            // this has no value in the released versions.
            return nextVersion.With(releaseLabel: prerelease);
        }

        return CalculateNextVersion(previousVersion ?? nextVersion, latestVersion, previousVersion is null && prerelease is null ? DefaultAlphaRelease : prerelease);
    }

    private static NuGet.Versioning.SemanticVersion CalculateNextVersion(NuGet.Versioning.SemanticVersion releaseVersion, NuGet.Versioning.SemanticVersion? patchVersion, string? prerelease)
    {
        if (patchVersion is null)
        {
            return releaseVersion.With(releaseLabel: prerelease ?? DefaultAlphaRelease);
        }

        return patchVersion.With(patch: patchVersion.Patch + 1, releaseLabel: prerelease ?? string.Empty);
    }

    private static NuGet.Versioning.SemanticVersion CalculateNextVersion(NuGet.Versioning.SemanticVersion previousVersion, SemanticVersionChange semanticVersionChange) => semanticVersionChange switch
    {
        SemanticVersionChange.Major => previousVersion.With(major: previousVersion.Major + 1, minor: 0, patch: 0),
        SemanticVersionChange.Minor => previousVersion.With(minor: previousVersion.Minor + 1, patch: 0),
        SemanticVersionChange.Patch => previousVersion.With(patch: previousVersion.Patch + 1),
        _ => previousVersion,
    };

    private static NuGet.Versioning.SemanticVersion? GetLatestVersion(IEnumerable<NuGet.Versioning.SemanticVersion> previousVersions, NuGet.Versioning.SemanticVersion nextVersion) => previousVersions.Where(previousVersion => previousVersion.Major == nextVersion.Major && previousVersion.Minor == nextVersion.Minor).Max();

    private static NuGet.Versioning.SemanticVersion? GetReleaseVersion(IEnumerable<NuGet.Versioning.SemanticVersion> previousVersions) => previousVersions.Where(lastSemanticVersion => !lastSemanticVersion.IsPrerelease).Max();

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