// -----------------------------------------------------------------------
// <copyright file="SemanticVersionExtensions.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning
{
    /// <summary>
    /// The <see cref="NuGet.Versioning.SemanticVersion"/> extensions.
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
        public static NuGet.Versioning.SemanticVersion With(this NuGet.Versioning.SemanticVersion version, int? major = default, int? minor = default, int? patch = default, string? releaseLabel = default, string? metadata = default) => new(major ?? version.Major, minor ?? version.Minor, patch ?? version.Patch, releaseLabel ?? version.Release, metadata ?? version.Metadata);

        /// <summary>
        /// Gets the feature version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>The feature version.</returns>
        public static int GetFeature(this NuGet.Versioning.SemanticVersion version) => version.Patch / 100;

        /// <summary>
        /// Gets the patch version.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>The patch version.</returns>
        public static int GetPatch(this NuGet.Versioning.SemanticVersion version) => version.Patch % 100;

        /// <summary>
        /// Gets a value as to whether the specified version matches the policy.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="requested">The requested version.</param>
        /// <param name="allowPrerelease">Set to <see langword="true"/> to allow prerelease versions.</param>
        /// <param name="rollForwardPolicy">The roll-forward policy.</param>
        /// <returns><see langword="true"/> if <paramref name="version"/> matches the policy.</returns>
        public static bool MatchesPolicy(this NuGet.Versioning.SemanticVersion? version, NuGet.Versioning.SemanticVersion? requested, bool allowPrerelease, RollForwardPolicy rollForwardPolicy)
        {
            var versionIsNull = version is null;
            var versionIsPrerelease = version?.IsPrerelease == true;
            if (versionIsNull
                || (!allowPrerelease && versionIsPrerelease)
                || rollForwardPolicy == RollForwardPolicy.Disable)
            {
                return false;
            }

            if (requested is null)
            {
                return true;
            }

            return rollForwardPolicy switch
            {
                RollForwardPolicy.Patch or RollForwardPolicy.LatestPatch => TestPatchVersion(),
                RollForwardPolicy.Feature or RollForwardPolicy.LatestFeature => TestFeatureVersion(),
                RollForwardPolicy.Minor or RollForwardPolicy.LatestMinor => TestMinorVersion(),
                _ => version >= requested,
            };

            bool TestPatchVersion()
            {
                return version is not null
                    && requested is not null
                    && version.Major == requested.Major
                    && version.Minor == requested.Minor
                    && version.GetFeature() == requested.GetFeature();
            }

            bool TestFeatureVersion()
            {
                return version is not null
                    && requested is not null
                    && version.Major == requested.Major
                    && version.Minor == requested.Minor;
            }

            bool TestMinorVersion()
            {
                return version is not null
                    && requested is not null
                    && version.Major == requested.Major;
            }
        }

        /// <summary>
        /// Tests to see if the current version is a better match.
        /// </summary>
        /// <param name="current">The current version.</param>
        /// <param name="previous">The previous version.</param>
        /// <param name="rollForwardPolicy">The roll-forward policy.</param>
        /// <returns><see langword="true"/> if <paramref name="current"/> is a better match.</returns>
        public static bool IsBetterMatch(this NuGet.Versioning.SemanticVersion current, NuGet.Versioning.SemanticVersion? previous, RollForwardPolicy rollForwardPolicy)
        {
            // If no previous match, then the current one is better
            if (previous is null)
            {
                return true;
            }

            // Use the later of the two if there is no requested version, the policy requires it,
            // or if everything is equal up to the feature level (latest patch always wins)
            if (IsPolicyUseLatest(rollForwardPolicy) ||
                (current.Major == previous.Major &&
                 current.Minor == previous.Minor &&
                 current.GetPatch() == previous.GetPatch()))
            {
                // Accept the later of the versions
                // This will also handle stable and prerelease comparisons
                return current > previous;
            }

            return current < previous;

            static bool IsPolicyUseLatest(RollForwardPolicy rollFowardPolicy)
            {
                return rollFowardPolicy switch
                {
                    RollForwardPolicy.LatestFeature or RollForwardPolicy.LatestMajor or RollForwardPolicy.LatestMinor or RollForwardPolicy.LatestPatch => true,
                    _ => false,
                };
            }
        }
    }
}