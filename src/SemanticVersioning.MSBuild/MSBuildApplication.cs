// -----------------------------------------------------------------------
// <copyright file="MSBuildApplication.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

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
    /// <param name="outputPath">The output path, without TFM.</param>
    /// <param name="source">The NuGet source.</param>
    /// <param name="packageIds">The package ID.</param>
    /// <param name="packageIdRegex">The package ID regex.</param>
    /// <param name="packageIdReplace">The package ID replacement value.</param>
    /// <param name="previous">The previous version.</param>
    /// <param name="folderCommits">The commits for the folder.</param>
    /// <param name="headCommits">The commits for the head.</param>
    /// <param name="referenceCommit">The reference commit.</param>
    /// <param name="referencePackageIds">The reference package IDs.</param>
    /// <param name="noCache">Set to <see langword="true"/> to disable using the machine cache as the first package source.</param>
    /// <param name="directDownload">Set to <see langword="true"/> to download directly without populating any caches with metadata or binaries.</param>
    /// <param name="increment">The increment location.</param>
    /// <param name="getVersionSuffix">The function to get the version suffix.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The task.</returns>
    public static async Task<(PackageCommitIdentity Identity, IEnumerable<ProjectResult> Results, bool Published)> ProcessProject(
        string projectDirectory,
        string assemblyName,
        string projectPackageId,
        string targetExt,
        string buildOutputTargetFolder,
        string outputPath,
        IEnumerable<string> source,
        IEnumerable<string> packageIds,
        System.Text.RegularExpressions.Regex? packageIdRegex,
        string? packageIdReplace,
        NuGet.Versioning.SemanticVersion? previous,
        IEnumerable<string> folderCommits,
        IEnumerable<string> headCommits,
        string? referenceCommit,
        IEnumerable<PackageCommitIdentity> referencePackageIds,
        bool noCache,
        bool directDownload,
        SemanticVersionIncrement increment,
        Func<string?, string?> getVersionSuffix,
        NuGet.Common.ILogger? logger = default)
    {
        // install the NuGet package
        var projectPackageIds = CreateEnumerable(projectPackageId)
            .Union(packageIds, StringComparer.Ordinal)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        if (packageIdRegex is not null)
        {
            projectPackageIds = projectPackageIds
                .Union(CreateEnumerable(packageIdRegex.Replace(projectPackageId, packageIdReplace)), StringComparer.Ordinal)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        var packages = IsNullOrEmpty(previous)
            ? await NuGetInstaller.GetPackagesAsync(projectPackageIds, source, log: logger, root: projectDirectory).ToArrayAsync().ConfigureAwait(false)
            : new[] { new NuGet.Packaging.Core.PackageIdentity(projectPackageId, new NuGet.Versioning.NuGetVersion(previous.Major, previous.Minor, previous.Patch, previous.ReleaseLabels, previous.Metadata)) };

        var folderCommitsList = folderCommits.ToList();
        var headCommitsList = headCommits.ToList();
        var referenceVersionsList = referencePackageIds.ToList();

        // if we have commits, and a reference does not have a newer commit
        if (folderCommitsList.Count > 0)
        {
            var (commitPackage, manifest) = await NuGetInstaller.GetPackageByCommit(
                folderCommitsList,
                headCommitsList,
                packages,
                source,
                log: logger,
                root: projectDirectory).ConfigureAwait(false);

            if (commitPackage is not null && manifest is not null)
            {
                var packageCommitId = new PackageCommitIdentity(commitPackage.Id, commitPackage.Version, manifest.Metadata.Repository.Commit);
                if (referenceCommit is not null && headCommitsList.Contains(referenceCommit, StringComparer.Ordinal))
                {
                    // get the referenced package
                    var referencedPackage = referenceVersionsList.Find(r => EqualCommits(r.Commit, referenceCommit));
                    if (referencedPackage is not null
                        && manifest.Metadata.DependencyGroups
                            .SelectMany(dg => dg.Packages)
                            .Where(dp => string.Equals(dp.Id, referencedPackage.Id, StringComparison.OrdinalIgnoreCase))
                            .All(d => IsInBounds(referencedPackage, d)))
                    {
                        return (packageCommitId, Enumerable.Empty<ProjectResult>(), Published: true);
                    }
                }
                else
                {
                    return (packageCommitId, Enumerable.Empty<ProjectResult>(), Published: true);
                }
            }

            static bool IsInBounds(NuGet.Packaging.Core.PackageIdentity reference, NuGet.Packaging.Core.PackageDependency packageDependency)
            {
                return reference.Version == packageDependency.VersionRange.MinVersion;
            }

            static bool EqualCommits(string? package, string reference)
            {
                if (package is null)
                {
                    return false;
                }

                return string.Compare(package, 0, reference, 0, Math.Min(package.Length, reference.Length), StringComparison.OrdinalIgnoreCase) == 0;
            }
        }

        var previousPackages = packages.GetLatestPackages().ToArray();
        var installDir = await TryInstallPackagesAsync(packages, projectDirectory, logger).ConfigureAwait(false);
        var calculatedVersion = new NuGet.Versioning.SemanticVersion(0, 0, 0);
        var results = new List<ProjectResult>();

        if (installDir is null)
        {
            var previousVersion = previousPackages.Max(package => package.Version);
            calculatedVersion = previousVersion is null
                ? new NuGet.Versioning.SemanticVersion(1, 0, 0, getVersionSuffix(NuGetVersion.DefaultAlphaRelease)) // have this as being a 0.1.0 release
                : new NuGet.Versioning.SemanticVersion(previousVersion.Major, previousVersion.Minor, previousVersion.Patch + 1, getVersionSuffix(previousVersion.Release));
        }
        else
        {
            var installedBuildOutputTargetFolder = TrimEndingDirectorySeparator(Path.Combine(installDir, buildOutputTargetFolder));

            // Get the package output path
            var fullPackageOutputPath = TrimEndingDirectorySeparator(Path.Combine(projectDirectory, outputPath.Replace('\\', Path.DirectorySeparatorChar)));

            // check the frameworks
            var currentFrameworks = Directory.EnumerateDirectories(fullPackageOutputPath).Select(Path.GetFileName).ToArray();
            var previousFrameworks = Directory.EnumerateDirectories(installedBuildOutputTargetFolder).Select(Path.GetFileName).ToArray();
            var frameworks = currentFrameworks.Intersect(previousFrameworks, StringComparer.OrdinalIgnoreCase);
            IList<NuGet.Versioning.SemanticVersion> previousVersions = previousPackages.Select<NuGet.Packaging.Core.PackageIdentity, NuGet.Versioning.SemanticVersion>(package => package.Version).ToList();
            if (previousFrameworks.Except(currentFrameworks, StringComparer.OrdinalIgnoreCase).Any())
            {
                // we have removed frameworks, this is a breaking change
                calculatedVersion = NuGetVersion.CalculateVersion(SemanticVersionChange.Major, previousVersions, getVersionSuffix(default), increment);
            }
            else if (currentFrameworks.Except(previousFrameworks, StringComparer.OrdinalIgnoreCase).Any())
            {
                // we have added frameworks, this is a feature change
                calculatedVersion = NuGetVersion.CalculateVersion(SemanticVersionChange.Minor, previousVersions, getVersionSuffix(default), increment);
            }

            var searchPattern = assemblyName + targetExt;
#if NETSTANDARD2_1_OR_GREATER
            var options = new EnumerationOptions { RecurseSubdirectories = false };
#else
            const SearchOption options = SearchOption.TopDirectoryOnly;
#endif
            foreach (var currentAssembly in frameworks.SelectMany(framework => Directory.EnumerateFiles(Path.Combine(fullPackageOutputPath, framework ?? string.Empty), searchPattern, options)))
            {
                var oldAssembly = currentAssembly
#if NETSTANDARD2_1_OR_GREATER
                    .Replace(fullPackageOutputPath, installedBuildOutputTargetFolder, StringComparison.CurrentCulture);
#else
                    .Replace(fullPackageOutputPath, installedBuildOutputTargetFolder);
#endif
                var differences = LibraryComparison.DetectChanges(oldAssembly, currentAssembly);
                var resultsType = File.Exists(oldAssembly)
                    ? LibraryComparison.GetMinimumAcceptableChange(differences)
                    : SemanticVersionChange.Major;
                var version = NuGetVersion.CalculateVersion(resultsType == SemanticVersionChange.None ? SemanticVersionChange.Patch : resultsType, previousVersions, getVersionSuffix(default), increment);
                if (version is not null && differences is not null)
                {
                    results.Add(new(version, differences));
                }

                calculatedVersion = calculatedVersion.Max(version);
            }

            Directory.Delete(installDir, recursive: true);
        }

        var nugetVersion = new NuGet.Versioning.NuGetVersion(
            calculatedVersion.Major,
            calculatedVersion.Minor,
            calculatedVersion.Patch,
            calculatedVersion.Release,
            calculatedVersion.Metadata);
        return (
            new PackageCommitIdentity(projectPackageId, nugetVersion, folderCommitsList.FirstOrDefault()),
            results,
            Published: false);

        bool IsNullOrEmpty([System.Diagnostics.CodeAnalysis.NotNullWhen(false)] NuGet.Versioning.SemanticVersion? version)
        {
            return version?.Equals(Empty) != false;
        }

        async Task<string?> TryInstallPackagesAsync(IEnumerable<NuGet.Packaging.Core.PackageIdentity> packages, string projectDirectory, NuGet.Common.ILogger? logger = default)
        {
            var previousVersion = IsNullOrEmpty(previous)
                ? default
                : previous;

            try
            {
                return await NuGetInstaller.InstallAsync(packages, source, version: previousVersion, noCache: noCache, directDownload: directDownload, log: logger, root: projectDirectory).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                logger?.LogWarning(ex.Message);
            }

            return default;
        }

        static string TrimEndingDirectorySeparator(string path)
        {
            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        static IEnumerable<T> CreateEnumerable<T>(T value)
        {
            yield return value;
        }
    }
}