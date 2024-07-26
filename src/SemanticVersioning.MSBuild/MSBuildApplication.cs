// -----------------------------------------------------------------------
// <copyright file="MSBuildApplication.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altavec.SemanticVersioning;

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
                .Union(CreateEnumerable(packageIdRegex.Replace(projectPackageId, packageIdReplace ?? string.Empty)), StringComparer.Ordinal)
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

                if (referenceVersionsList.Count > 0)
                {
                    var packageDependencies = manifest.Metadata.DependencyGroups
                        .SelectMany(dg => dg.Packages)
                        .Distinct();

                    if (packageDependencies.All(packageDependency => referenceVersionsList.Find(referenceVersion => string.Equals(packageDependency.Id, referenceVersion.Id, StringComparison.OrdinalIgnoreCase)) is not PackageCommitIdentity referenceVersion || IsInBounds(referenceVersion, packageDependency)))
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
        }

        var previousPackages = packages.GetLatestPackages().ToArray();
        var installDir = await TryInstallPackagesAsync(packages, projectDirectory, logger).ConfigureAwait(false);
        var calculatedVersion = new NuGet.Versioning.SemanticVersion(0, 0, 0);
        var results = new List<ProjectResult>();

        if (installDir is null)
        {
            var previousVersion = previousPackages.Max(package => package.Version);
            calculatedVersion = (previousVersion, increment) switch
            {
                // increment patch
                (not null, SemanticVersionIncrement.Patch) => NuGetVersion.IncrementPatch(previousVersion, getVersionSuffix(previousVersion.Release)),

                // increment release label
                (not null, SemanticVersionIncrement.ReleaseLabel) => NuGetVersion.IncrementReleaseLabel(previousVersion, getVersionSuffix(previousVersion.Release)),

                // have this as being a 1.0.0 release
                _ => new NuGet.Versioning.SemanticVersion(1, 0, 0, getVersionSuffix(NuGetVersion.DefaultAlphaRelease)),
            };
        }
        else
        {
            var installedBuildOutputTargetFolder = TrimEndingDirectorySeparator(Path.Combine(installDir, buildOutputTargetFolder));

            // Get the package output path
            var fullPackageOutputPath = TrimEndingDirectorySeparator(Path.Combine(projectDirectory, outputPath.Replace('\\', Path.DirectorySeparatorChar)));

            // check the frameworks
            var currentFrameworks = Directory
                .EnumerateDirectories(fullPackageOutputPath)
                .Select(Path.GetFileName)
                .Select(NuGet.Frameworks.NuGetFramework.Parse)
                .ToArray();
            var previousFrameworks = Directory
                .EnumerateDirectories(installedBuildOutputTargetFolder)
                .Select(Path.GetFileName)
                .Select(NuGet.Frameworks.NuGetFramework.Parse)
                .ToArray();
            var comparer = new NuGetFrameworkComparer();
            var frameworks = currentFrameworks.Join(previousFrameworks, f => f, f => f, (current, previous) => (Current: current, Previous: previous), comparer);
            IList<NuGet.Versioning.SemanticVersion> previousVersions = previousPackages.Select<NuGet.Packaging.Core.PackageIdentity, NuGet.Versioning.SemanticVersion>(package => package.Version).ToList();
            if (previousFrameworks.Except(currentFrameworks, comparer).Any())
            {
                // we have removed frameworks, this is a breaking change
                calculatedVersion = NuGetVersion.CalculateVersion(SemanticVersionChange.Major, previousVersions, getVersionSuffix(default), increment);
            }
            else if (currentFrameworks.Except(previousFrameworks, comparer).Any())
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
            foreach (var (currentFramework, previousFramework) in frameworks)
            {
                foreach (var currentAssembly in Directory.EnumerateFiles(Path.Combine(fullPackageOutputPath, currentFramework.GetShortFolderName()), searchPattern, options))
                {
                    var previousAssembly = currentAssembly
#if NETSTANDARD2_1_OR_GREATER
                        .Replace(fullPackageOutputPath, installedBuildOutputTargetFolder, StringComparison.CurrentCulture)
                        .Replace(currentFramework.GetShortFolderName(), previousFramework.GetShortFolderName(), StringComparison.CurrentCulture);
#else
                        .Replace(fullPackageOutputPath, installedBuildOutputTargetFolder)
                        .Replace(currentFramework.GetShortFolderName(), previousFramework.GetShortFolderName());
#endif
                    var differences = LibraryComparison.DetectChanges(previousAssembly, currentAssembly);
                    var resultsType = File.Exists(previousAssembly)
                        ? LibraryComparison.GetMinimumAcceptableChange(differences)
                        : SemanticVersionChange.Major;
                    var version = NuGetVersion.CalculateVersion(resultsType == SemanticVersionChange.None ? SemanticVersionChange.Patch : resultsType, previousVersions, getVersionSuffix(default), increment);
                    if (version is not null && differences is not null)
                    {
                        results.Add(new(version, differences));
                    }

                    calculatedVersion = calculatedVersion.Max(version);
                }
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
            catch (KeyNotFoundException ex)
            {
                logger?.LogWarning($"SV0001|{ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                logger?.LogWarning($"SV0002|{ex.Message}");
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

    private sealed class NuGetFrameworkComparer : IEqualityComparer<NuGet.Frameworks.NuGetFramework>
    {
        private readonly NuGet.Frameworks.DefaultCompatibilityProvider provider = new();

        public bool Equals(NuGet.Frameworks.NuGetFramework? x, NuGet.Frameworks.NuGetFramework? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (x.Version == y.Version
                && StringComparer.OrdinalIgnoreCase.Equals(x.Framework, y.Framework)
                && StringComparer.OrdinalIgnoreCase.Equals(x.Profile, y.Profile)
                && !x.IsUnsupported)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(x.Platform, y.Platform)
                    && x.PlatformVersion == y.PlatformVersion)
                {
                    return true;
                }

                return this.provider.IsCompatible(x, y) || this.provider.IsCompatible(y, x);
            }

            return false;
        }

        public int GetHashCode(NuGet.Frameworks.NuGetFramework obj) => obj is null ? 0 : HashCode.Combine(obj.Framework, obj.Version, obj.Profile);
    }
}