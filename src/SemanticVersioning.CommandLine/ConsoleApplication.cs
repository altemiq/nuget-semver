// -----------------------------------------------------------------------
// <copyright file="ConsoleApplication.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Altemiq.SemanticVersioning.CommandLine.Specs")]

namespace Altemiq.SemanticVersioning
{
    using System;
    using System.CommandLine.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// The console application.
    /// </summary>
    internal static partial class ConsoleApplication
    {
        /// <summary>
        /// The default for the no logo option.
        /// </summary>
        public const bool DefaultNoLogo = default;

        /// <summary>
        /// The default for the output option.
        /// </summary>
        public const OutputTypes DefaultOutput = OutputTypes.TeamCity | OutputTypes.Diagnostic;

        /// <summary>
        /// The default for the build number parameter option.
        /// </summary>
        public const string DefaultBuildNumberParameter = "buildNumber";

        /// <summary>
        /// The default for the version suffix parameter option.
        /// </summary>
        public const string DefaultVersionSuffixParameter = "system.build.suffix";

        /// <summary>
        /// The default for the package ID Regex option.
        /// </summary>
        public const string? DefaultPackageIdRegex = default;

        /// <summary>
        /// The default for the package ID replace option.
        /// </summary>
        public const string? DefaultPackageIdReplace = default;

        /// <summary>
        /// The default for the version suffix option.
        /// </summary>
        public const string DefaultVersionSuffix = "";

        /// <summary>
        /// The default for the previous version option.
        /// </summary>
        public const NuGet.Versioning.SemanticVersion? DefaultPrevious = default;

        /// <summary>
        /// The default for the no version suffix option.
        /// </summary>
        public const bool DefaultNoVersionSuffix = default;

        /// <summary>
        /// The default for the no cache option.
        /// </summary>
        public const bool DefaultNoCache = default;

        /// <summary>
        /// The default for the direct download option.
        /// </summary>
        public const bool DefaultDirectDownload = default;

        /// <summary>
        /// The default for the configuration option.
        /// </summary>
        public const string? DefaultConfiguration = default;

        /// <summary>
        /// The default for the platform option.
        /// </summary>
        public const string? DefaultPlatform = default;

        private const string DisableSemanticVersioningPropertyName = "DisableSemanticVersioning";

        private const string IsPackablePropertyName = "IsPackable";

        private const string MSBuildProjectNamePropertyName = "MSBuildProjectName";

        private const string AssemblyNamePropertyName = "AssemblyName";

        private const string PackageIdPropertyName = "PackageId";

        private const string TargetExtPropertyName = "TargetExt";

        private static bool isRegistered;

        /// <summary>
        /// The file function delegate.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="first">The first file.</param>
        /// <param name="second">The second file.</param>
        /// <param name="previous">The previous version.</param>
        /// <param name="build">The build.</param>
        /// <param name="output">The output.</param>
        /// <param name="buildNumberParameter">The build number parameter.</param>
        /// <param name="versionSuffixParameter">The version suffix parameter.</param>
        /// <param name="noLogo">Set to <see langword="true"/> to not display the startup banner or the copyright message.</param>
        public delegate void FileFunctionDelegate(
            System.CommandLine.IConsole console,
            System.IO.FileInfo first,
            System.IO.FileInfo second,
            NuGet.Versioning.SemanticVersion previous,
            string build,
            OutputTypes output = DefaultOutput,
            string buildNumberParameter = DefaultBuildNumberParameter,
            string versionSuffixParameter = DefaultVersionSuffixParameter,
            bool noLogo = DefaultNoLogo);

        /// <summary>
        /// The process project of solution delegate.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="projectOrSolution">The project or solution.</param>
        /// <param name="source">The NuGet source.</param>
        /// <param name="packageId">The package ID.</param>
        /// <param name="exclude">The values to exclude.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="platform">The platform.</param>
        /// <param name="packageIdRegex">The package ID regex.</param>
        /// <param name="packageIdReplace">The package ID replacement value.</param>
        /// <param name="versionSuffix">The version suffix.</param>
        /// <param name="previous">The previous version.</param>
        /// <param name="noVersionSuffix">Set to <see langword="true"/> to force there to be no version suffix.</param>
        /// <param name="noCache">Set to <see langword="true"/> to disable using the machine cache as the first package source.</param>
        /// <param name="directDownload">Set to <see langword="true"/> to download directly without populating any caches with metadata or binaries.</param>
        /// <param name="output">The output type.</param>
        /// <param name="buildNumberParameter">The parameter name for the build number.</param>
        /// <param name="versionSuffixParameter">The parameter name for the version suffix.</param>
        /// <param name="noLogo">Set to <see langword="true"/> to not display the startup banner or the copyright message.</param>
        /// <returns>The task.</returns>
        public delegate Task<int> ProcessProjectOrSolutionDelegate(
            System.CommandLine.IConsole console,
            System.IO.FileSystemInfo projectOrSolution,
            System.Collections.Generic.IEnumerable<string> source,
            System.Collections.Generic.IEnumerable<string> packageId,
            System.Collections.Generic.IEnumerable<string> exclude,
            string? configuration = DefaultConfiguration,
            string? platform = DefaultPlatform,
            string? packageIdRegex = DefaultPackageIdRegex,
            string? packageIdReplace = DefaultPackageIdReplace,
            string? versionSuffix = DefaultVersionSuffix,
            NuGet.Versioning.SemanticVersion? previous = DefaultPrevious,
            bool noVersionSuffix = DefaultNoVersionSuffix,
            bool noCache = DefaultNoCache,
            bool directDownload = DefaultDirectDownload,
            OutputTypes output = DefaultOutput,
            string buildNumberParameter = DefaultBuildNumberParameter,
            string versionSuffixParameter = DefaultVersionSuffixParameter,
            bool noLogo = DefaultNoLogo);

