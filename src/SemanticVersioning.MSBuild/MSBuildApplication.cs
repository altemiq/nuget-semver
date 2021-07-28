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
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The MSBuild application.
    /// </summary>
    public static class MSBuildApplication
    {
        private static readonly NuGet.Versioning.SemanticVersion Empty = new(0, 0, 0);

        /// <summary>
        /// The process project.
        /// </summary>
        /// <param name="projectName">The project name.</param>
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
        /// <param name="logger">The logger.</param>
        /// <param name="output">The output type.</param>
        /// <param name="previous">The previous version.</param>
        /// <param name="noCache">Set to <see langword="true"/> to disable using the machine cache as the first package source.</param>
        /// <param name="directDownload">Set to <see langword="true"/> to download directly without populating any caches with metadata or binaries.</param>
        /// <param name="getVersionSuffix">The function to get the version suffix.</param>
        /// <returns>The task.</returns>
        public static async Task<NuGet.Versioning.SemanticVersion> ProcessProject(
            string projectName,
            string projectDirectory,
            string assemblyName,
            string projectPackageId,
            string targetExt,
            string buildOutputTargetFolder,
            string packageOutputPath,
            System.Collections.Generic.IEnumerable<string> source,
            System.Collections.Generic.IEnumerable<string> packageIds,
            System.Text.RegularExpressions.Regex? packageIdRegex,
            string packageIdReplace,
            ILogger logger,
            OutputTypes output,
            NuGet.Versioning.SemanticVersion? previous,
            bool noCache,
            bool directDownload,
            Func<string?, string?> getVersionSuffix)
        {
            if (output.HasFlag(OutputTypes.Diagnostic))
            {
                logger.LogTrace(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.Checking, projectName));
            }

            // install the NuGet package
            var projectPackageIds = new[] { projectPackageId }.Union(packageIds, StringComparer.Ordinal);
            if (packageIdRegex is not null)
            {
                projectPackageIds = projectPackageIds.Union(new[] { packageIdRegex.Replace(projectPackageId, packageIdReplace) }, StringComparer.Ordinal);
            }

            var installDir = await TryInstallAsync(projectPackageIds, projectDirectory).ConfigureAwait(false);
            var previousVersions = IsNullOrEmpty(previous)
                ? NuGetInstaller.GetLatestVersionsAsync(projectPackageIds, source, root: projectDirectory)
                : CreateAsyncEnumerable(previous);
            var calculatedVersion = new NuGet.Versioning.SemanticVersion(0, 0, 0);

            if (installDir is null)
            {
                var previousVersion = await previousVersions.MaxAsync().ConfigureAwait(false);
                calculatedVersion = previousVersion is null
                    ? new NuGet.Versioning.SemanticVersion(1, 0, 0, getVersionSuffix(LibraryComparison.DefaultAlphaRelease)) // have this as being a 0.1.0 release
                    : new NuGet.Versioning.SemanticVersion(previousVersion.Major, previousVersion.Minor, previousVersion.Patch + 1, getVersionSuffix(previousVersion.Release));
            }
            else
            {
                var installedBuildOutputTargetFolder = TrimEndingDirectorySeparator(System.IO.Path.Combine(installDir, buildOutputTargetFolder));

                var previousStringVersions = await previousVersions.Select(previousVersion => previousVersion.ToString()).ToArrayAsync().ConfigureAwait(false);

                // Get the package output path
                var fullPackageOutputPath = TrimEndingDirectorySeparator(System.IO.Path.Combine(projectDirectory, packageOutputPath.Replace('\\', System.IO.Path.DirectorySeparatorChar)));

                // check the frameworks
                var currentFrameworks = System.IO.Directory.EnumerateDirectories(fullPackageOutputPath).Select(System.IO.Path.GetFileName).ToArray();
                var previousFrameworks = System.IO.Directory.EnumerateDirectories(installedBuildOutputTargetFolder).Select(System.IO.Path.GetFileName).ToArray();
                var frameworks = currentFrameworks.Intersect(previousFrameworks, StringComparer.OrdinalIgnoreCase);
                if (previousFrameworks.Except(currentFrameworks, StringComparer.OrdinalIgnoreCase).Any())
                {
                    // we have removed frameworks, this is a breaking change
                    calculatedVersion = LibraryComparison.CalculateVersion(SemanticVersionChange.Major, previousStringVersions, getVersionSuffix(default));
                }
                else if (currentFrameworks.Except(previousFrameworks, StringComparer.OrdinalIgnoreCase).Any())
                {
                    // we have added frameworks, this is a feature change
                    calculatedVersion = LibraryComparison.CalculateVersion(SemanticVersionChange.Minor, previousStringVersions, getVersionSuffix(default));
                }

                var searchPattern = assemblyName + targetExt;
#if NETFRAMEWORK
                const System.IO.SearchOption searchOptions = System.IO.SearchOption.TopDirectoryOnly;
#else
                var searchOptions = new System.IO.EnumerationOptions { RecurseSubdirectories = false };
#endif
                foreach (var currentDll in frameworks.SelectMany(framework => System.IO.Directory.EnumerateFiles(System.IO.Path.Combine(fullPackageOutputPath, framework ?? string.Empty), searchPattern, searchOptions)))
                {
                    var oldDll = currentDll
#if NETFRAMEWORK
                                .Replace(fullPackageOutputPath, installedBuildOutputTargetFolder);
#else
                                .Replace(fullPackageOutputPath, installedBuildOutputTargetFolder, StringComparison.CurrentCulture);
#endif
                    (var version, _, var differences) = LibraryComparison.Analyze(oldDll, currentDll, previousStringVersions, getVersionSuffix(default));
                    calculatedVersion = calculatedVersion.Max(version);
                    WriteChanges(output, differences);
                }

                System.IO.Directory.Delete(installDir, recursive: true);
            }

            if (output.HasFlag(OutputTypes.Diagnostic))
            {
                logger.LogTrace(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.Calculated, projectName, calculatedVersion));
            }

            return calculatedVersion;

            bool IsNullOrEmpty([System.Diagnostics.CodeAnalysis.NotNullWhen(false)] NuGet.Versioning.SemanticVersion? version)
            {
                return version?.Equals(Empty) != false;
            }

            async Task<string?> TryInstallAsync(System.Collections.Generic.IEnumerable<string> packageIds, string projectDirectory)
            {
                var previousVersion = IsNullOrEmpty(previous)
                    ? default
                    : previous;
                NuGet.Common.ILogger? logger = default;
                try
                {
                    return await NuGetInstaller.InstallAsync(packageIds, source, version: previousVersion, noCache: noCache, directDownload: directDownload, log: logger, root: projectDirectory).ConfigureAwait(false);
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

        /// <summary>
        /// Writes the changes.
        /// </summary>
        /// <param name="outputTypes">The output type.</param>
        /// <param name="differences">The differences.</param>
        public static void WriteChanges(OutputTypes outputTypes, Endjin.ApiChange.Api.Diff.AssemblyDiffCollection differences)
        {
            var breakingChanges = outputTypes.HasFlag(OutputTypes.BreakingChanges);
            var functionalChanges = outputTypes.HasFlag(OutputTypes.FunctionalChanges);
            if (!breakingChanges
                && !functionalChanges)
            {
                return;
            }

            void PrintBreakingChange(Endjin.ApiChange.Api.Diff.DiffOperation operation, string message, int tabs = 0)
            {
                if (breakingChanges && operation.IsRemoved)
                {
                    WriteLine(ConsoleColor.Red, message, tabs);
                }
            }

            void PrintFunctionalChange(Endjin.ApiChange.Api.Diff.DiffOperation operation, string message, int tabs = 0)
            {
                if (functionalChanges && operation.IsAdded)
                {
                    WriteLine(ConsoleColor.Blue, message, tabs);
                }
            }

            void PrintDiff<T>(Endjin.ApiChange.Api.Diff.DiffResult<T> diffResult, int tabs = 0)
            {
                var message = $"{diffResult}";
                PrintFunctionalChange(diffResult.Operation, message, tabs);
                PrintBreakingChange(diffResult.Operation, message, tabs);
            }

            var originalColour = Console.ForegroundColor;
            void WriteLine(ConsoleColor consoleColor, string value, int tabs = 0)
            {
                Console.ForegroundColor = consoleColor;
                Console.WriteLine(string.Concat(new string('\t', tabs), value));
                Console.ForegroundColor = originalColour;
            }

            bool ShouldPrintChangedBaseType(bool changedBaseType)
            {
                return breakingChanges && changedBaseType;
            }

            bool ShouldPrintChangedTypes(System.Collections.Generic.IList<Endjin.ApiChange.Api.Diff.TypeDiff> typeDifferences)
            {
                return typeDifferences.Any(ShouldPrintChangedType);
            }

            bool ShouldPrintChanged<T>(Endjin.ApiChange.Api.Diff.DiffCollection<T> collection)
            {
                return (breakingChanges && collection.Any(method => method.Operation.IsRemoved))
                    || (functionalChanges && collection.Any(method => method.Operation.IsAdded));
            }

            bool ShouldPrintChangedType(Endjin.ApiChange.Api.Diff.TypeDiff typeDiff)
            {
                return ShouldPrintChangedBaseType(typeDiff.HasChangedBaseType)
                    || ShouldPrintChanged(typeDiff.Methods)
                    || ShouldPrintChanged(typeDiff.Fields)
                    || ShouldPrintChanged(typeDiff.Events)
                    || ShouldPrintChanged(typeDiff.Interfaces);
            }

            bool ShouldPrintCollection(Endjin.ApiChange.Api.Diff.AssemblyDiffCollection assemblyDiffCollection)
            {
                return (breakingChanges && assemblyDiffCollection.AddedRemovedTypes.RemovedCount != 0)
                    || (functionalChanges && assemblyDiffCollection.AddedRemovedTypes.AddedCount != 0)
                    || ShouldPrintChangedTypes(assemblyDiffCollection.ChangedTypes);
            }

            if (ShouldPrintCollection(differences))
            {
                foreach (var addedRemovedType in differences.AddedRemovedTypes)
                {
                    PrintDiff(addedRemovedType, 1);
                }

                if (ShouldPrintChangedTypes(differences.ChangedTypes))
                {
                    WriteLine(originalColour, Properties.Resources.ChangedTypes, 1);
                    foreach (var changedType in differences.ChangedTypes.Where(ShouldPrintChangedType))
                    {
                        WriteLine(originalColour, $"{changedType.TypeV1}", 2);
                        if (ShouldPrintChangedBaseType(changedType.HasChangedBaseType))
                        {
                            WriteLine(ConsoleColor.Red, Properties.Resources.ChangedBaseType, 3);
                        }

                        if (ShouldPrintChanged(changedType.Methods))
                        {
                            WriteLine(originalColour, Properties.Resources.Methods, 3);
                            foreach (var method in changedType.Methods)
                            {
                                PrintDiff(method, 4);
                            }
                        }

                        if (ShouldPrintChanged(changedType.Fields))
                        {
                            WriteLine(originalColour, Properties.Resources.Fields, 3);
                            foreach (var field in changedType.Fields)
                            {
                                PrintDiff(field, 4);
                            }
                        }

                        if (ShouldPrintChanged(changedType.Events))
                        {
                            WriteLine(originalColour, Properties.Resources.Events, 3);
                            foreach (var @event in changedType.Events)
                            {
                                PrintDiff(@event, 4);
                            }
                        }

                        if (ShouldPrintChanged(changedType.Interfaces))
                        {
                            WriteLine(originalColour, Properties.Resources.Interfaces, 3);
                            foreach (var @interface in changedType.Interfaces)
                            {
                                PrintDiff(@interface, 4);
                            }
                        }
                    }
                }
            }
        }
    }
}