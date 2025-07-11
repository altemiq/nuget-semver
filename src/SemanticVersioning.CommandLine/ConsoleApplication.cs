﻿// -----------------------------------------------------------------------
// <copyright file="ConsoleApplication.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

using System.CommandLine.IO;

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
    /// The default for the commit count option.
    /// </summary>
    public const int DefaultCommitCount = 10;

    /// <summary>
    /// The default for the configuration option.
    /// </summary>
    public const string? DefaultConfiguration = default;

    /// <summary>
    /// The default for the platform option.
    /// </summary>
    public const string? DefaultPlatform = default;

    /// <summary>
    /// The default for the force option.
    /// </summary>
    public const bool DefaultForce = default;

    private const string DisableSemanticVersioningPropertyName = "DisableSemanticVersioning";

    private const string IsPackablePropertyName = "IsPackable";

    private const string MSBuildProjectNamePropertyName = "MSBuildProjectName";

    private const string AssemblyNamePropertyName = "AssemblyName";

    private const string PackageIdPropertyName = "PackageId";

    private const string TargetExtPropertyName = "TargetExt";

    /// <summary>
    /// The file function.
    /// </summary>
    /// <param name="console">The console.</param>
    /// <param name="first">The first assembly.</param>
    /// <param name="second">The second assembly.</param>
    /// <param name="build">The build label.</param>
    /// <param name="previous">The previous version.</param>
    /// <param name="output">The output.</param>
    /// <param name="buildNumberParameter">The build number parameter.</param>
    /// <param name="versionSuffixParameter">The version suffix parameter.</param>
    /// <param name="increment">The increment location.</param>
    /// <param name="noLogo">Set to <see langword="true"/> to not display the startup banner or the copyright message.</param>
    public static void FileFunction(
        System.CommandLine.IConsole console,
        FileInfo first,
        FileInfo second,
        string? build,
        NuGet.Versioning.SemanticVersion previous,
        OutputTypes output = DefaultOutput,
        string buildNumberParameter = DefaultBuildNumberParameter,
        string versionSuffixParameter = DefaultVersionSuffixParameter,
        SemanticVersionIncrement increment = default,
        bool noLogo = DefaultNoLogo)
    {
        if (!noLogo)
        {
            WriteHeader(console);
        }

        (var version, _, var differences) = LibraryComparison.Analyze(first.FullName, second.FullName, [previous.ToString()], build: build, increment: increment);

        var consoleWithOutput = ConsoleWithOutput.Create(console, output);
        WriteChanges(consoleWithOutput, differences);
        if (version is not null)
        {
            WriteTeamCityVersion(consoleWithOutput, version, buildNumberParameter, versionSuffixParameter);
            WriteJsonVersion(consoleWithOutput, version);
        }
    }

    /// <summary>
    /// The process project of solution delegate.
    /// </summary>
    /// <param name="console">The console.</param>
    /// <param name="projectOrSolution">The project or solution.</param>
    /// <param name="source">The source.</param>
    /// <param name="packageId">The package ID.</param>
    /// <param name="exclude">The packages to exclude.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="platform">The platform.</param>
    /// <param name="packageIdRegex">The package ID regex.</param>
    /// <param name="packageIdReplace">The value to replace in <paramref name="packageIdRegex"/>.</param>
    /// <param name="versionSuffix">The version suffix.</param>
    /// <param name="noVersionSuffix">Set to <see langword="true"/> to ignore <paramref name="versionSuffix"/>.</param>
    /// <param name="noCache">Do not send the packages to the cache.</param>
    /// <param name="directDownload">Bypass the package cache.</param>
    /// <param name="commitCount">The commit count.</param>
    /// <param name="previous">The previous version.</param>
    /// <param name="output">The output type.</param>
    /// <param name="buildNumberParameter">The parameter name for the build number.</param>
    /// <param name="versionSuffixParameter">The parameter name for the version suffix.</param>
    /// <param name="increment">The increment location.</param>
    /// <param name="noLogo">Set to <see langword="true"/> to not display the startup banner or the copyright message.</param>
    /// <param name="force">Set to <see langword="true"/> to force the computation of the version.</param>
    /// <returns>The task.</returns>
    public static async Task<int> ProcessProjectOrSolution(
        System.CommandLine.IConsole console,
        FileSystemInfo? projectOrSolution,
        IEnumerable<string> source,
        IEnumerable<string> packageId,
        IEnumerable<string> exclude,
        string? configuration,
        string? platform,
        string? packageIdRegex,
        string? packageIdReplace,
        string? versionSuffix,
        bool noVersionSuffix,
        bool noCache,
        bool directDownload,
        int commitCount,
        NuGet.Versioning.SemanticVersion? previous = DefaultPrevious,
        OutputTypes output = DefaultOutput,
        string buildNumberParameter = DefaultBuildNumberParameter,
        string versionSuffixParameter = DefaultVersionSuffixParameter,
        SemanticVersionIncrement increment = default,
        bool noLogo = DefaultNoLogo,
        bool force = DefaultForce)
    {
        if (!noLogo)
        {
            WriteHeader(console);
        }

        var instance = RegisterMSBuild(projectOrSolution);

        var consoleWithOutput = ConsoleWithOutput.Create(console, output);
        consoleWithOutput.Out.WriteLine(string.Create(System.Globalization.CultureInfo.CurrentCulture, $"Using {instance.Name} {instance.Version}"), OutputTypes.Diagnostic);

        var regex = string.IsNullOrEmpty(packageIdRegex)
            ? null
            : new System.Text.RegularExpressions.Regex(packageIdRegex, System.Text.RegularExpressions.RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(3));

        var version = await ProcessProjectOrSolutionCore(
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
            directDownload,
            commitCount,
            increment,
            force).ConfigureAwait(false);

        // write out the version and the suffix
        WriteTeamCityVersion(consoleWithOutput, version, buildNumberParameter, versionSuffixParameter);
        WriteJsonVersion(consoleWithOutput, version);

        return 0;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1172:Unused method parameters should be removed", Justification = "Checked")]
    private static async Task<NuGet.Versioning.SemanticVersion> ProcessProjectOrSolutionCore(
        IConsoleWithOutput console,
        FileSystemInfo? projectOrSolution,
        string? configuration,
        string? platform,
        IEnumerable<string> source,
        IEnumerable<string> packageId,
        IEnumerable<string> exclude,
        System.Text.RegularExpressions.Regex? packageIdRegex,
        string? packageIdReplace,
        string? versionSuffix,
        NuGet.Versioning.SemanticVersion? previous,
        bool noVersionSuffix,
        bool noCache,
        bool directDownload,
        int commitCount,
        SemanticVersionIncrement increment,
        bool force)
    {
        var globalVersion = new NuGet.Versioning.SemanticVersion(0, 0, 0);

        var packageIds = packageId ?? [];
        var referenceVersions = new List<PackageCommitIdentity>();
        foreach (var project in GetProjects(projectOrSolution, configuration, platform, exclude))
        {
            var calculatedVersion = await ProcessProject(
                console,
                project,
                source,
                packageIds,
                packageIdRegex,
                packageIdReplace,
                previous,
                referenceVersions,
                noCache,
                directDownload,
                commitCount,
                increment,
                force,
                GetVersionSuffix).ConfigureAwait(false);

            if (calculatedVersion?.HasVersion == true)
            {
                globalVersion = globalVersion.Max(calculatedVersion.Version);
                referenceVersions.Add(calculatedVersion);
            }
        }

        return globalVersion;

        string? GetVersionSuffix(string? previousVersionRelease = default)
        {
            return noVersionSuffix ? string.Empty : (versionSuffix ?? previousVersionRelease);
        }
    }

    private static async Task<PackageCommitIdentity> ProcessProject(
        IConsoleWithOutput console,
        Microsoft.Build.Evaluation.Project project,
        IEnumerable<string> source,
        IEnumerable<string> packageIds,
        System.Text.RegularExpressions.Regex? packageIdRegex,
        string? packageIdReplace,
        NuGet.Versioning.SemanticVersion? previous,
        IReadOnlyList<PackageCommitIdentity> referencePackageIds,
        bool noCache,
        bool directDownload,
        int commitCount,
        SemanticVersionIncrement increment,
        bool force,
        Func<string?, string?> getVersionSuffix)
    {
        var projectName = project.GetPropertyValue(MSBuildProjectNamePropertyName);
        console.Out.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.Checking, projectName), OutputTypes.Diagnostic);

        IList<string>? folderCommits = default;
        IList<string>? headCommits = default;
        if (!force)
        {
            var baseDir = GetBaseDirectory(project.DirectoryPath);
            if (baseDir is not null)
            {
                using var repository = new LibGit2Sharp.Repository(baseDir);
                try
                {
                    folderCommits = [.. GetCommits(repository, project.DirectoryPath, commitCount)];
                }
                catch (LibGit2Sharp.NotFoundException ex)
                {
                    // this indicates that the fetch is too shallow
                    throw new InvalidOperationException("Failed to find GIT commits. This indicates that the clone was too shallow.", ex);
                }

                headCommits = [.. GetHeadCommits(repository, folderCommits.FirstOrDefault())];
            }
        }

        var nugetLogger = console.Output.HasFlag(OutputTypes.Diagnostic)
            ? new NuGetConsole(console)
            : NuGet.Common.NullLogger.Instance;

        (var referenceVersion, var results, _) = await MSBuildApplication.ProcessProject(
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
            folderCommits ?? Array.Empty<string>(),
            headCommits ?? Array.Empty<string>(),
            referencePackageIds,
            noCache,
            directDownload,
            increment,
            getVersionSuffix,
            loggingHelper: null,
            nugetLogger).ConfigureAwait(false);

        foreach (var result in results)
        {
            WriteChanges(console, result.Differences);
        }

        console.Out.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.Calculated, projectName, referenceVersion.Version), OutputTypes.Diagnostic);

        return referenceVersion;
    }

    private static IEnumerable<Microsoft.Build.Evaluation.Project> GetDependentProjects(Microsoft.Build.Evaluation.Project project)
    {
        var projectReferences = project.Items.Where(projectItem => string.Equals(projectItem.ItemType, "ProjectReference", StringComparison.Ordinal));

        foreach (var path in projectReferences.Select(ProjectPath))
        {
            var referencedProject = project.ProjectCollection.LoadedProjects.SingleOrDefault(p => string.Equals(p.FullPath, path, StringComparison.Ordinal))
                ?? LoadProject(project.ProjectCollection, path, default, default, default);
            if (referencedProject is not null)
            {
                yield return referencedProject;

                foreach (var subproject in GetDependentProjects(referencedProject))
                {
                    yield return subproject;
                }
            }
        }

        static string ProjectPath(Microsoft.Build.Evaluation.ProjectItem projectReference)
        {
            var path = projectReference.EvaluatedInclude;
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(projectReference.Project.DirectoryPath, path);
            }

            return Path.GetFullPath(path);
        }
    }

    private static void WriteHeader(System.CommandLine.IConsole console)
    {
        console.Out.WriteLine(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.Logo, VersionUtils.GetVersion()));
        console.Out.WriteLine(Properties.Resources.Copyright);
    }

    private static IDictionary<string, string>? AddProperty(
        this IDictionary<string, string>? properties,
        string name,
        string? value)
    {
        if (value is null)
        {
            return properties;
        }

        properties ??= new Dictionary<string, string>(StringComparer.Ordinal);
        properties.Add(name, value);
        return properties;
    }

    private static IEnumerable<string> GetCommits(LibGit2Sharp.Repository repository, string projectDir, int count)
    {
        var path = Path.GetFullPath(projectDir);
        if (path is null)
        {
            yield break;
        }

        string baseDir = repository.Info.WorkingDirectory;
        var relativePath = path
            .Substring(baseDir.Length)
            .Replace('\\', '/')
            .TrimStart('/');

        var logEntries = new FolderHistory(repository, relativePath) as IEnumerable<LibGit2Sharp.LogEntry>;

        if (count > 0)
        {
            logEntries = logEntries.Take(count);
        }

        foreach (var logEntry in logEntries)
        {
            yield return logEntry.Commit.Sha;
        }
    }

    private static IEnumerable<string> GetHeadCommits(LibGit2Sharp.Repository repository, string? commit)
    {
        foreach (var c in repository.Commits.TakeWhile(c => !string.Equals(c.Sha, commit, StringComparison.Ordinal)))
        {
            yield return c.Sha;
        }
    }

    private static string? GetBaseDirectory(string? directory)
    {
        while (directory is not null)
        {
            var git = Path.Combine(directory, ".git");
            if (Directory.Exists(git))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory);
        }

        return default;
    }

    private static IEnumerable<Microsoft.Build.Evaluation.Project> GetProjects(
        FileSystemInfo? projectOrSolution,
        string? configuration,
        string? platform,
        IEnumerable<string> exclude)
    {
        var ordered = OrderByDependencies(GetProjects(projectOrSolution, configuration, platform).LoadedProjects);

        foreach (var project in ordered.Where(project =>
            IsPackable(project)
            && !ShouldDisableSemanticVersioning(project)
            && !ShouldExclude(project, exclude)))
        {
            yield return project;
        }

        static IEnumerable<Microsoft.Build.Evaluation.Project> OrderByDependencies(ICollection<Microsoft.Build.Evaluation.Project> projects)
        {
            var ordered = new List<ProjectWithDependencies>();
            foreach (var project in projects
                .Select(project => new ProjectWithDependencies(project, [.. GetDependentProjects(project)])))
            {
                var inserted = false;
                for (int i = 0; i < ordered.Count; i++)
                {
                    var compare = ProjectWithDependenciesComparer.Instance.Compare(project, ordered[i]);
                    if (compare == 0)
                    {
                        break;
                    }

                    if (compare == -1)
                    {
                        ordered.Insert(i, project);
                        inserted = true;
                        break;
                    }
                }

                if (!inserted)
                {
                    ordered.Add(project);
                }
            }

            return ordered.Select(project => project.Project);
        }

        static bool IsPackable(Microsoft.Build.Evaluation.Project project)
        {
            return bool.TryParse(project.GetPropertyValue(IsPackablePropertyName), out var isPackable) && isPackable;
        }

        static bool ShouldDisableSemanticVersioning(Microsoft.Build.Evaluation.Project project)
        {
            return bool.TryParse(project.GetPropertyValue(DisableSemanticVersioningPropertyName), out var excludeFromSemanticVersioning) && excludeFromSemanticVersioning;
        }

        static bool ShouldExclude(Microsoft.Build.Evaluation.Project project, IEnumerable<string> excludes)
        {
            return excludes?.Contains(project.GetPropertyValue(PackageIdPropertyName), StringComparer.Ordinal) == true;
        }

        static Microsoft.Build.Evaluation.ProjectCollection GetProjects(FileSystemInfo? projectOrSolution, string? configuration, string? platform)
        {
            var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
            var projectOrSolutionPath = GetPath(projectOrSolution ?? new DirectoryInfo(Directory.GetCurrentDirectory()), projectOrSolution is null);
            var solution = string.Equals(projectOrSolutionPath.Extension, ".sln", StringComparison.OrdinalIgnoreCase)
                ? Microsoft.Build.Construction.SolutionFile.Parse(projectOrSolutionPath.FullName)
                : default;

            IEnumerable<string> projectPaths = solution is null
                ? [projectOrSolutionPath.FullName]
                : solution.ProjectsInOrder
                    .Where(projectInSolution => projectInSolution.ProjectType == Microsoft.Build.Construction.SolutionProjectType.KnownToBeMSBuildFormat)
                    .Select(projectInSolution => projectInSolution.AbsolutePath)
                    .ToArray();

            foreach (var projectPath in projectPaths)
            {
                System.Diagnostics.Debug.WriteLine(projectPath);
                LoadProject(projectCollection, projectPath, solution, configuration, platform);
            }

            return projectCollection;

            static FileInfo GetPath(FileSystemInfo path, bool currentDirectory)
            {
                if (!path.Exists)
                {
                    throw new FileNotFoundException(Properties.Resources.ProjectFileDoesNotExist);
                }

                // If a directory was passed in, search for a .sln or .csproj file
                switch (path)
                {
                    case DirectoryInfo directoryInfo:
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
                                throw new FileLoadException(Properties.Resources.MultipleInCurrentFolder);
                            }

                            throw new FileLoadException(string.Format(Properties.Resources.Culture, Properties.Resources.MultipleInSpecifiedFolder, path));
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
                                throw new FileLoadException(Properties.Resources.MultipleInCurrentFolder);
                            }

                            throw new FileLoadException(string.Format(Properties.Resources.Culture, Properties.Resources.MultipleInSpecifiedFolder, path));
                        }

                        // At this point the path contains no solutions or projects, so throw an exception
                        throw new FileNotFoundException(Properties.Resources.ProjectFileDoesNotExist);
                    case FileInfo fileInfo when
                        string.Equals(fileInfo.Extension, ".sln", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(fileInfo.Extension, ".csproj", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(fileInfo.Extension, ".vbproj", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(fileInfo.Extension, ".fsproj", StringComparison.OrdinalIgnoreCase):
                        return fileInfo;
                }

                // At this point, we know the file passed in is not a valid project or solution
                throw new FileNotFoundException(Properties.Resources.ProjectFileDoesNotExist);
            }
        }
    }

    private static Microsoft.Build.Locator.VisualStudioInstance RegisterMSBuild(FileSystemInfo? projectOrSolution)
    {
        var finder = new VisualStudioInstanceFinder(GetInstances());
        var instance = finder.GetVisualStudioInstance(projectOrSolution);
        if (Microsoft.Build.Locator.MSBuildLocator.CanRegister)
        {
            Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(instance);
        }

        return instance;

        static IEnumerable<Microsoft.Build.Locator.VisualStudioInstance> GetInstances()
        {
            return Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances(new Microsoft.Build.Locator.VisualStudioInstanceQueryOptions
            {
                DiscoveryTypes = Microsoft.Build.Locator.DiscoveryType.DotNetSdk,
            });
        }
    }

    private static Microsoft.Build.Evaluation.Project? LoadProject(
        Microsoft.Build.Evaluation.ProjectCollection projectCollection,
        string projectPath,
        Microsoft.Build.Construction.SolutionFile? solution,
        string? configuration,
        string? platform)
    {
        var (configurationName, platformName, includeInBuild) = GetBuildConfiguration(projectPath);
        if (!includeInBuild)
        {
            return default;
        }

        var globalProperties = default(IDictionary<string, string>?)
            .AddProperty("Configuration", configurationName)
            .AddProperty("Platform", platformName);

        return projectCollection.LoadProject(projectPath, globalProperties, projectCollection.DefaultToolsVersion);

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
    }

    private sealed record class ProjectWithDependencies(Microsoft.Build.Evaluation.Project Project, IList<Microsoft.Build.Evaluation.Project> Dependencies);

    private sealed class ProjectWithDependenciesComparer : IComparer<ProjectWithDependencies>
    {
        public static readonly IComparer<ProjectWithDependencies> Instance = new ProjectWithDependenciesComparer();

        /// <inheritdoc/>
        int IComparer<ProjectWithDependencies>.Compare(ProjectWithDependencies? x, ProjectWithDependencies? y)
        {
            if (x is null)
            {
                if (y is null)
                {
                    return 0;
                }

                return 1;
            }

            if (y is null)
            {
                return -1;
            }

            if (string.Equals(x.Project.FullPath, y.Project.FullPath, StringComparison.Ordinal))
            {
                return 0;
            }

            if (x.Dependencies.Contains(y.Project))
            {
                return 1;
            }

            if (y.Dependencies.Contains(x.Project))
            {
                return -1;
            }

            return 1;
        }
    }
}