// -----------------------------------------------------------------------
// <copyright file="NuGetVersion.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

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
    /// <param name="increment">The increment location.</param>
    /// <returns>The semantic version.</returns>
    public static NuGet.Versioning.SemanticVersion CalculateVersion(SemanticVersionChange semanticVersionChange, IEnumerable<string> previousVersions, string? prerelease, SemanticVersionIncrement increment) =>
        CalculateVersion(semanticVersionChange, previousVersions.ToSemanticVersions().ToList(), prerelease, increment);

    /// <summary>
    /// Calculates the version.
    /// </summary>
    /// <param name="semanticVersionChange">The semantic version change.</param>
    /// <param name="previousVersions">The previous versions.</param>
    /// <param name="prerelease">The prerelease tag.</param>
    /// <param name="increment">The increment location.</param>
    /// <returns>The semantic version.</returns>
    public static NuGet.Versioning.SemanticVersion CalculateVersion(SemanticVersionChange semanticVersionChange, IList<NuGet.Versioning.SemanticVersion> previousVersions, string? prerelease, SemanticVersionIncrement increment) =>
        CalculateVersion(semanticVersionChange, previousVersions, GetReleaseVersion(previousVersions), prerelease, increment);

    /// <summary>
    /// Increments the patch in a version.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="prerelease">The prerelease label.</param>
    /// <returns>The incremented version.</returns>
    public static NuGet.Versioning.SemanticVersion IncrementPatch(NuGet.Versioning.SemanticVersion version, string? prerelease) => version.With(patch: version.Patch + 1, releaseLabel: prerelease ?? string.Empty);

    /// <summary>
    /// Increments the release label in a version.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="prerelease">The prerelease label.</param>
    /// <returns>The incremented version.</returns>
    public static NuGet.Versioning.SemanticVersion IncrementReleaseLabel(NuGet.Versioning.SemanticVersion version, string? prerelease)
    {
        if (string.IsNullOrEmpty(prerelease))
        {
            // only increment the patch if the previous was a release as well
            return version switch
            {
                { IsPrerelease: false } v => v.With(patch: v.Patch + 1),
                var v => v.With(releaseLabel: string.Empty),
            };
        }

        if (version.IsPrerelease)
        {
            // calculate using the prerelease
            var releaseLabels = version.ReleaseLabels.ToList();

            var releaseCount = 0;
            if (releaseLabels.Count > 1
                && int.TryParse(releaseLabels[1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out releaseCount))
            {
                releaseCount++;
            }

            return version.With(releaseLabel: FormattableString.Invariant($"{prerelease}.{releaseCount}"));
        }

        // just add the prerelease label
        return version.With(patch: version.Patch + 1, releaseLabel: prerelease);
    }

    private static NuGet.Versioning.SemanticVersion CalculateVersion(SemanticVersionChange semanticVersionChange, IList<NuGet.Versioning.SemanticVersion> previousVersions, NuGet.Versioning.SemanticVersion? previousVersion, string? prerelease, SemanticVersionIncrement increment)
    {
        var nextVersion = previousVersion is null
            ? new(0, 1, 0)
            : CalculateNextVersion(previousVersion, semanticVersionChange);
        var latestVersion = GetLatestVersion(previousVersions, nextVersion);
        if (latestVersion is null && previousVersions.Count > 0)
        {
            // this has no value in the released versions.
            return nextVersion.With(releaseLabel: prerelease);
        }

        return CalculateNextVersion(previousVersion ?? nextVersion, latestVersion, previousVersion is null && prerelease is null ? DefaultAlphaRelease : prerelease, increment);
    }

    private static NuGet.Versioning.SemanticVersion CalculateNextVersion(NuGet.Versioning.SemanticVersion releaseVersion, NuGet.Versioning.SemanticVersion? patchVersion, string? prerelease, SemanticVersionIncrement increment)
    {
        if (patchVersion is null)
        {
            return releaseVersion.With(releaseLabel: prerelease ?? DefaultAlphaRelease);
        }

        return increment switch
        {
            SemanticVersionIncrement.Patch => IncrementPatch(patchVersion, prerelease),
            _ => IncrementReleaseLabel(patchVersion, prerelease),
        };
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with \"LINQ\" expressions", Justification = "This is the simplification")]
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