        /// <inheritdoc cref="FileFunctionDelegate" />
        public static void FileFunction(
            System.CommandLine.IConsole console,
            System.IO.FileInfo first,
            System.IO.FileInfo second,
            NuGet.Versioning.SemanticVersion previous,
            string build,
            OutputTypes output = DefaultOutput,
            string buildNumberParameter = DefaultBuildNumberParameter,
            string versionSuffixParameter = DefaultVersionSuffixParameter,
            bool noLogo = DefaultNoLogo)
        {
            var consoleWithOutput = console is System.CommandLine.Rendering.ITerminal terminal
                ? new TerminalWithOutput(terminal, output)
                : new ConsoleWithOutput(console, output);
            if (!noLogo)
            {
                WriteHeader(console);
            }

            (var version, _, var differences) = LibraryComparison.Analyze(first.FullName, second.FullName, new[] { previous.ToString() }, build);

            WriteChanges(consoleWithOutput, differences);
            if (version is not null)
            {
                WriteTeamCityVersion(consoleWithOutput, version, buildNumberParameter, versionSuffixParameter);
                WriteJsonVersion(consoleWithOutput, version);
            }
        }

        /// <inheritdoc cref="ProcessProjectOrSolutionDelegate" />
        public static async Task<int> ProcessProjectOrSolution(
            System.CommandLine.IConsole console,
            System.IO.FileSystemInfo projectOrSolution,
            System.Collections.Generic.IEnumerable<string> source,
            System.Collections.Generic.IEnumerable<string> packageId,
            System.Collections.Generic.IEnumerable<string> exclude,
            string? configuration = DefaultConfiguration,
            string? platform = DefaultPlatform,
            string? packageIdRegex = DefaultPackageIdRegex,
            string? packageIdReplace = DefaultPackageIdReplace,
            string? versionSuffix = DefaultVersionSuffix,
            NuGet.Versioning.SemanticVersion? previous = DefaultPrevious,
            bool noVersionSuffix = DefaultNoVersionSuffix,
            bool noCache = DefaultNoCache,
            bool directDownload = DefaultDirectDownload,
            OutputTypes output = DefaultOutput,
            string buildNumberParameter = DefaultBuildNumberParameter,
            string versionSuffixParameter = DefaultVersionSuffixParameter,
            bool noLogo = DefaultNoLogo)
        {
            if (!noLogo)
            {
                WriteHeader(console);
            }

            var instance = RegisterMSBuild(projectOrSolution);

            var consoleWithOutput = console is System.CommandLine.Rendering.ITerminal terminal
                ? new TerminalWithOutput(terminal, output)
                : new ConsoleWithOutput(console, output);
            consoleWithOutput.Out.WriteLine($"Using {instance.Name} {instance.Version}", OutputTypes.Diagnostic);

            var regex = string.IsNullOrEmpty(packageIdRegex)
                ? null
                : new System.Text.RegularExpressions.Regex(packageIdRegex, System.Text.RegularExpressions.RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(3));

            var version = await ProcessProjectOrSolution(
                consoleWithOutput,
                projectOrSolution,
                configuration,
                platform,
                source,
                packageId,
                exclude,
                regex,
                packageIdReplace,
                string.IsNullOrEmpty(versionSuffix) ? default : versionSuffix,
                previous,
                noVersionSuffix,
                noCache,
                directDownload).ConfigureAwait(false);

            // write out the version and the suffix
            WriteTeamCityVersion(consoleWithOutput, version, buildNumberParameter, versionSuffixParameter);
            WriteJsonVersion(consoleWithOutput, version);

            return 0;

            static Microsoft.Build.Locator.VisualStudioInstance RegisterMSBuild(System.IO.FileSystemInfo projectOrSolution)
            {
                var finder = new VisualStudioInstanceFinder(GetInstances());
                var instance = finder.GetVisualStudioInstance(projectOrSolution);
                if (!isRegistered)
                {
                    isRegistered = true;
                    Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(instance);
                }

                return instance;

                static System.Collections.Generic.IEnumerable<Microsoft.Build.Locator.VisualStudioInstance> GetInstances()
                {
                    return Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances(new Microsoft.Build.Locator.VisualStudioInstanceQueryOptions
                    {
                        DiscoveryTypes = Microsoft.Build.Locator.DiscoveryType.DotNetSdk,
                    });
                }
            }
        }

