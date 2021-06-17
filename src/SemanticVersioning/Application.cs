// -----------------------------------------------------------------------
// <copyright file="Application.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Altemiq.SemanticVersioning.Specs")]

namespace Altemiq.SemanticVersioning
{
    using System;
    using System.CommandLine.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// The application class.
    /// </summary>
    internal static partial class Application
    {
        /// <summary>
        /// The default for the configuration option.
        /// </summary>
        public const string? DefaultConfiguration = default;

        /// <summary>
        /// The default for the platform option.
        /// </summary>
        public const string? DefaultPlatform = default;

        /// <summary>
        /// The default for the package ID Regex option.
        /// </summary>
        public const string? DefaultPackageIdRegex = default;

        /// <summary>
        /// The default for the package ID replace option.
        /// </summary>
        public const string DefaultPackageIdReplace = default;

        /// <summary>
        /// The default for the version suffix option.
        /// </summary>
        public const string? DefaultVersionSuffix = "";

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

        private const string DisableSemanticVersioningPropertyName = "DisableSemanticVersioning";

        private const string IsPackablePropertyName = "IsPackable";

        private const string MSBuildProjectNamePropertyName = "MSBuildProjectName";

        private const string AssemblyNamePropertyName = "AssemblyName";

        private const string PackageIdPropertyName = "PackageId";

        private const string TargetExtPropertyName = "TargetExt";

        private static readonly NuGet.Versioning.SemanticVersion Empty = new(0, 0, 0);

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
            string packageIdReplace = DefaultPackageIdReplace,
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
            if (!noLogo)
            {
                WriteHeader(console);
            }

            var result = Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.Analyze(first.FullName, second.FullName, new[] { previous.ToString() }, build);
            WriteChanges(output, result.Differences);
            if (output.HasFlag(OutputTypes.TeamCity))
            {
                WriteTeamCityVersion(console, NuGet.Versioning.SemanticVersion.Parse(result.VersionNumber), buildNumberParameter, versionSuffixParameter);
            }

            if (output.HasFlag(OutputTypes.Json))
            {
                WriteJsonVersion(console, NuGet.Versioning.SemanticVersion.Parse(result.VersionNumber));
            }
        }

