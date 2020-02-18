// -----------------------------------------------------------------------
// <copyright file="SemanticVersionAnalyzer.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.SemVer
{
    using System.Linq;
    using Mondo.Assembly.ChangeDetection.Infrastructure;
    using Mondo.Assembly.ChangeDetection.Rules;

    /// <summary>
    /// The semantic version number.
    /// </summary>
    public static class SemanticVersionAnalyzer
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
        /// Analyses the results.
        /// </summary>
        /// <param name="previousAssembly">The previous assembly.</param>
        /// <param name="currentAssembly">The current assembly.</param>
        /// <param name="lastVersions">The last version numbers for each major.minor grouping.</param>
        /// <param name="prerelease">The pre-release label.</param>
        /// <param name="build">The build label.</param>
        /// <returns>The results.</returns>
        public static AnalysisResult Analyze(string previousAssembly, string currentAssembly, System.Collections.Generic.IEnumerable<string> lastVersions, string? prerelease = default, string? build = default)
        {
            var previous = new FileQuery(previousAssembly);
            var current = new FileQuery(currentAssembly);

            var differences = DiffAssemblies.Execute(new[] { previous }, new[] { current });

            var breakingChanges = new BreakingChangeRule();
            var breakingChange = breakingChanges.Detect(differences);

            var featuresAddedChanges = new AddedFunctionalityRule();
            var featuresAdded = featuresAddedChanges.Detect(differences);

            var lastSemanticVersions = lastVersions is null
                ? Enumerable.Empty<NuGet.Versioning.SemanticVersion>()
                : lastVersions.Select(lastVersion => NuGet.Versioning.SemanticVersion.TryParse(lastVersion, out var version) ? version : null).Where(version => version != null).Cast<NuGet.Versioning.SemanticVersion>().ToArray();

            NuGet.Versioning.SemanticVersion previousVersion;
            if (System.IO.File.Exists(previousAssembly))
            {
                previousVersion = GetProductVersion(previousAssembly);
            }
            else
            {
                breakingChange = true;
                previousVersion = lastSemanticVersions.Where(lastSemanticVersion => !lastSemanticVersion.IsPrerelease).Max() ?? lastSemanticVersions.Max();
            }

            NuGet.Versioning.SemanticVersion calculatedVersion;
            if (breakingChange)
            {
                calculatedVersion = GetNextPatchVersion(lastSemanticVersions, previousVersion.Change(major: previousVersion.Major + 1, minor: 0, patch: 0), prerelease);
            }
            else if (featuresAdded)
            {
                calculatedVersion = GetNextPatchVersion(lastSemanticVersions, previousVersion.Change(minor: previousVersion.Minor + 1, patch: 0), prerelease);
            }
            else
            {
                calculatedVersion = GetNextPatchVersion(lastSemanticVersions, previousVersion, prerelease);
            }

            if (build != null)
            {
                calculatedVersion = calculatedVersion.Change(metadata: build);
            }

            return new AnalysisResult(
                calculatedVersion.ToString(),
                breakingChange ? ResultsType.Major : featuresAdded ? ResultsType.Minor : ResultsType.Patch,
                differences);
        }

        /// <summary>
        /// Creates a breaking change.
        /// </summary>
        /// <param name="lastVersions">The last version numbers for each major.minor grouping.</param>
        /// <param name="prerelease">The pre-release label.</param>
        /// <returns>The breaking change version.</returns>
        public static NuGet.Versioning.SemanticVersion CreateBreakingChange(System.Collections.Generic.IEnumerable<string> lastVersions, string? prerelease)
        {
            var lastSemanticVersions = lastVersions is null
                ? Enumerable.Empty<NuGet.Versioning.SemanticVersion>()
                : lastVersions.Select(lastVersion => NuGet.Versioning.SemanticVersion.TryParse(lastVersion, out var version) ? version : null).Where(version => version != null).Cast<NuGet.Versioning.SemanticVersion>().ToArray();

            var previousVersion = lastSemanticVersions.Where(lastSemanticVersion => !lastSemanticVersion.IsPrerelease).Max() ?? lastSemanticVersions.Max();
            return GetNextPatchVersion(lastSemanticVersions, previousVersion.Change(major: previousVersion.Major + 1, minor: 0, patch: 0), prerelease);
        }

        /// <summary>
        /// Creates a feature change.
        /// </summary>
        /// <param name="lastVersions">The last version numbers for each major.minor grouping.</param>
        /// <param name="prerelease">The pre-release label.</param>
        /// <returns>The breaking change version.</returns>
        public static NuGet.Versioning.SemanticVersion CreateFeatureChange(System.Collections.Generic.IEnumerable<string> lastVersions, string? prerelease)
        {
            var lastSemanticVersions = lastVersions is null
                ? Enumerable.Empty<NuGet.Versioning.SemanticVersion>()
                : lastVersions.Select(lastVersion => NuGet.Versioning.SemanticVersion.TryParse(lastVersion, out var version) ? version : null).Where(version => version != null).Cast<NuGet.Versioning.SemanticVersion>().ToArray();

            var previousVersion = lastSemanticVersions.Where(lastSemanticVersion => !lastSemanticVersion.IsPrerelease).Max() ?? lastSemanticVersions.Max();
            return GetNextPatchVersion(lastSemanticVersions, previousVersion.Change(minor: previousVersion.Minor + 1, patch: 0), prerelease);
        }

        private static NuGet.Versioning.SemanticVersion GetNextPatchVersion(
            System.Collections.Generic.IEnumerable<NuGet.Versioning.SemanticVersion> versions,
            NuGet.Versioning.SemanticVersion previousVersion,
            string? prerelease)
        {
            // find the one with the same major/minor
            var patchedVersion = versions.Where(version => version.Major == previousVersion.Major && version.Minor == previousVersion.Minor).Max();
            return patchedVersion is null
                ? previousVersion.Change(releaseLabel: prerelease ?? DefaultAlphaRelease)
                : patchedVersion.Change(patch: patchedVersion.Patch + 1, releaseLabel: prerelease ?? patchedVersion.Release);
        }

        private static NuGet.Versioning.SemanticVersion GetProductVersion(string assembly) => NuGet.Versioning.SemanticVersion.Parse(System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly).ProductVersion);
    }
}
