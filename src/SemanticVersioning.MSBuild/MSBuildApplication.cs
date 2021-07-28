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
        private const string DisableSemanticVersioningPropertyName = "DisableSemanticVersioning";

        private const string IsPackablePropertyName = "IsPackable";

        private const string MSBuildProjectNamePropertyName = "MSBuildProjectName";

        private const string AssemblyNamePropertyName = "AssemblyName";

        private const string PackageIdPropertyName = "PackageId";

        private const string TargetExtPropertyName = "TargetExt";

        private static readonly NuGet.Versioning.SemanticVersion Empty = new(0, 0, 0);

        /// <summary>
        /// The process project of solution.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="projectOrSolution">The project or solution.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="platform">The platform.</param>
        /// <param name="source">The NuGet source.</param>
        /// <param name="packageId">The package ID.</param>
        /// <param name="exclude">The values to exclude.</param>
        /// <param name="packageIdRegex">The package ID regex.</param>
        /// <param name="packageIdReplace">The package ID replacement value.</param>
        /// <param name="versionSuffix">The version suffix.</param>
        /// <param name="previous">The previous version.</param>
        /// <param name="noVersionSuffix">Set to <see langword="true"/> to force there to be no version suffix.</param>
        /// <param name="noCache">Set to <see langword="true"/> to disable using the machine cache as the first package source.</param>
        /// <param name="directDownload">Set to <see langword="true"/> to download directly without populating any caches with metadata or binaries.</param>
        /// <param name="output">The output type.</param>
        /// <returns>The task.</returns>
        public static async Task<NuGet.Versioning.SemanticVersion> ProcessProjectOrSolution(
            ILogger logger,
            System.IO.FileSystemInfo projectOrSolution,
            string? configuration,
            string? platform,
            System.Collections.Generic.IEnumerable<string> source,
            System.Collections.Generic.IEnumerable<string> packageId,
            System.Collections.Generic.IEnumerable<string> exclude,
            string? packageIdRegex,
            string packageIdReplace,
            string? versionSuffix,
            NuGet.Versioning.SemanticVersion? previous,
            bool noVersionSuffix,
            bool noCache,
            bool directDownload,
            OutputTypes output)
        {
            var globalVersion = new NuGet.Versioning.SemanticVersion(0, 0, 0);

            var packageIds = packageId ?? Enumerable.Empty<string>();
            var regex = string.IsNullOrEmpty(packageIdRegex)
                ? null
                : new System.Text.RegularExpressions.Regex(packageIdRegex, System.Text.RegularExpressions.RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(3));
            using var projectCollection = GetProjects(projectOrSolution, configuration, platform);
            foreach (var project in projectCollection.LoadedProjects.Where(project =>
                IsPackable(project)
                && !ShouldDisableSemanticVersioning(project)
                && !ShouldExclude(project, exclude)))
            {
                var calculatedVersion = await ProcessProject(
                    project,
                    source,
                    packageIds,
                    regex,
                    packageIdReplace,
                    logger,
                    output,
                    previous,
                    noCache,
                    directDownload,
                    GetVersionSuffix).ConfigureAwait(false);
                globalVersion = Max(globalVersion, calculatedVersion);
            }

            return globalVersion;

            static bool IsPackable(Microsoft.Build.Evaluation.Project project)
            {
                return bool.TryParse(project.GetPropertyValue(IsPackablePropertyName), out var isPackable) && isPackable;
            }

            static bool ShouldDisableSemanticVersioning(Microsoft.Build.Evaluation.Project project)
            {
                return bool.TryParse(project.GetPropertyValue(DisableSemanticVersioningPropertyName), out var excludeFromSemanticVersioning) && excludeFromSemanticVersioning;
            }

            static bool ShouldExclude(Microsoft.Build.Evaluation.Project project, System.Collections.Generic.IEnumerable<string> excludes)
            {
                return excludes?.Contains(project.GetPropertyValue(PackageIdPropertyName), StringComparer.Ordinal) == true;
            }

            static Microsoft.Build.Evaluation.ProjectCollection GetProjects(System.IO.FileSystemInfo projectOrSolution, string? configuration, string? platform)
            {
                var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
                var projectOrSolutionPath = GetPath(projectOrSolution ?? new System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory()), projectOrSolution is null);
                var solution = string.Equals(projectOrSolutionPath.Extension, ".sln", StringComparison.OrdinalIgnoreCase)
                    ? Microsoft.Build.Construction.SolutionFile.Parse(projectOrSolutionPath.FullName)
                    : default;

                System.Collections.Generic.IEnumerable<string> projectPaths = solution is null
                    ? new string[] { projectOrSolutionPath.FullName }
                    : solution.ProjectsInOrder.Where(projectInSolution => projectInSolution.ProjectType == Microsoft.Build.Construction.SolutionProjectType.KnownToBeMSBuildFormat).Select(projectInSolution => projectInSolution.AbsolutePath).ToArray();

                foreach (var projectPath in projectPaths)
                {
                    System.Diagnostics.Debug.WriteLine(projectPath);
                    var (configurationName, platformName, includeInBuild) = GetBuildConfiguration(projectPath);
                    if (!includeInBuild)
                    {
                        continue;
                    }

                    var globalProperties = default(System.Collections.Generic.IDictionary<string, string>?)
                        .AddProperty("Configuration", configurationName)
                        .AddProperty("Platform", platformName);

                    projectCollection.LoadProject(projectPath, globalProperties, projectCollection.DefaultToolsVersion);
                }

                return projectCollection;

                (string? ConfigurationName, string? PlatformName, bool IncludeInBuild) GetBuildConfiguration(string path)
                {
                    if (solution is null)
                    {
                        return (configuration, platform, IncludeInBuild: true);
                    }

                    // get the project in solution
                    var projectInSolution = solution.ProjectsInOrder.First(p => string.Equals(p.AbsolutePath, path, StringComparison.OrdinalIgnoreCase));
                    var configurationName = configuration ?? solution.GetDefaultConfigurationName();
                    var platformName = platform ?? solution.GetDefaultPlatformName();

                    var solutionConfiguration = solution.SolutionConfigurations.First(c => string.Equals(c.ConfigurationName, configurationName, StringComparison.OrdinalIgnoreCase) && string.Equals(c.PlatformName, platformName, StringComparison.OrdinalIgnoreCase));

                    var projectConfiguration = projectInSolution.ProjectConfigurations[solutionConfiguration.FullName];

                    return (projectConfiguration.ConfigurationName, projectConfiguration.PlatformName, projectConfiguration.IncludeInBuild);
                }

                static System.IO.FileInfo GetPath(System.IO.FileSystemInfo path, bool currentDirectory)
                {
                    if (!path.Exists)
                    {
                        throw new System.IO.FileNotFoundException(Properties.Resources.ProjectFileDoesNotExist);
                    }

                    // If a directory was passed in, search for a .sln or .csproj file
                    switch (path)
                    {
                        case System.IO.DirectoryInfo directoryInfo:
                            // Search for solution(s)
                            var solutionFiles = directoryInfo.GetFiles("*.sln");
                            if (solutionFiles.Length == 1)
                            {
                                return solutionFiles[0];
                            }

                            if (solutionFiles.Length > 1)
                            {
                                if (currentDirectory)
                                {
                                    throw new System.IO.FileLoadException(Properties.Resources.MultipleInCurrentFolder);
                                }

                                throw new System.IO.FileLoadException(string.Format(Properties.Resources.Culture, Properties.Resources.MultipleInSpecifiedFolder, path));
                            }

                            // We did not find any solutions, so try and find individual projects
                            var projectFiles = directoryInfo.EnumerateFiles("*.csproj")
                                .Concat(directoryInfo.EnumerateFiles("*.fsproj"))
                                .Concat(directoryInfo.EnumerateFiles("*.vbproj"))
                                .ToArray();
                            if (projectFiles.Length == 1)
                            {
                                return projectFiles[0];
                            }

                            if (projectFiles.Length > 1)
                            {
                                if (currentDirectory)
                                {
                                    throw new System.IO.FileLoadException(Properties.Resources.MultipleInCurrentFolder);
                                }

                                throw new System.IO.FileLoadException(string.Format(Properties.Resources.Culture, Properties.Resources.MultipleInSpecifiedFolder, path));
                            }

                            // At this point the path contains no solutions or projects, so throw an exception
                            throw new System.IO.FileNotFoundException(Properties.Resources.ProjectFileDoesNotExist);
                        case System.IO.FileInfo fileInfo when
                            string.Equals(fileInfo.Extension, ".sln", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(fileInfo.Extension, ".csproj", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(fileInfo.Extension, ".vbproj", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(fileInfo.Extension, ".fsproj", StringComparison.OrdinalIgnoreCase):
                            return fileInfo;
                    }

                    // At this point, we know the file passed in is not a valid project or solution
                    throw new System.IO.FileNotFoundException(Properties.Resources.ProjectFileDoesNotExist);
                }
            }

            string? GetVersionSuffix(string? previousVersionRelease = default)
            {
                return noVersionSuffix ? string.Empty : (versionSuffix ?? previousVersionRelease);
            }
        }

        /// <summary>
        /// The process project.
        /// </summary>
        /// <param name="project">The project.</param>
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
        public static Task<NuGet.Versioning.SemanticVersion> ProcessProject(
            Microsoft.Build.Evaluation.Project project,
            System.Collections.Generic.IEnumerable<string> source,
            System.Collections.Generic.IEnumerable<string> packageIds,
            System.Text.RegularExpressions.Regex? packageIdRegex,
            string packageIdReplace,
            ILogger logger,
            OutputTypes output,
            NuGet.Versioning.SemanticVersion? previous,
            bool noCache,
            bool directDownload,
            Func<string?, string?> getVersionSuffix) =>
            ProcessProject(
                project.GetPropertyValue(MSBuildProjectNamePropertyName),
                project.DirectoryPath,
                project.GetPropertyValue(AssemblyNamePropertyName),
                project.GetPropertyValue(PackageIdPropertyName),
                project.GetProperty(TargetExtPropertyName)?.EvaluatedValue ?? ".dll",
                project.GetPropertyValue("BuildOutputTargetFolder"),
                project.GetPropertyValue("PackageOutputPath"),
                source,
                packageIds,
                packageIdRegex,
                packageIdReplace,
                logger,
                output,
                previous,
                noCache,
                directDownload,
                getVersionSuffix);

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
                var searchOptions =
#if NETFRAMEWORK
                                System.IO.SearchOption.TopDirectoryOnly;
#else
                                new System.IO.EnumerationOptions { RecurseSubdirectories = false };
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
                    calculatedVersion = Max(calculatedVersion, version);
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

        private static NuGet.Versioning.SemanticVersion Max(NuGet.Versioning.SemanticVersion first, NuGet.Versioning.SemanticVersion? second)
        {
            if (second is null)
            {
                return first;
            }

            return NuGet.Versioning.VersionComparer.VersionRelease.Compare(first, second) > 0
                ? first
                : second;
        }

        private static System.Collections.Generic.IDictionary<string, string>? AddProperty(
            this System.Collections.Generic.IDictionary<string, string>? properties,
            string name,
            string? value)
        {
            if (value is null)
            {
                return properties;
            }

            properties ??= new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);
            properties.Add(name, value);
            return properties;
        }
    }
}