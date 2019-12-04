// -----------------------------------------------------------------------
// <copyright file="NuGetInstaller.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning.TeamCity
{
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
    internal static class NuGetInstaller
    {
        /// <summary>
        /// Installs the specified package.
        /// </summary>
        /// <param name="packageNames">The package names.</param>
        /// <param name="sources">The sources.</param>
        /// <param name="noCache">Set to true to ignore the cache.</param>
        /// <param name="directDownload">Set to true to directly download.</param>
        /// <param name="log">The log.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<string> InstallAsync(IEnumerable<string> packageNames, IEnumerable<string> sources = null, bool noCache = false, bool directDownload = false, NuGet.Common.ILogger log = null, System.Threading.CancellationToken cancellationToken = default)
        {
            var enumerableSources = sources ?? Enumerable.Empty<string>();
            var settings = Settings.LoadDefaultSettings(null);
            SourcePackageDependencyInfo latest = default;
            foreach (var packageName in packageNames)
            {
                var package = await GetLatestPackage(enumerableSources, packageName, false, log ?? NuGet.Common.NullLogger.Instance, cancellationToken).ConfigureAwait(false);
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

            throw new PackageNotFoundProtocolException(latest ?? new PackageIdentity(packageNames.FirstOrDefault(), null));
        }

        /// <summary>
        /// Gets the latest vesion.
        /// </summary>
        /// <param name="packageName">The package name.</param>
        /// <param name="sources">The sources.</param>
        /// <param name="log">The log.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The latest NuGet version.</returns>
        public static Task<NuGet.Versioning.NuGetVersion> GetLatestVersionAsync(string packageName, IEnumerable<string> sources = null, NuGet.Common.ILogger log = null, System.Threading.CancellationToken cancellationToken = default) => GetLatestVersion(GetRepositories(Settings.LoadDefaultSettings(null), sources), packageName, true, log ?? NuGet.Common.NullLogger.Instance, cancellationToken);

        /// <summary>
        /// Gets the latest versions.
        /// </summary>
        /// <param name="packageNames">The package names.</param>
        /// <param name="sources">The sources.</param>
        /// <param name="log">The log.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The latest NuGet versions.</returns>
        public static async IAsyncEnumerable<NuGet.Versioning.NuGetVersion> GetLatestVersionsAsync(
            IEnumerable<string> packageNames,
            IEnumerable<string> sources = null,
            NuGet.Common.ILogger log = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
        {
            await foreach (var group in packageNames
                .ToAsyncEnumerable()
                .SelectMany(packageName => GetVersions(sources, packageName, log ?? NuGet.Common.NullLogger.Instance, cancellationToken))
                .Where(info => info.Listed && info.HasVersion && !info.Version.IsLegacyVersion)
                .Select(info => info.Version)
                .GroupBy(version => (version.Version.Major, version.Version.Minor, version.IsPrerelease)))
            {
                yield return await group.MaxAsync().ConfigureAwait(false);
            }
        }

        private static async Task<string> InstallPackage(PackageIdentity package, IEnumerable<string> sources, ISettings settings, string installPath, bool useCache, bool addToCache, NuGet.Common.ILogger log, System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Setup local installation path
            var localInstallPath = installPath ?? System.IO.Directory.GetCurrentDirectory();

            // Read package file from remote or cache
            log.LogInformation($"Downloading package {package}");
            using var packageReader = await DownloadPackage(package, sources, settings, useCache, addToCache, log, cancellationToken).ConfigureAwait(false);

            // Package installation
            log.LogInformation($"Installing package {package} to {localInstallPath}");

            var tempInstallPath = await InstallToTemp(package, packageReader, log, cancellationToken).ConfigureAwait(false);
            CopyFiles(log, tempInstallPath, localInstallPath);
            System.IO.Directory.Delete(tempInstallPath, true);

            log.LogInformation($"Package {package} installation complete");
            return System.IO.Path.GetFullPath(localInstallPath);
        }

        private static IEnumerable<SourceRepository> GetRepositories(ISettings settings, IEnumerable<string> sources)
        {
            var repositories = sources.Select(source => Repository.Factory.GetCoreV3(source)).ToList();
            if (repositories.Count == 0)
            {
                repositories = SettingsUtility.GetEnabledSources(settings).Select(packageSource => Repository.Factory.GetCoreV3(packageSource.Source)).ToList();
            }

            return repositories;
        }

        private static async Task<NuGet.Versioning.NuGetVersion> GetLatestVersion(IEnumerable<SourceRepository> repositories, string id, bool includePrerelease, NuGet.Common.ILogger log, System.Threading.CancellationToken cancellationToken)
        {
            NuGet.Versioning.NuGetVersion latestVersion = null;

            foreach (var repository in repositories)
            {
                var repositoryVersion = await GetLatestVersion(repository, id, includePrerelease, log, cancellationToken).ConfigureAwait(false);
                if (repositoryVersion != default && repositoryVersion > latestVersion)
                {
                    latestVersion = repositoryVersion;
                }
            }

            return latestVersion;
        }

        private static async Task<NuGet.Versioning.NuGetVersion> GetLatestVersion(SourceRepository source, string packageId, bool includePrerelease, NuGet.Common.ILogger log, System.Threading.CancellationToken cancellationToken) => await
                GetVersions(source, packageId, log, cancellationToken)
                    .Where(package => package.Listed && (package.HasVersion && package.Version.IsPrerelease ? includePrerelease : true))
                    .OrderByDescending(package => package.Version, NuGet.Versioning.VersionComparer.Default)
                    .Select(package => package.Version)
                    .FirstOrDefaultAsync().ConfigureAwait(false);

        private static async Task<SourcePackageDependencyInfo> GetLatestPackage(IEnumerable<string> sources, string packageId, bool includePrerelease, NuGet.Common.ILogger log, System.Threading.CancellationToken cancellationToken)
        {
            SourcePackageDependencyInfo latest = default;
            foreach (var repository in GetRepositories(Settings.LoadDefaultSettings(null), sources))
            {
                var package = await GetLatestPackage(repository, packageId, includePrerelease, log, cancellationToken).ConfigureAwait(false);
                if (latest == null || package.Version > latest.Version)
                {
                    latest = package;
                }
            }

            return latest;
        }

        private static async Task<SourcePackageDependencyInfo> GetLatestPackage(SourceRepository source, string packageId, bool includePrerelease, NuGet.Common.ILogger log, System.Threading.CancellationToken cancellationToken) => await
            GetVersions(source, packageId, log, cancellationToken)
            .Where(package => package.Listed && (package.HasVersion && package.Version.IsPrerelease ? includePrerelease : true))
            .OrderByDescending(package => package.Version, NuGet.Versioning.VersionComparer.Default)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        private static IAsyncEnumerable<SourcePackageDependencyInfo> GetVersions(IEnumerable<string> sources, string packageId, NuGet.Common.ILogger log, System.Threading.CancellationToken cancellationToken) => GetRepositories(Settings.LoadDefaultSettings(null), sources).ToAsyncEnumerable().SelectMany(repository => GetVersions(repository, packageId, log, cancellationToken));

        private static async IAsyncEnumerable<SourcePackageDependencyInfo> GetVersions(
            SourceRepository source,
            string packageId,
            NuGet.Common.ILogger log,
            [System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<SourcePackageDependencyInfo> returnValues = null;

            try
            {
                var dependencyInfoResource = await source.GetResourceAsync<DependencyInfoResource>(cancellationToken).ConfigureAwait(false);
                using var sourceCacheContext = new SourceCacheContext() { IgnoreFailedSources = true };
                returnValues = await dependencyInfoResource.ResolvePackages(packageId, NuGet.Frameworks.NuGetFramework.AgnosticFramework, sourceCacheContext, log, cancellationToken).ConfigureAwait(false);
            }
            catch (FatalProtocolException e)
            {
                log.LogError(e.Message);
            }

            if (returnValues != null)
            {
                foreach (var returnValue in returnValues)
                {
                    yield return returnValue;
                }
            }
        }

        private static async Task<bool> IsPackageInSource(PackageIdentity packageIdentity, IEnumerable<SourceRepository> repositories, NuGet.Common.ILogger log, System.Threading.CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (!packageIdentity.HasVersion)
            {
                return false;
            }

            return await repositories
                .ToAsyncEnumerable()
                .SelectMany(repository => GetVersions(repository, packageIdentity.Id, log, token))
                .AnyAsync(info => info.Listed && NuGet.Versioning.VersionComparer.Default.Equals(info.Version, packageIdentity.Version))
                .ConfigureAwait(false);
        }

        private static async Task<PackageReaderBase> DownloadPackage(PackageIdentity package, IEnumerable<string> sources, ISettings settings, bool useCache, bool addToCache, NuGet.Common.ILogger logger, System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (useCache)
            {
                var globalPackage = GlobalPackagesFolderUtility.GetPackage(package, SettingsUtility.GetGlobalPackagesFolder(settings));
                if (globalPackage != null)
                {
                    logger.LogInformation($"Found {package} in global package folder");
                    return globalPackage.PackageReader;
                }
            }

            using (var sourceCacheContext = new SourceCacheContext())
            {
                foreach (var repository in GetRepositories(settings, sources))
                {
                    var findPackagesByIdResource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken).ConfigureAwait(false);
                    using var packageDownloder = await findPackagesByIdResource.GetPackageDownloaderAsync(package, sourceCacheContext, logger, cancellationToken).ConfigureAwait(false);

                    if (packageDownloder == null)
                    {
                        logger.LogWarning($"Package {package} not found in repository {repository}");
                        continue;
                    }

                    logger.LogInformation($"Getting {package} from {repository}");

                    var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
                    var downloaded = await packageDownloder.CopyNupkgFileToAsync(tempFile, cancellationToken).ConfigureAwait(false);

                    if (!downloaded)
                    {
                        throw new Exception($"Failed to fetch package {package} from source {repository}");
                    }

                    if (addToCache)
                    {
                        using var stream = System.IO.File.OpenRead(tempFile);
                        var downloadCacheContext = new PackageDownloadContext(sourceCacheContext);
                        var clientPolicy = NuGet.Packaging.Signing.ClientPolicyContext.GetClientPolicy(settings, logger);
                        using var downloadResourceResult = await GlobalPackagesFolderUtility.AddPackageAsync(repository.PackageSource.Source, package, stream, SettingsUtility.GetGlobalPackagesFolder(settings), downloadCacheContext.ParentId, clientPolicy, logger, cancellationToken).ConfigureAwait(false);
                    }

                    return new PackageArchiveReader(tempFile);
                }
            }

            throw new PackageNotFoundProtocolException(package);
        }

        private static async Task<string> InstallToTemp(PackageIdentity package, PackageReaderBase reader, NuGet.Common.ILogger logger, System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tempInstallPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
            var versionFolderPathResolver = new VersionFolderPathResolver(tempInstallPath, true);

            var hashFileName = versionFolderPathResolver.GetHashFileName(package.Id, package.Version);
            var packageFiles = (await reader.GetFilesAsync(cancellationToken).ConfigureAwait(false)).Where(file => ShouldInclude(file, hashFileName)).ToList();
            var packageFileExtractor = new PackageFileExtractor(packageFiles, XmlDocFileSaveMode.None);

            await reader.CopyFilesAsync(tempInstallPath, packageFiles, packageFileExtractor.ExtractPackageFile, logger, cancellationToken).ConfigureAwait(false);

            return tempInstallPath;
        }

        private static bool ShouldInclude(string fullName, string hashFileName)
        {
            // Not all the files from a zip file are needed
            // So, files such as '.rels' and '[Content_Types].xml' are not extracted
            var fileName = System.IO.Path.GetFileName(fullName);
            if (fileName != null)
            {
                if (fileName == ".rels")
                {
                    return false;
                }

                if (fileName == "[Content_Types].xml")
                {
                    return false;
                }
            }

            return System.IO.Path.GetExtension(fullName) != ".psmdcp"
                && !string.Equals(fullName, hashFileName, StringComparison.OrdinalIgnoreCase)
                && (!PackageHelper.IsRoot(fullName) || (!PackageHelper.IsNuspec(fullName) && !fullName.EndsWith(PackagingCoreConstants.NupkgExtension, StringComparison.OrdinalIgnoreCase)));
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
                System.IO.File.Copy(file, destinationPath, true);

                if (!ValidateFileInstallation(file, source, destination))
                {
                    throw new Exception($"Failed to install package file <{relativePath}> to <{destinationPath}>");
                }

                logger.LogDebug($"Installed file {relativePath} to {destinationPath}");
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

            if (baseUri.Scheme != uri.Scheme)
            {
                return absolutePath;
            }

            var relativeUri = baseUri.MakeRelativeUri(uri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase)
                ? relativePath.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar)
                : relativePath;
        }

        private static bool ValidateFileInstallation(string sourceFile, string source, string destination)
        {
            var relativePath = MakeRelativePath(source, sourceFile);
            var destinationFile = System.IO.Path.Combine(destination, relativePath);

            // check to see if this file is in the destination
            if (!System.IO.File.Exists(destinationFile))
            {
                return false;
            }

            return FilesContentsAreEqual(new System.IO.FileInfo(sourceFile), new System.IO.FileInfo(destinationFile));
        }

        private static bool FilesContentsAreEqual(System.IO.FileInfo first, System.IO.FileInfo second)
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
        }

        private static bool StreamsContentsAreEqual(System.IO.Stream first, System.IO.Stream second)
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
