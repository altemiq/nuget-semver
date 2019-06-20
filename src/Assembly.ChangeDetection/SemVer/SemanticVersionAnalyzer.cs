// -----------------------------------------------------------------------
// <copyright file="SemanticVersionAnalyzer.cs" company="GeomaticTechnologies">
// Copyright (c) GeomaticTechnologies. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.SemVer
{
    using Mondo.Assembly.ChangeDetection.Infrastructure;
    using Mondo.Assembly.ChangeDetection.Rules;

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
        /// <param name="prerelease">The pre-release label.</param>
        /// <param name="build">The build label.</param>
        /// <returns>The results.</returns>
        public static AnalysisResult Analyze(string previousAssembly, string currentAssembly, string lastVersionNumber, string prerelease = null, string build = null)
        {
            var previous = new FileQuery(previousAssembly);
            var current = new FileQuery(currentAssembly);

            var differences = DiffAssemblies.Execute(new[] { previous }, new[] { current });

            var breakingChanges = new BreakingChangeRule();
            var breakingChange = breakingChanges.Detect(differences);

            var featuresAddedChanges = new AddedFunctionalityRule();
            var featuresAdded = featuresAddedChanges.Detect(differences);

            var nextVersion = GetNextPatchVersion(lastVersionNumber);
            var previousVersion = System.IO.File.Exists(previousAssembly)
                ? GetProductVersion(previousAssembly)
                : nextVersion;

            NuGet.Versioning.SemanticVersion calculatedVersion;
            if (breakingChange)
            {
                calculatedVersion = previousVersion.Change(major: previousVersion.Major + 1, minor: 0, patch: 0, releaseLabel: prerelease ?? nextVersion.Release);
            }
            else if (featuresAdded)
            {
                calculatedVersion = previousVersion.Change(minor: previousVersion.Minor + 1, patch: 0, releaseLabel: prerelease ?? nextVersion.Release);
            }
            else
            {
                calculatedVersion = nextVersion.Change(releaseLabel: prerelease);
            }

            // see if the proposed version has the same major/minor
            if (calculatedVersion.Major == nextVersion.Major && calculatedVersion.Minor == nextVersion.Minor)
            {
                calculatedVersion = calculatedVersion.Change(patch: nextVersion.Patch);
            }

            if (build != null)
            {
                calculatedVersion = calculatedVersion.Change(metadata: build);
            }

            return new AnalysisResult
            {
                ResultsType = breakingChange ? ResultsType.Major : featuresAdded ? ResultsType.Minor : ResultsType.Patch,
                VersionNumber = calculatedVersion.ToString(),
            };
        }

        private static NuGet.Versioning.SemanticVersion GetNextPatchVersion(string lastVersionNumber)
        {
            var lastVersion = NuGet.Versioning.SemanticVersion.Parse(lastVersionNumber);
            return lastVersion.Change(patch: lastVersion.Patch + 1);
        }

        private static NuGet.Versioning.SemanticVersion GetProductVersion(string assembly)
        {
            var fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly);
            return NuGet.Versioning.SemanticVersion.Parse(fileVersionInfo.ProductVersion);
        }
    }
}