        /// <summary>
        /// The process project of solution.
        /// </summary>
        /// <param name="console">The console.</param>
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
        /// <returns>The task.</returns>
        public static async Task<NuGet.Versioning.SemanticVersion> ProcessProjectOrSolution(
            IConsoleWithOutput console,
            System.IO.FileSystemInfo projectOrSolution,
            string? configuration,
            string? platform,
            System.Collections.Generic.IEnumerable<string> source,
            System.Collections.Generic.IEnumerable<string> packageId,
            System.Collections.Generic.IEnumerable<string> exclude,
            System.Text.RegularExpressions.Regex? packageIdRegex,
            string? packageIdReplace,
            string? versionSuffix,
            NuGet.Versioning.SemanticVersion? previous,
            bool noVersionSuffix,
            bool noCache,
            bool directDownload)
        {
            var globalVersion = new NuGet.Versioning.SemanticVersion(0, 0, 0);

            var packageIds = packageId ?? Enumerable.Empty<string>();
            using var projectCollection = GetProjects(projectOrSolution, configuration, platform);
            foreach (var project in projectCollection.LoadedProjects.Where(project =>
                IsPackable(project)
                && !ShouldDisableSemanticVersioning(project)
                && !ShouldExclude(project, exclude)))
            {
                var calculatedVersion = await ProcessProject(
                    console,
                    project,
                    source,
                    packageIds,
                    packageIdRegex,
                    packageIdReplace,
                    previous,
                    noCache,
                    directDownload,
                    GetVersionSuffix).ConfigureAwait(false);
                globalVersion = globalVersion.Max(calculatedVersion);
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
        /// <param name="console">The console.</param>
        /// <param name="project">The project.</param>
        /// <param name="source">The NuGet source.</param>
        /// <param name="packageIds">The package ID.</param>
        /// <param name="packageIdRegex">The package ID regex.</param>
        /// <param name="packageIdReplace">The package ID replacement value.</param>
        /// <param name="previous">The previous version.</param>
        /// <param name="noCache">Set to <see langword="true"/> to disable using the machine cache as the first package source.</param>
        /// <param name="directDownload">Set to <see langword="true"/> to download directly without populating any caches with metadata or binaries.</param>
        /// <param name="getVersionSuffix">The function to get the version suffix.</param>
        /// <returns>The task.</returns>
        public static async Task<NuGet.Versioning.SemanticVersion> ProcessProject(
            IConsoleWithOutput console,
            Microsoft.Build.Evaluation.Project project,
            System.Collections.Generic.IEnumerable<string> source,
            System.Collections.Generic.IEnumerable<string> packageIds,
            System.Text.RegularExpressions.Regex? packageIdRegex,
            string? packageIdReplace,
            NuGet.Versioning.SemanticVersion? previous,
            bool noCache,
            bool directDownload,
            Func<string?, string?> getVersionSuffix)
        {
            var projectName = project.GetPropertyValue(MSBuildProjectNamePropertyName);
            console.Out.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.Checking, projectName), OutputTypes.Diagnostic);
            var commit = GetCommit(project.DirectoryPath);

            (var version, var results, var published) = await MSBuildApplication.ProcessProject(
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
                previous,
                commit,
                noCache,
                directDownload,
                getVersionSuffix).ConfigureAwait(false);

            foreach (var result in results)
            {
                WriteChanges(console, result.Differences);
            }

            console.Out.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.Calculated, projectName, version), OutputTypes.Diagnostic);

            return version;

            static string? GetCommit(string projectDir)
            {
                var path = System.IO.Path.GetFullPath(projectDir);
                if (path is null)
                {
                    return default;
                }

                var baseDir = GetBaseDirectory(path);
                if (baseDir is null)
                {
                    return default;
                }

                using (var repository = new LibGit2Sharp.Repository(baseDir))
                {
                    var relativePath = path
                        .Substring(baseDir.Length + 1)
                        .Replace("\\", "/", StringComparison.Ordinal);

                    if (repository.Commits.QueryBy(relativePath).Take(1).FirstOrDefault() is LibGit2Sharp.LogEntry logEntry)
                    {
                        return logEntry.Commit.Sha;
                    }
                }

                return default;

                static string? GetBaseDirectory(string? directory)
                {
                    while (directory is not null)
                    {
                        var git = System.IO.Path.Combine(directory, ".git");
                        if (System.IO.Directory.Exists(git))
                        {
                            return directory;
                        }

                        directory = System.IO.Path.GetDirectoryName(directory);
                    }

                    return default;
                }
            }
        }

        private static void WriteHeader(System.CommandLine.IConsole console)
        {
            console.Out.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.Logo, VersionUtils.GetVersion()));
            console.Out.WriteLine(Properties.Resources.Copyright);
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