// -----------------------------------------------------------------------
// <copyright file="MSBuildApplication.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// The MSBuild application.
    /// </summary>
    public static class MSBuildApplication
    {
        private static readonly NuGet.Versioning.SemanticVersion Empty = new(0, 0, 0);

        /// <summary>
        /// The process project.
        /// </summary>
        /// <param name="projectDirectory">The project directory.</param>
        /// <param name="assemblyName">The assembly name.</param>
        /// <param name="projectPackageId">The project package ID.</param>
        /// <param name="targetExt">The target extension.</param>
        /// <param name="buildOutputTargetFolder">The build output target folder.</param>
        /// <param name="packageOutputPath">The package output path.</param>
        /// <param name="source">The NuGet source.</param>
        /// <param name="packageIds">The package ID.</param>
        /// <param name="packageIdRegex">The package ID regex.</param>
        /// <param name="packageIdReplace">The package ID replacement value.</param>
        /// <param name="previous">The previous version.</param>
        /// <param name="folderCommits">The commits for the folder.</param>
        /// <param name="headCommits">The commits for the head.</param>
        /// <param name="noCache">Set to <see langword="true"/> to disable using the machine cache as the first package source.</param>
        /// <param name="directDownload">Set to <see langword="true"/> to download directly without populating any caches with metadata or binaries.</param>
        /// <param name="getVersionSuffix">The function to get the version suffix.</param>
        /// <returns>The task.</returns>
        public static async Task<(NuGet.Versioning.SemanticVersion Version, System.Collections.Generic.IEnumerable<ProjectResult> Results, bool Published)> ProcessProject(
            string projectDirectory,
            string assemblyName,
            string projectPackageId,
            string targetExt,
            string buildOutputTargetFolder,
            string packageOutputPath,
            System.Collections.Generic.IEnumerable<string> source,
            System.Collections.Generic.IEnumerable<string> packageIds,
            System.Text.RegularExpressions.Regex? packageIdRegex,
            string? packageIdReplace,
            NuGet.Versioning.SemanticVersion? previous,
            System.Collections.Generic.IEnumerable<string> folderCommits,
            System.Collections.Generic.IEnumerable<string> headCommits,
            bool noCache,
            bool directDownload,
            Func<string?, string?> getVersionSuffix)
        {
            // install the NuGet package
            var projectPackageIds = new[] { projectPackageId }.Union(packageIds, StringComparer.Ordinal);
            if (packageIdRegex is not null)
            {
                projectPackageIds = projectPackageIds.Union(new[] { packageIdRegex.Replace(projectPackageId, packageIdReplace) }, StringComparer.Ordinal);
            }

            var packages = IsNullOrEmpty(previous)
                ? NuGetInstaller.GetPackagesAsync(projectPackageIds, source, root: projectDirectory)
                : CreateAsyncEnumerable(new NuGet.Packaging.Core.PackageIdentity(projectPackageId, new NuGet.Versioning.NuGetVersion(previous.Major, previous.Minor, previous.Patch, previous.ReleaseLabels, previous.Metadata)));

            var folderCommitsList = folderCommits.ToList();
            var headCommitsList = headCommits.ToList();
            if (folderCommitsList.Count > 0)
            {
                var commitPackage = await NuGetInstaller.GetPackageByCommit(folderCommitsList, headCommitsList, packages, source, root: projectDirectory).ConfigureAwait(false);
                if (commitPackage is not null)
                {
                    return (commitPackage.Version, Enumerable.Empty<ProjectResult>(), Published: true);
                }
            }

            var previousPackages = packages.GetLatestPackagesAsync();
            var installDir = await TryInstallPackagesAsync(packages.ToEnumerable(), projectDirectory).ConfigureAwait(false);
            var calculatedVersion = new NuGet.Versioning.SemanticVersion(0, 0, 0);
            var results = new System.Collections.Generic.List<ProjectResult>();

            if (installDir is null)
            {
                var previousVersion = await previousPackages.Select(package => package.Version).MaxAsync().ConfigureAwait(false);
                calculatedVersion = previousVersion is null
                    ? new NuGet.Versioning.SemanticVersion(1, 0, 0, getVersionSuffix(NuGetVersion.DefaultAlphaRelease)) // have this as being a 0.1.0 release
                    : new NuGet.Versioning.SemanticVersion(previousVersion.Major, previousVersion.Minor, previousVersion.Patch + 1, getVersionSuffix(previousVersion.Release));
            }
            else
            {
                var installedBuildOutputTargetFolder = TrimEndingDirectorySeparator(System.IO.Path.Combine(installDir, buildOutputTargetFolder));

                var previousPackagesArray = await previousPackages.ToArrayAsync().ConfigureAwait(false);

                // Get the package output path
                var fullPackageOutputPath = TrimEndingDirectorySeparator(System.IO.Path.Combine(projectDirectory, packageOutputPath.Replace('\\', System.IO.Path.DirectorySeparatorChar)));

                // check the frameworks
                var currentFrameworks = System.IO.Directory.EnumerateDirectories(fullPackageOutputPath).Select(System.IO.Path.GetFileName).ToArray();
                var previousFrameworks = System.IO.Directory.EnumerateDirectories(installedBuildOutputTargetFolder).Select(System.IO.Path.GetFileName).ToArray();
                var frameworks = currentFrameworks.Intersect(previousFrameworks, StringComparer.OrdinalIgnoreCase);
                if (previousFrameworks.Except(currentFrameworks, StringComparer.OrdinalIgnoreCase).Any())
                {
                    // we have removed frameworks, this is a breaking change
                    calculatedVersion = NuGetVersion.CalculateVersion(SemanticVersionChange.Major, previousPackagesArray.Select(package => package.Version), getVersionSuffix(default));
                }
                else if (currentFrameworks.Except(previousFrameworks, StringComparer.OrdinalIgnoreCase).Any())
                {
                    // we have added frameworks, this is a feature change
                    calculatedVersion = NuGetVersion.CalculateVersion(SemanticVersionChange.Minor, previousPackagesArray.Select(package => package.Version), getVersionSuffix(default));
                }

                var releasePackage = NuGetVersion.GetReleasePackage(previousPackagesArray);

                var searchPattern = assemblyName + targetExt;
#if NETSTANDARD2_1_OR_GREATER
                var searchOptions = new System.IO.EnumerationOptions { RecurseSubdirectories = false };
#else
                const System.IO.SearchOption searchOptions = System.IO.SearchOption.TopDirectoryOnly;
#endif
                foreach (var currentAssembly in frameworks.SelectMany(framework => System.IO.Directory.EnumerateFiles(System.IO.Path.Combine(fullPackageOutputPath, framework ?? string.Empty), searchPattern, searchOptions)))
                {
                    var oldAssembly = currentAssembly
#if NETSTANDARD2_1_OR_GREATER
                        .Replace(fullPackageOutputPath, installedBuildOutputTargetFolder, StringComparison.CurrentCulture);
#else
                        .Replace(fullPackageOutputPath, installedBuildOutputTargetFolder);
#endif
                    var differences = LibraryComparison.DetectChanges(oldAssembly, currentAssembly);
                    var resultsType = System.IO.File.Exists(oldAssembly)
                        ? LibraryComparison.GetMinimumAcceptableChange(differences)
                        : SemanticVersionChange.Major;
                    var lastPackage = NuGetVersion.GetLastestPackage(resultsType == SemanticVersionChange.None ? SemanticVersionChange.Patch : resultsType, previousPackagesArray, releasePackage);

                    var version = NuGetVersion.CalculateNextVersion(releasePackage.Version, lastPackage?.Version, getVersionSuffix(default));
                    if (version is not null && differences is not null)
                    {
                        results.Add(new(version, differences));
                    }

                    calculatedVersion = calculatedVersion.Max(version);
                }

                System.IO.Directory.Delete(installDir, recursive: true);
            }

            return (calculatedVersion, results, Published: false);

            bool IsNullOrEmpty([System.Diagnostics.CodeAnalysis.NotNullWhen(false)] NuGet.Versioning.SemanticVersion? version)
            {
                return version?.Equals(Empty) != false;
            }

            async Task<string?> TryInstallPackagesAsync(System.Collections.Generic.IEnumerable<NuGet.Packaging.Core.PackageIdentity> packages, string projectDirectory)
            {
                var previousVersion = IsNullOrEmpty(previous)
                    ? default
                    : previous;

                NuGet.Common.ILogger? logger = default;
                try
                {
                    return await NuGetInstaller.InstallAsync(packages, source, version: previousVersion, noCache: noCache, directDownload: directDownload, log: logger, root: projectDirectory).ConfigureAwait(false);
                }
                catch (NuGet.Protocol.PackageNotFoundProtocolException ex)
                {
                    logger?.LogError(ex.Message);
                }

                return default;
            }

            static string TrimEndingDirectorySeparator(string path)
            {
                return path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            }

            static async System.Collections.Generic.IAsyncEnumerable<T> CreateAsyncEnumerable<T>(T value)
            {
                await Task.CompletedTask.ConfigureAwait(false);
                yield return value;
            }
        }
    }
}