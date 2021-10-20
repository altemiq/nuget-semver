// -----------------------------------------------------------------------
// <copyright file="NuGetInstaller.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

/// <summary>
/// The NuGet installer.
/// </summary>
public static class NuGetInstaller
{
    /// <summary>
    /// Installs the specified package.
    /// </summary>
    /// <param name="packageNames">The package names.</param>
    /// <param name="sources">The sources.</param>
    /// <param name="version">The specific version.</param>
    /// <param name="noCache">Set to true to ignore the cache.</param>
    /// <param name="directDownload">Set to true to directly download.</param>
    /// <param name="log">The log.</param>
    /// <param name="root">The root of the settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public static async Task<string> InstallAsync(
        IEnumerable<string> packageNames,
        IEnumerable<string>? sources = default,
        NuGet.Versioning.SemanticVersion? version = default,
        bool noCache = default,
        bool directDownload = default,
        NuGet.Common.ILogger? log = default,
        string? root = default,
        System.Threading.CancellationToken cancellationToken = default)
    {
        var enumerableSources = sources ?? Enumerable.Empty<string>();
        var settings = Settings.LoadDefaultSettings(root);
        SourcePackageDependencyInfo? latest = default;
        foreach (var packageName in packageNames)
        {
            var package = version is null
                ? await GetLatestPackage(enumerableSources, packageName, includePrerelease: false, log ?? NuGet.Common.NullLogger.Instance, settings, cancellationToken).ConfigureAwait(false)
                : await GetPackage(enumerableSources, packageName, version, log ?? NuGet.Common.NullLogger.Instance, settings, cancellationToken).ConfigureAwait(false);
            if (package is null)
            {
                continue;
            }

            latest ??= package;
            if (package.Version > latest.Version)
            {
                latest = package;
            }
        }

        if (latest != default && await IsPackageInSource(latest, GetRepositories(settings, enumerableSources), log ?? NuGet.Common.NullLogger.Instance, cancellationToken).ConfigureAwait(false))
        {
            return await InstallPackage(latest, enumerableSources, settings, System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName(), latest.Id), !noCache, !directDownload, log ?? NuGet.Common.NullLogger.Instance, cancellationToken).ConfigureAwait(false);
        }

        throw new PackageNotFoundProtocolException(latest ?? new PackageIdentity(packageNames.FirstOrDefault(), version: null));

        static async Task<bool> IsPackageInSource(PackageIdentity packageIdentity, IEnumerable<SourceRepository> repositories, NuGet.Common.ILogger log, System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!packageIdentity.HasVersion)
            {
                return false;
            }

            return await repositories
                .ToAsyncEnumerable()
                .SelectMany(repository => GetPackages(repository, packageIdentity.Id, log, cancellationToken))
                .AnyAsync(info => info.Listed && NuGet.Versioning.VersionComparer.Default.Equals(info.Version, packageIdentity.Version), cancellationToken)
                .ConfigureAwait(false);
        }

        static async Task<SourcePackageDependencyInfo?> GetLatestPackage(
            IEnumerable<string> sources,
            string packageId,
            bool includePrerelease,
            NuGet.Common.ILogger log,
            ISettings settings,
            System.Threading.CancellationToken cancellationToken)
        {
            SourcePackageDependencyInfo? latest = default;
            foreach (var repository in GetRepositories(settings, sources))
            {
                var package = await GetLatestPackage(repository, packageId, includePrerelease, log, cancellationToken).ConfigureAwait(false);
                if (package is null)
                {
                    continue;
                }

                latest ??= package;
                if (package.Version > latest.Version)
                {
                    latest = package;
                }
            }

            return latest;

            static async Task<SourcePackageDependencyInfo?> GetLatestPackage(SourceRepository source, string packageId, bool includePrerelease, NuGet.Common.ILogger log, System.Threading.CancellationToken cancellationToken)
            {
                return await GetPackages(source, packageId, log, cancellationToken)
                    .Where(package => package.Listed && (!package.HasVersion || !package.Version.IsPrerelease || includePrerelease))
                    .OrderByDescending(package => package.Version, NuGet.Versioning.VersionComparer.Default)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        static async Task<string> InstallPackage(PackageIdentity package, IEnumerable<string> sources, ISettings settings, string installPath, bool useCache, bool addToCache, NuGet.Common.ILogger log, System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Setup local installation path
            var localInstallPath = installPath ?? System.IO.Directory.GetCurrentDirectory();

            // Read package file from remote or cache
            log.LogInformation($"Downloading package {package}");
            using var packageReader = await DownloadPackage(package, sources, settings, useCache, addToCache, log, cancellationToken).ConfigureAwait(false);

            // Package installation
            log.LogInformation($"Installing package {package} to {localInstallPath}");

            var tempInstallPath = await InstallToTemp(packageReader, log, cancellationToken: cancellationToken).ConfigureAwait(false);
            CopyFiles(log, tempInstallPath, localInstallPath);
            System.IO.Directory.Delete(tempInstallPath, recursive: true);

            log.LogInformation($"Package {package} installation complete");
            return System.IO.Path.GetFullPath(localInstallPath);
        }
    }

    /// <summary>
    /// Installs the specified package.
    /// </summary>
    /// <param name="packages">The packages.</param>
    /// <param name="sources">The sources.</param>
    /// <param name="version">The specific version.</param>
    /// <param name="noCache">Set to true to ignore the cache.</param>
    /// <param name="directDownload">Set to true to directly download.</param>
    /// <param name="log">The log.</param>
    /// <param name="root">The root of the settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    public static async Task<string> InstallAsync(
        IEnumerable<PackageIdentity> packages,
        IEnumerable<string>? sources = default,
        NuGet.Versioning.SemanticVersion? version = default,
        bool noCache = default,
        bool directDownload = default,
        NuGet.Common.ILogger? log = default,
        string? root = default,
        System.Threading.CancellationToken cancellationToken = default)
    {
        var package = version is null
            ? GetLatestPackage(packages, includePrerelease: false)
            : GetPackage(packages, version);

        if (package is null)
        {
            throw new InvalidOperationException($"Failed to find any release version of {string.Join(" or ", packages.Select(package => package.Id).Distinct(StringComparer.OrdinalIgnoreCase))}");
        }

        return await InstallPackage(
            package,
            sources ?? Enumerable.Empty<string>(),
            Settings.LoadDefaultSettings(root),
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName(), package.Id),
            !noCache,
            !directDownload,
            log ?? NuGet.Common.NullLogger.Instance,
            cancellationToken).ConfigureAwait(false);

        static PackageIdentity? GetLatestPackage(IEnumerable<PackageIdentity> packages, bool includePrerelease)
        {
            return packages
                .Where(package => (package is not SourcePackageDependencyInfo info || info.Listed) && package.HasVersion && (!package.Version.IsPrerelease || includePrerelease))
                .OrderByDescending(package => package.Version, NuGet.Versioning.VersionComparer.Default)
                .FirstOrDefault();
        }

        static PackageIdentity? GetPackage(IEnumerable<PackageIdentity> packages, NuGet.Versioning.SemanticVersion version)
        {
            return packages.FirstOrDefault(package => (package is not SourcePackageDependencyInfo info || info.Listed) && package.HasVersion && NuGet.Versioning.VersionComparer.Default.Compare(package.Version, version) == 0);
        }

        static async Task<string> InstallPackage(PackageIdentity package, IEnumerable<string> sources, ISettings settings, string installPath, bool useCache, bool addToCache, NuGet.Common.ILogger log, System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Setup local installation path
            var localInstallPath = installPath ?? System.IO.Directory.GetCurrentDirectory();

            // Read package file from remote or cache
            log.LogInformation($"Downloading package {package}");
            using var packageReader = package is SourcePackageDependencyInfo info
                ? await DownloadPackage(info, info.Source, settings, useCache, addToCache, log, cancellationToken).ConfigureAwait(false)
                : await DownloadPackage(package, sources, settings, useCache, addToCache, log, cancellationToken).ConfigureAwait(false);

            // Package installation
            log.LogInformation($"Installing package {package} to {localInstallPath}");

            var tempInstallPath = await InstallToTemp(packageReader, log, cancellationToken: cancellationToken).ConfigureAwait(false);
            CopyFiles(log, tempInstallPath, localInstallPath);
            System.IO.Directory.Delete(tempInstallPath, recursive: true);

            log.LogInformation($"Package {package} installation complete");
            return System.IO.Path.GetFullPath(localInstallPath);
        }
    }

    /// <summary>
    /// Gets the manifest for the specified package and version.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <param name="sources">The sources.</param>
    /// <param name="log">The log.</param>
    /// <param name="root">The root.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The manifest.</returns>
    public static async Task<Manifest?> GetManifestAsync(
        PackageIdentity package,
        IEnumerable<string>? sources = default,
        NuGet.Common.ILogger? log = default,
        string? root = default,
        System.Threading.CancellationToken cancellationToken = default)
    {
        log ??= NuGet.Common.NullLogger.Instance;
        var settings = Settings.LoadDefaultSettings(root);
        var packageReader = package is SourcePackageDependencyInfo info
            ? await DownloadPackage(info, info.Source, settings, useCache: true, addToCache: false, log, cancellationToken).ConfigureAwait(false)
            : await DownloadPackage(package, sources ?? Enumerable.Empty<string>(), settings, useCache: true, addToCache: false, log, cancellationToken).ConfigureAwait(false);

        var installDir = await InstallToTemp(packageReader, log, PackageSaveMode.Nuspec, XmlDocFileSaveMode.None, cancellationToken).ConfigureAwait(false);

        var manifest = GetManifest(installDir);

        System.IO.Directory.Delete(installDir, recursive: true);

        return manifest;

        static Manifest GetManifest(string path, string? name = "*")
        {
            var file = System.IO.Directory.EnumerateFiles(path, name + PackagingCoreConstants.NuspecExtension).Single();
            using var stream = System.IO.File.OpenRead(file);
            return Manifest.ReadFrom(stream, validateSchema: true);
        }
    }

    /// <summary>
    /// Gets the latest packages.
    /// </summary>
    /// <param name="packageNames">The package names.</param>
    /// <param name="sources">The sources.</param>
    /// <param name="log">The log.</param>
    /// <param name="root">The settings root.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The latest NuGet packages.</returns>
    public static IAsyncEnumerable<PackageIdentity> GetPackagesAsync(
        IEnumerable<string> packageNames,
        IEnumerable<string>? sources = default,
        NuGet.Common.ILogger? log = default,
        string? root = default,
        System.Threading.CancellationToken cancellationToken = default)
    {
        var settings = Settings.LoadDefaultSettings(root);
        log ??= NuGet.Common.NullLogger.Instance;
        return packageNames
            .ToAsyncEnumerable()
            .SelectMany(packageName => GetPackagesFromSources(sources, packageName, log, settings, cancellationToken))
            .Where(info => info.Listed && info.HasVersion && !info.Version.IsLegacyVersion);

        static IAsyncEnumerable<SourcePackageDependencyInfo> GetPackagesFromSources(IEnumerable<string>? sources, string packageId, NuGet.Common.ILogger log, ISettings settings, System.Threading.CancellationToken cancellationToken)
        {
            return GetRepositories(settings, sources).ToAsyncEnumerable().SelectMany(repository => GetPackages(repository, packageId, log, cancellationToken));
        }
    }

    /// <summary>
    /// Gets the latest packages.
    /// </summary>
    /// <param name="folderCommits">The commits for the package.</param>
    /// <param name="headCommits">The commits for the head.</param>
    /// <param name="packageNames">The package names.</param>
    /// <param name="sources">The sources.</param>
    /// <param name="log">The log.</param>
    /// <param name="root">The settings root.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The latest NuGet packages.</returns>
    public static Task<PackageIdentity?> GetPackageByCommit(
        IList<string> folderCommits,
        IList<string> headCommits,
        IEnumerable<string> packageNames,
        IEnumerable<string>? sources = default,
        NuGet.Common.ILogger? log = default,
        string? root = default,
        System.Threading.CancellationToken cancellationToken = default) => GetPackageByCommit(
            folderCommits,
            headCommits,
            GetPackagesAsync(packageNames, sources, log, root, cancellationToken).ToEnumerable(),
            sources,
            log,
            root,
            cancellationToken);

    /// <summary>
    /// Gets the latest packages.
    /// </summary>
    /// <param name="folderCommits">The commits for the package.</param>
    /// <param name="headCommits">The commits for the head.</param>
    /// <param name="packages">The packages.</param>
    /// <param name="sources">The sources.</param>
    /// <param name="log">The log.</param>
    /// <param name="root">The settings root.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The latest NuGet packages.</returns>
    public static async Task<PackageIdentity?> GetPackageByCommit(
        IList<string> folderCommits,
        IList<string> headCommits,
        IEnumerable<PackageIdentity> packages,
        IEnumerable<string>? sources = default,
        NuGet.Common.ILogger? log = default,
        string? root = default,
        System.Threading.CancellationToken cancellationToken = default)
    {
        var settings = Settings.LoadDefaultSettings(root);
        log ??= NuGet.Common.NullLogger.Instance;
        IEnumerable<SourceRepository> sourceRepositories = GetRepositories(settings, sources);

        using var cacheContext = new SourceCacheContext { IgnoreFailedSources = true };
        foreach (var package in packages)
        {
            var manifest = await GetManifest(package, sourceRepositories, cacheContext, log, cancellationToken).ConfigureAwait(false);
            if (manifest?.Metadata?.Repository is RepositoryMetadata repository)
            {
                if (headCommits.Contains(repository.Commit, StringComparer.Ordinal))
                {
                    // this is in the head commits, so let this through
                    return package;
                }

                // see where this is in the folder commits
                var index = folderCommits.IndexOf(repository.Commit);
                if (index < 0)
                {
                    // not found.
                    break;
                }

                if (index > 0)
                {
                    // this is before the latest commit
                    continue;
                }

                // this is the same commit
                return package;
            }
        }

        return default;

        static async Task<Manifest?> GetManifest(PackageIdentity package, IEnumerable<SourceRepository> sourceRepositories, SourceCacheContext cacheContext, NuGet.Common.ILogger logger, System.Threading.CancellationToken cancellationToken)
        {
            return package is SourcePackageDependencyInfo info
                ? await GetManifestFromSource(info, cacheContext, logger, cancellationToken).ConfigureAwait(false)
                : await GetManifestFromSources(package, sourceRepositories, cacheContext, logger, cancellationToken).ConfigureAwait(false);

            static async Task<Manifest?> GetManifestFromSource(SourcePackageDependencyInfo info, SourceCacheContext cacheContext, NuGet.Common.ILogger logger, System.Threading.CancellationToken cancellationToken)
            {
                var archiveStream = new System.IO.MemoryStream();
                if (await info.Source.GetResourceAsync<HttpSourceResource>(cancellationToken).ConfigureAwait(false) is HttpSourceResource httpSourceResource)
                {
                    var downloader = new FindPackagesByIdNupkgDownloader(httpSourceResource.HttpSource);
                    if (!await downloader.CopyNupkgToStreamAsync(info, info.DownloadUri.ToString(), archiveStream, cacheContext, logger, cancellationToken).ConfigureAwait(false))
                    {
                        return default;
                    }
                }
                else if (await info.Source.GetResourceAsync<FindPackageByIdResource>(cancellationToken).ConfigureAwait(false) is FindPackageByIdResource findPackagesByIdResource
                    && !await findPackagesByIdResource.CopyNupkgToStreamAsync(info.Id, info.Version, archiveStream, cacheContext, logger, cancellationToken).ConfigureAwait(false))
                {
                    return default;
                }

                using var archive = new System.IO.Compression.ZipArchive(archiveStream, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: false);
                var entry = archive.GetEntry(info.Id + PackagingCoreConstants.NuspecExtension);
#if NETSTANDARD2_1_OR_GREATER
                var entryStream = entry.Open();
                await using (entryStream.ConfigureAwait(false))
                {
                    return Manifest.ReadFrom(entryStream, validateSchema: true);
                }
#else
                using var entryStream = entry.Open();
                return Manifest.ReadFrom(entryStream, validateSchema: true);
#endif
            }

            static async Task<Manifest?> GetManifestFromSources(PackageIdentity package, IEnumerable<SourceRepository> sources, SourceCacheContext cacheContext, NuGet.Common.ILogger logger, System.Threading.CancellationToken cancellationToken)
            {
                foreach (var source in sources)
                {
                    var dependencyInfoResource = await source.GetResourceAsync<DependencyInfoResource>(cancellationToken).ConfigureAwait(false);
                    var info = await dependencyInfoResource.ResolvePackage(package, NuGet.Frameworks.NuGetFramework.AgnosticFramework, cacheContext, logger, cancellationToken).ConfigureAwait(false);
                    if (info is null)
                    {
                        continue;
                    }

                    var manifest = await GetManifestFromSource(info, cacheContext, logger, cancellationToken).ConfigureAwait(false);
                    if (manifest is not null)
                    {
                        return manifest;
                    }
                }

                return default;
            }
        }
    }

    /// <summary>
    /// Gets the latest packages.
    /// </summary>
    /// <param name="packageNames">The package names.</param>
    /// <param name="sources">The sources.</param>
    /// <param name="log">The log.</param>
    /// <param name="root">The settings root.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The latest NuGet packages.</returns>
    public static IAsyncEnumerable<PackageIdentity> GetLatestPackagesAsync(
        IEnumerable<string> packageNames,
        IEnumerable<string>? sources = default,
        NuGet.Common.ILogger? log = default,
        string? root = default,
        System.Threading.CancellationToken cancellationToken = default) => GetLatestPackagesAsync(
            GetPackagesAsync(packageNames, sources, log, root, cancellationToken),
            cancellationToken);

    /// <summary>
    /// Gets the latest packages.
    /// </summary>
    /// <param name="packages">The packages.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The latest NuGet packages.</returns>
    public static async IAsyncEnumerable<PackageIdentity> GetLatestPackagesAsync(
        this IAsyncEnumerable<PackageIdentity> packages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
    {
        await foreach (var group in packages
            .GroupBy(info => (info.Version.Version.Major, info.Version.Version.Minor, info.Version.IsPrerelease))
            .ConfigureAwait(false)
            .WithCancellation(cancellationToken))
        {
            yield return await group.MaxAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the latest packages.
    /// </summary>
    /// <param name="packages">The packages.</param>
    /// <returns>The latest NuGet packages.</returns>
    public static IEnumerable<PackageIdentity> GetLatestPackages(
        this IEnumerable<PackageIdentity> packages)
    {
        foreach (var group in packages
            .GroupBy(info => (info.Version.Version.Major, info.Version.Version.Minor, info.Version.IsPrerelease)))
        {
            yield return group.Max();
        }
    }

    private static IEnumerable<SourceRepository> GetRepositories(ISettings settings, IEnumerable<string>? sources)
    {
        var enabledSources = SettingsUtility.GetEnabledSources(settings);
        var repositories = sources?
            .Where(source => !string.IsNullOrEmpty(source))
            .Select(source => GetFromMachineSources(source, enabledSources) ?? Repository.Factory.GetCoreV3(source)).ToArray();
        return repositories?.Length > 0
            ? repositories
            : enabledSources.Select(packageSource => Repository.Factory.GetCoreV3(packageSource.Source)).ToArray();

        static PackageSource ResolveSource(IEnumerable<PackageSource> availableSources, string source)
        {
            var resolvedSource = availableSources.FirstOrDefault(
                    f => f.Source.Equals(source, StringComparison.OrdinalIgnoreCase) ||
                        f.Name.Equals(source, StringComparison.OrdinalIgnoreCase));

            return resolvedSource ?? new PackageSource(source);
        }

        static SourceRepository? GetFromMachineSources(string source, IEnumerable<PackageSource> enabledSources)
        {
            var resolvedSource = ResolveSource(enabledSources, source);
            return resolvedSource.ProtocolVersion == 2
                ? Repository.Factory.GetCoreV2(resolvedSource)
                : Repository.Factory.GetCoreV3(resolvedSource.Source);
        }
    }

    private static async IAsyncEnumerable<SourcePackageDependencyInfo> GetPackages(
        SourceRepository source,
        string packageId,
        NuGet.Common.ILogger log,
        [System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<SourcePackageDependencyInfo>? returnValues = default;

        try
        {
            var dependencyInfoResource = await source.GetResourceAsync<DependencyInfoResource>(cancellationToken).ConfigureAwait(false);
            using var sourceCacheContext = new SourceCacheContext { IgnoreFailedSources = true };
            returnValues = await dependencyInfoResource.ResolvePackages(packageId, NuGet.Frameworks.NuGetFramework.AgnosticFramework, sourceCacheContext, log, cancellationToken).ConfigureAwait(false);
        }
        catch (FatalProtocolException e)
        {
            log.LogError(e.Message);
        }

        if (returnValues is null)
        {
            yield break;
        }

        foreach (var returnValue in returnValues)
        {
            yield return returnValue;
        }
    }

    private static async Task<PackageReaderBase> DownloadPackage(
        PackageIdentity package,
        IEnumerable<string> sources,
        ISettings settings,
        bool useCache,
        bool addToCache,
        NuGet.Common.ILogger logger,
        System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (useCache)
        {
            var globalPackage = GlobalPackagesFolderUtility.GetPackage(package, SettingsUtility.GetGlobalPackagesFolder(settings));
            if (globalPackage is not null)
            {
                logger.LogInformation($"Found {package} in global package folder");
                return globalPackage.PackageReader;
            }
        }

        using (var sourceCacheContext = new SourceCacheContext())
        {
            foreach (var repository in GetRepositories(settings, sources))
            {
                var packageReader = await DownloadPackage(package, repository, sourceCacheContext, settings, addToCache, logger, cancellationToken).ConfigureAwait(false);
                if (packageReader is not null)
                {
                    return packageReader;
                }
            }
        }

        throw new PackageNotFoundProtocolException(package);
    }

    private static async Task<PackageReaderBase> DownloadPackage(
        PackageIdentity package,
        SourceRepository repository,
        ISettings settings,
        bool useCache,
        bool addToCache,
        NuGet.Common.ILogger logger,
        System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (useCache)
        {
            var globalPackage = GlobalPackagesFolderUtility.GetPackage(package, SettingsUtility.GetGlobalPackagesFolder(settings));
            if (globalPackage is not null)
            {
                logger.LogInformation($"Found {package} in global package folder");
                return globalPackage.PackageReader;
            }
        }

        using var sourceCacheContext = new SourceCacheContext();

        return await DownloadPackage(package, repository, sourceCacheContext, settings, addToCache, logger, cancellationToken).ConfigureAwait(false)
            ?? throw new PackageNotFoundProtocolException(package);
    }

    private static async Task<PackageReaderBase?> DownloadPackage(
        PackageIdentity package,
        SourceRepository repository,
        SourceCacheContext cacheContext,
        ISettings settings,
        bool addToCache,
        NuGet.Common.ILogger logger,
        System.Threading.CancellationToken cancellationToken)
    {
        var findPackagesByIdResource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken).ConfigureAwait(false);
        using var packageDownloder = await findPackagesByIdResource.GetPackageDownloaderAsync(package, cacheContext, logger, cancellationToken).ConfigureAwait(false);

        if (packageDownloder is null)
        {
            logger.LogWarning($"Package {package} not found in repository {repository}");
            return default;
        }

        logger.LogInformation($"Getting {package} from {repository}");

        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        var downloaded = await packageDownloder.CopyNupkgFileToAsync(tempFile, cancellationToken).ConfigureAwait(false);

        if (!downloaded)
        {
            throw new InvalidOperationException($"Failed to fetch package {package} from source {repository}");
        }

        if (addToCache)
        {
            var stream = System.IO.File.OpenRead(tempFile);
#if NETSTANDARD2_1_OR_GREATER
            await
#endif
            using (stream)
            {
                var downloadCacheContext = new PackageDownloadContext(cacheContext);
                var clientPolicy = NuGet.Packaging.Signing.ClientPolicyContext.GetClientPolicy(settings, logger);
                using var downloadResourceResult = await GlobalPackagesFolderUtility.AddPackageAsync(repository.PackageSource.Source, package, stream, SettingsUtility.GetGlobalPackagesFolder(settings), downloadCacheContext.ParentId, clientPolicy, logger, cancellationToken).ConfigureAwait(false);
            }
        }

        return new PackageArchiveReader(tempFile);
    }

    private static void CopyFiles(NuGet.Common.ILogger logger, string source, string destination)
    {
        if (source[source.Length - 1] != System.IO.Path.DirectorySeparatorChar)
        {
            source = string.Concat(source, System.IO.Path.DirectorySeparatorChar);
        }

        if (string.IsNullOrEmpty(destination))
        {
            destination = System.IO.Directory.GetCurrentDirectory();
        }

        // copy all the files
        foreach (var file in System.IO.Directory.EnumerateFiles(source, "*", System.IO.SearchOption.AllDirectories))
        {
            var relativePath = MakeRelativePath(source, file);
            var destinationPath = System.IO.Path.Combine(destination, relativePath);

            // create the directory
            var directory = System.IO.Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // copy to destination
            System.IO.File.Copy(file, destinationPath, overwrite: true);

            if (!ValidateFileInstallation(file, source, destination))
            {
                throw new InvalidOperationException($"Failed to install package file <{relativePath}> to <{destinationPath}>");
            }

            logger.LogDebug($"Installed file {relativePath} to {destinationPath}");

            static bool ValidateFileInstallation(string sourceFile, string source, string destination)
            {
                var relativePath = MakeRelativePath(source, sourceFile);
                var destinationFile = System.IO.Path.Combine(destination, relativePath);

                // check to see if this file is in the destination
                if (!System.IO.File.Exists(destinationFile))
                {
                    return false;
                }

                return FilesContentsAreEqual(new System.IO.FileInfo(sourceFile), new System.IO.FileInfo(destinationFile));

                static bool FilesContentsAreEqual(System.IO.FileInfo first, System.IO.FileInfo second)
                {
                    if (first.Length != second.Length)
                    {
                        return false;
                    }

                    var firstFileStream = first.OpenRead();
                    var secondFileStream = second.OpenRead();
                    var result = StreamsContentsAreEqual(firstFileStream, secondFileStream);
                    firstFileStream?.Dispose();
                    secondFileStream?.Dispose();
                    return result;

                    static bool StreamsContentsAreEqual(System.IO.Stream first, System.IO.Stream second)
                    {
                        const int sizeOfLong = sizeof(long);
                        const int bufferSize = 1024 * sizeOfLong;
                        var firstBuffer = new byte[bufferSize];
                        var secondBuffer = new byte[bufferSize];

                        while (true)
                        {
                            var firstCount = first.Read(firstBuffer, 0, bufferSize);
                            var secondCount = second.Read(secondBuffer, 0, bufferSize);

                            if (firstCount != secondCount)
                            {
                                return false;
                            }

                            if (firstCount == 0)
                            {
                                return true;
                            }

                            var iterations = (int)Math.Ceiling((double)firstCount / sizeOfLong);
                            for (var i = 0; i < iterations; i++)
                            {
                                if (BitConverter.ToInt64(firstBuffer, i * sizeOfLong) != BitConverter.ToInt64(secondBuffer, i * sizeOfLong))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private static string MakeRelativePath(string basePath, string absolutePath)
    {
        if (string.IsNullOrEmpty(basePath) || !System.IO.Path.IsPathRooted(absolutePath))
        {
            return absolutePath;
        }

        var baseUri = new Uri(basePath);
        var uri = new Uri(absolutePath);

        if (!string.Equals(baseUri.Scheme, uri.Scheme, StringComparison.Ordinal))
        {
            return absolutePath;
        }

        var relativeUri = baseUri.MakeRelativeUri(uri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        return uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase)
            ? relativePath.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar)
            : relativePath;
    }

    private static async Task<SourcePackageDependencyInfo?> GetPackage(
        IEnumerable<string> sources,
        string packageId,
        NuGet.Versioning.SemanticVersion version,
        NuGet.Common.ILogger log,
        ISettings settings,
        System.Threading.CancellationToken cancellationToken)
    {
        foreach (var repository in GetRepositories(settings, sources))
        {
            var package = await GetPackage(repository, packageId, version, log, cancellationToken).ConfigureAwait(false);
            if (package is null)
            {
                continue;
            }

            return package;
        }

        return default;

        static async Task<SourcePackageDependencyInfo?> GetPackage(SourceRepository source, string packageId, NuGet.Versioning.SemanticVersion version, NuGet.Common.ILogger log, System.Threading.CancellationToken cancellationToken)
        {
            return await GetPackages(source, packageId, log, cancellationToken)
                .FirstOrDefaultAsync(package => package.Listed && package.HasVersion && NuGet.Versioning.VersionComparer.Default.Compare(package.Version, version) == 0, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task<string> InstallToTemp(PackageReaderBase reader, NuGet.Common.ILogger logger, PackageSaveMode packageSaveMode = PackageSaveMode.Files, XmlDocFileSaveMode xmlDocFileSaveMode = default, System.Threading.CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tempInstallPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());

        var packageFiles = (await reader.GetFilesAsync(cancellationToken).ConfigureAwait(false)).Where(file => PackageHelper.IsPackageFile(file, packageSaveMode)).ToList();
        var packageFileExtractor = new PackageFileExtractor(packageFiles, xmlDocFileSaveMode);

        await reader.CopyFilesAsync(tempInstallPath, packageFiles, packageFileExtractor.ExtractPackageFile, logger, cancellationToken).ConfigureAwait(false);

        return tempInstallPath;
    }
}