        /// <inheritdoc cref="ProcessProjectOrSolutionDelegate" />
        public static Task<int> ProcessProjectOrSolution(
            System.CommandLine.IConsole console,
            System.IO.FileSystemInfo projectOrSolution,
            System.Collections.Generic.IEnumerable<string> source,
            System.Collections.Generic.IEnumerable<string> packageId,
            System.Collections.Generic.IEnumerable<string> exclude,
            string? configuration = DefaultConfiguration,
            string? platform = DefaultPlatform,
            string? packageIdRegex = DefaultPackageIdRegex,
            string packageIdReplace = DefaultPackageIdReplace,
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

            return ProcessProjectOrSolutionWithInstance(
                console,
                projectOrSolution,
                RegisterMSBuild(projectOrSolution),
                configuration,
                platform,
                source,
                packageId,
                exclude,
                packageIdRegex,
                packageIdReplace,
                string.IsNullOrEmpty(versionSuffix) ? default : versionSuffix,
                previous,
                noVersionSuffix,
                noCache,
                directDownload,
                output,
                buildNumberParameter,
                versionSuffixParameter);

            static async Task<int> ProcessProjectOrSolutionWithInstance(
                System.CommandLine.IConsole console,
                System.IO.FileSystemInfo projectOrSolution,
                Microsoft.Build.Locator.VisualStudioInstance instance,
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
                OutputTypes output,
                string buildNumberParameter,
                string versionSuffixParameter)
            {
                if (output.HasFlag(OutputTypes.Diagnostic))
                {
                    console.Out.WriteLine($"Using {instance.Name} {instance.Version}");
                }

                var version = new NuGet.Versioning.SemanticVersion(0, 0, 0);

                var packageIds = packageId ?? Enumerable.Empty<string>();
                var regex = string.IsNullOrEmpty(packageIdRegex)
                    ? null
                    : new System.Text.RegularExpressions.Regex(packageIdRegex, System.Text.RegularExpressions.RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(3));
                using var projectCollection = GetProjects(projectOrSolution, configuration, platform);
                foreach (var project in projectCollection.LoadedProjects.Where(project =>
                    bool.TryParse(project.GetPropertyValue(IsPackablePropertyName), out var isPackable) && isPackable
                    && (!bool.TryParse(project.GetPropertyValue(DisableSemanticVersioningPropertyName), out var excludeFromSemanticVersioning) || !excludeFromSemanticVersioning)))
                {
                    var projectName = project.GetPropertyValue(MSBuildProjectNamePropertyName);
                    if (output.HasFlag(OutputTypes.Diagnostic))
                    {
                        console.Out.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.Checking, projectName));
                    }

                    var projectPackageId = project.GetPropertyValue(PackageIdPropertyName);
                    if (exclude?.Contains(projectPackageId, StringComparer.Ordinal) == true)
                    {
                        continue;
                    }

                    var projectDirectory = project.DirectoryPath;
                    var assemblyName = project.GetPropertyValue(AssemblyNamePropertyName);

                    // install the NuGet package
                    var projectPackageIds = new[] { projectPackageId }.Union(packageIds, StringComparer.Ordinal);
                    if (regex is not null)
                    {
                        projectPackageIds = projectPackageIds.Union(new[] { regex.Replace(projectPackageId, packageIdReplace) }, StringComparer.Ordinal);
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
                            ? new NuGet.Versioning.SemanticVersion(1, 0, 0, GetVersionSuffix(Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.DefaultAlphaRelease)) // have this as being a 0.1.0 release
                            : new NuGet.Versioning.SemanticVersion(previousVersion.Major, previousVersion.Minor, previousVersion.Patch + 1, GetVersionSuffix(previousVersion.Release));
                    }
                    else
                    {
                        var buildOutputTargetFolder = TrimEndingDirectorySeparator(System.IO.Path.Combine(installDir, project.GetPropertyValue("BuildOutputTargetFolder")));

                        var targetExt = project.GetProperty(TargetExtPropertyName)?.EvaluatedValue ?? ".dll";
                        var previousStringVersions = await previousVersions.Select(previousVersion => previousVersion.ToString()).ToArrayAsync().ConfigureAwait(false);

                        // Get the package output path
                        var packageOutputPath = TrimEndingDirectorySeparator(System.IO.Path.Combine(project.DirectoryPath, project.GetPropertyValue("PackageOutputPath").Replace('\\', System.IO.Path.DirectorySeparatorChar)));

                        // check the frameworks
                        var currentFrameworks = System.IO.Directory.EnumerateDirectories(packageOutputPath).Select(System.IO.Path.GetFileName).ToArray();
                        var previousFrameworks = System.IO.Directory.EnumerateDirectories(buildOutputTargetFolder).Select(System.IO.Path.GetFileName).ToArray();
                        var frameworks = currentFrameworks.Intersect(previousFrameworks, StringComparer.OrdinalIgnoreCase);
                        if (previousFrameworks.Except(currentFrameworks, StringComparer.OrdinalIgnoreCase).Any())
                        {
                            // we have removed frameworks, this is a breaking change
                            calculatedVersion = Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.CreateBreakingChange(previousStringVersions, GetVersionSuffix());
                        }
                        else if (currentFrameworks.Except(previousFrameworks, StringComparer.OrdinalIgnoreCase).Any())
                        {
                            // we have added frameworks, this is a feature change
                            calculatedVersion = Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.CreateFeatureChange(previousStringVersions, GetVersionSuffix());
                        }

                        foreach (var currentDll in frameworks.SelectMany(framework => System.IO.Directory.EnumerateFiles(System.IO.Path.Combine(packageOutputPath, framework ?? string.Empty), assemblyName + targetExt, new System.IO.EnumerationOptions { RecurseSubdirectories = false })))
                        {
                            var result = Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.Analyze(currentDll.Replace(packageOutputPath, buildOutputTargetFolder, StringComparison.CurrentCulture), currentDll, previousStringVersions, GetVersionSuffix());
                            calculatedVersion = Max(calculatedVersion, NuGet.Versioning.SemanticVersion.Parse(result.VersionNumber));
                            WriteChanges(output, result.Differences);
                        }

                        System.IO.Directory.Delete(installDir, recursive: true);
                    }

                    if (output.HasFlag(OutputTypes.Diagnostic))
                    {
                        console.Out.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.Calculated, projectName, calculatedVersion));
                    }

                    version = Max(version, calculatedVersion);
                }

                // write out the version and the suffix
                if (output.HasFlag(OutputTypes.TeamCity))
                {
                    WriteTeamCityVersion(console, version, buildNumberParameter, versionSuffixParameter);
                }

                if (output.HasFlag(OutputTypes.Json))
                {
                    WriteJsonVersion(console, version);
                }

                return 0;

                bool IsNullOrEmpty([System.Diagnostics.CodeAnalysis.NotNullWhen(false)] NuGet.Versioning.SemanticVersion? version)
                {
                    return version?.Equals(Empty) != false;
                }

                string? GetVersionSuffix(string? previousVersionRelease = default)
                {
                    return noVersionSuffix ? string.Empty : (versionSuffix ?? previousVersionRelease);
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

                static NuGet.Versioning.SemanticVersion Max(NuGet.Versioning.SemanticVersion first, NuGet.Versioning.SemanticVersion second)
                {
                    return NuGet.Versioning.VersionComparer.VersionRelease.Compare(first, second) > 0 ? first : second;
                }

                static async System.Collections.Generic.IAsyncEnumerable<T> CreateAsyncEnumerable<T>(T value)
                {
                    await Task.CompletedTask.ConfigureAwait(false);
                    yield return value;
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
                            throw new CommandValidationException(Properties.Resources.ProjectFileDoesNotExist);
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
                                        throw new CommandValidationException(Properties.Resources.MultipleInCurrentFolder);
                                    }

                                    throw new CommandValidationException(string.Format(Properties.Resources.Culture, Properties.Resources.MultipleInSpecifiedFolder, path));
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
                                        throw new CommandValidationException(Properties.Resources.MultipleInCurrentFolder);
                                    }

                                    throw new CommandValidationException(string.Format(Properties.Resources.Culture, Properties.Resources.MultipleInSpecifiedFolder, path));
                                }

                                // At this point the path contains no solutions or projects, so throw an exception
                                throw new CommandValidationException(Properties.Resources.ProjectFileDoesNotExist);
                            case System.IO.FileInfo fileInfo when
                                string.Equals(fileInfo.Extension, ".sln", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(fileInfo.Extension, ".csproj", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(fileInfo.Extension, ".vbproj", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(fileInfo.Extension, ".fsproj", StringComparison.OrdinalIgnoreCase):
                                return fileInfo;
                        }

                        // At this point, we know the file passed in is not a valid project or solution
                        throw new CommandValidationException(Properties.Resources.ProjectFileDoesNotExist);
                    }
                }
            }

            static Microsoft.Build.Locator.VisualStudioInstance RegisterMSBuild(System.IO.FileSystemInfo projectOrSolution)
            {
                var finder = new VisualStudioInstanceFinder(GetInstances());
                var instance = finder.GetVisualStudioInstance(projectOrSolution);
                Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(instance);
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
