// -----------------------------------------------------------------------
// <copyright file="SemanticVersionAnalyzer.cs" company="GeomaticTechnologies">
// Copyright (c) GeomaticTechnologies. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.SemVer
{
    using Mondo.Assembly.ChangeDetection.Infrastructure;
    using Mondo.Assembly.ChangeDetection.Rules;
    using Semver;

    /// <summary>
    /// The semantic version number.
    /// </summary>
    public static class SemanticVersionAnalyzer
    {
        /// <summary>
        /// Analyses the results.
        /// </summary>
        /// <param name="previousAssembly">The previous assembly.</param>
        /// <param name="currentAssembly">The current assembly.</param>
        /// <param name="lastVersionNumber">The last version number.</param>
        /// <param name="build">The build label.</param>
        /// <returns>The results.</returns>
        public static AnalysisResult Analyze(string previousAssembly, string currentAssembly, string lastVersionNumber, string build = null)
        {
            var previous = new FileQuery(previousAssembly);
            var current = new FileQuery(currentAssembly);

            var differences = DiffAssemblies.Execute(new[] { previous }, new[] { current });

            var breakingChanges = new BreakingChangeRule();
            var breakingChange = breakingChanges.Detect(differences);

            var featuresAddedChanges = new AddedFunctionalityRule();
            var featuresAdded = featuresAddedChanges.Detect(differences);

            var previousVersion = GetProductVersion(previousAssembly);
            var nextVersion = GetNextPatchVersion(lastVersionNumber);

            SemVersion calculatedVersion;
            if (breakingChange)
            {
                calculatedVersion = previousVersion.Change(major: previousVersion.Major + 1, minor: 0, patch: 0, prerelease: nextVersion.Prerelease);
            }
            else if (featuresAdded)
            {
                calculatedVersion = previousVersion.Change(minor: previousVersion.Minor + 1, patch: 0, prerelease: nextVersion.Prerelease);
            }
            else
            {
                calculatedVersion = nextVersion;
            }

            // see if the proposed version has the same major/minor
            if (calculatedVersion.Major == nextVersion.Major && calculatedVersion.Minor == nextVersion.Minor)
            {
                calculatedVersion = calculatedVersion.Change(patch: nextVersion.Patch);
            }

            if (build != null)
            {
                calculatedVersion = calculatedVersion.Change(build: build);
            }

            return new AnalysisResult
            {
                ResultsType = breakingChange ? ResultsType.Major : featuresAdded ? ResultsType.Minor : ResultsType.Patch,
                VersionNumber = calculatedVersion.ToString(),
            };
        }

        private static SemVersion GetNextPatchVersion(string lastVersionNumber)
        {
            var lastVersion = SemVersion.Parse(lastVersionNumber);
            return lastVersion.Change(patch: lastVersion.Patch + 1);
        }

        private static SemVersion GetProductVersion(string assembly)
        {
            var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly);
            return SemVersion.Parse(fileVersionInfo.ProductVersion);
        }
    }
}
