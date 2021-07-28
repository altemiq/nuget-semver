// -----------------------------------------------------------------------
// <copyright file="LibraryComparison.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Endjin.ApiChange.Api.Diff;
    using Endjin.ApiChange.Api.Introspection;
    using Endjin.ApiChange.Api.Query;
    using Mono.Cecil;

    /// <summary>
    /// The library comparison.
    /// </summary>
    public static class LibraryComparison
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
        public static (NuGet.Versioning.SemanticVersion? Version, SemanticVersionChange Change, AssemblyDiffCollection Differences) Analyze(string previousAssembly, string currentAssembly, IEnumerable<string> lastVersions, string? prerelease = default, string? build = default)
        {
            var differences = DetectChanges(previousAssembly, currentAssembly);
            var resultsType = System.IO.File.Exists(previousAssembly)
                ? GetMinimumAcceptableChange(differences)
                : SemanticVersionChange.Major;
            var calculatedVersion = CalculateVersion(resultsType == SemanticVersionChange.None ? SemanticVersionChange.Patch : resultsType, lastVersions, prerelease);
            if (build is not null)
            {
                calculatedVersion = calculatedVersion.With(metadata: build);
            }

            return (calculatedVersion, resultsType, differences);
        }

        /// <summary>
        /// Reports the changes from one assembly to another.
        /// </summary>
        /// <param name="pathToOldAssembly">The full file path of the old assembly.</param>
        /// <param name="pathToNewAssembly">The full file path of the new assembly.</param>
        /// <returns>
        /// A <see cref="AssemblyDiffCollection"/> describing the changes.
        /// </returns>
        public static AssemblyDiffCollection DetectChanges(string pathToOldAssembly, string pathToNewAssembly)
        {
            var oldExists = System.IO.File.Exists(pathToOldAssembly);
            var newExists = System.IO.File.Exists(pathToNewAssembly);
            return (oldExists, newExists) switch
            {
                (true, true) => DetectChangesCore(),
                (true, false) => GetAll(pathToOldAssembly, isAdded: false),
                (false, true) => GetAll(pathToNewAssembly, isAdded: true),
                (false, false) => new AssemblyDiffCollection(),
            };

            static AssemblyDiffCollection GetAll(string path, bool isAdded)
            {
                using var assembly = AssemblyLoader.LoadCecilAssembly(path);
                var difference = new AssemblyDiffCollection();

                var typeQuery = new TypeQuery(TypeQueryMode.ApiRelevant);
                var results = typeQuery.GetTypes(assembly)
                    .Select(type => new DiffResult<TypeDefinition>(type, new DiffOperation(isAdded)));
                difference.AddedRemovedTypes.AddRange(results);

                return difference;
            }

            AssemblyDiffCollection DetectChangesCore()
            {
                using var oldAssembly = AssemblyLoader.LoadCecilAssembly(pathToOldAssembly);
                using var newAssembly = AssemblyLoader.LoadCecilAssembly(pathToNewAssembly);
                var ad = new AssemblyDiffer(oldAssembly, newAssembly);

                var qa = new QueryAggregator();
                qa.TypeQueries.Add(new TypeQuery(TypeQueryMode.ApiRelevant));

                qa.MethodQueries.Add(MethodQuery.PublicMethods);
                qa.MethodQueries.Add(MethodQuery.ProtectedMethods);

                qa.FieldQueries.Add(FieldQuery.PublicFields);
                qa.FieldQueries.Add(FieldQuery.ProtectedFields);

                qa.EventQueries.Add(EventQuery.PublicEvents);
                qa.EventQueries.Add(EventQuery.ProtectedEvents);

                return ad.GenerateTypeDiff(qa);
            }
        }

        /// <summary>
        /// Calculate the minimum acceptable version number change according to Semantic Versioning
        /// rules given some changes that have been made to a library.
        /// </summary>
        /// <param name="libraryChanges">The changes made to the library.</param>
        /// <returns>The minimum version number change acceptable in Semantic Versioning.</returns>
        public static SemanticVersionChange GetMinimumAcceptableChange(AssemblyDiffCollection libraryChanges)
        {
            bool typesRemoved = libraryChanges.AddedRemovedTypes.Any(type => type.Operation.IsRemoved);
            bool constructorsRemoved = libraryChanges.ChangedTypes.Any(td => FromDiff(td.Methods, added: false).Any(md => md.IsConstructor));
            bool methodsRemoved = libraryChanges.ChangedTypes.Any(td => FromDiff(td.Methods, added: false).Any(md => !md.IsSpecialName));
            bool propertiesRemoved = libraryChanges.ChangedTypes.Any(td => GetProperties(td, added: false).Any());
            bool fieldsRemoved = libraryChanges.ChangedTypes.Any(td => FromDiff(td.Fields, added: false).Any());

            if (typesRemoved || constructorsRemoved || methodsRemoved || propertiesRemoved || fieldsRemoved)
            {
                return SemanticVersionChange.Major;
            }

            bool typesAdded = libraryChanges.AddedRemovedTypes.Any(type => type.Operation.IsAdded);
            bool constructorsAdded = libraryChanges.ChangedTypes.Any(td => FromDiff(td.Methods, added: true).Any(md => md.IsConstructor));
            bool methodsAdded = libraryChanges.ChangedTypes.Any(td => FromDiff(td.Methods, added: true).Any(md => !md.IsSpecialName));
            bool propertiesAdded = libraryChanges.ChangedTypes.Any(td => GetProperties(td, added: true).Any());
            bool fieldsAdded = libraryChanges.ChangedTypes.Any(td => FromDiff(td.Fields, added: true).Any());

            return typesAdded || constructorsAdded || methodsAdded || propertiesAdded || fieldsAdded
                ? SemanticVersionChange.Minor
                : SemanticVersionChange.None;

            static IEnumerable<T> FromDiff<T>(DiffCollection<T> source, bool added)
            {
                return source
                    .Where(dr => added ? dr.Operation.IsAdded : dr.Operation.IsRemoved)
                    .Select(dr => dr.ObjectV1);
            }

            static IEnumerable<PropertyDefinition> GetProperties(TypeDiff td, bool added)
            {
                var source = added ? td.TypeV2 : td.TypeV1;
                return td
                    .Methods
                    .Where(md => (md.ObjectV1.IsGetter || md.ObjectV1.IsSetter) &&
                        (added ? md.Operation.IsAdded : md.Operation.IsRemoved))
                    .GroupBy(md => source.Properties.Single(p => p.GetMethod == md.ObjectV1 || p.SetMethod == md.ObjectV1))
                    .Select(g => g.Key);
            }
        }

        /// <summary>
        /// Calculates the version.
        /// </summary>
        /// <param name="semanticVersionChange">The semantic version change.</param>
        /// <param name="previousVersions">The previous versions.</param>
        /// <param name="prerelease">The prerelease tag.</param>
        /// <returns>The semantic version.</returns>
        public static NuGet.Versioning.SemanticVersion CalculateVersion(SemanticVersionChange semanticVersionChange, IEnumerable<string> previousVersions, string? prerelease)
        {
            var previousSemanticVersions = previousVersions is null
                ? Enumerable.Empty<NuGet.Versioning.SemanticVersion>()
                : WhereNotNull(previousVersions.Select(SafeParse)).ToArray();

            return CalculateVersion(semanticVersionChange, previousSemanticVersions, prerelease);

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

        /// <summary>
        /// Calculates the version.
        /// </summary>
        /// <param name="semanticVersionChange">The semantic version change.</param>
        /// <param name="previousVersions">The previous versions.</param>
        /// <param name="prerelease">The prerelease tag.</param>
        /// <returns>The semantic version.</returns>
        public static NuGet.Versioning.SemanticVersion CalculateVersion(SemanticVersionChange semanticVersionChange, IEnumerable<NuGet.Versioning.SemanticVersion> previousVersions, string? prerelease)
        {
            var previousVersion = previousVersions.Where(lastSemanticVersion => !lastSemanticVersion.IsPrerelease).Max() ?? previousVersions.Max();
            if (previousVersion is null)
            {
                throw new ArgumentException("Failed to find previous version", nameof(previousVersions));
            }

            return CalculateVersion(semanticVersionChange, previousVersions, previousVersion, prerelease);
        }

        /// <summary>
        /// Calculates the version.
        /// </summary>
        /// <param name="semanticVersionChange">The semantic version change.</param>
        /// <param name="previousVersions">The previous versions.</param>
        /// <param name="previousVersion">The previous version.</param>
        /// <param name="prerelease">The prerelease tag.</param>
        /// <returns>The semantic version.</returns>
        public static NuGet.Versioning.SemanticVersion CalculateVersion(SemanticVersionChange semanticVersionChange, IEnumerable<NuGet.Versioning.SemanticVersion> previousVersions, NuGet.Versioning.SemanticVersion previousVersion, string? prerelease)
        {
            return semanticVersionChange switch
            {
                SemanticVersionChange.Major => GetNextPatchVersion(previousVersions, previousVersion.With(major: previousVersion.Major + 1, minor: 0, patch: 0), prerelease),
                SemanticVersionChange.Minor => GetNextPatchVersion(previousVersions, previousVersion.With(minor: previousVersion.Minor + 1, patch: 0), prerelease),
                SemanticVersionChange.Patch => GetNextPatchVersion(previousVersions, previousVersion.With(patch: previousVersion.Patch + 1), prerelease),
                _ => previousVersion,
            };

            static NuGet.Versioning.SemanticVersion GetNextPatchVersion(
                IEnumerable<NuGet.Versioning.SemanticVersion> versions,
                NuGet.Versioning.SemanticVersion previousVersion,
                string? prerelease)
            {
                // find the one with the same major/minor
                var patchedVersion = versions.Where(version => version.Major == previousVersion.Major && version.Minor == previousVersion.Minor).Max();
                return patchedVersion is null
                    ? previousVersion.With(releaseLabel: prerelease ?? "alpha")
                    : patchedVersion.With(patch: patchedVersion.Patch + 1, releaseLabel: prerelease ?? patchedVersion.Release);
            }
        }
    }
}