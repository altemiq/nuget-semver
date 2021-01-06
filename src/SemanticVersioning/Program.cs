// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Altemiq.SemanticVersioning.Specs")]

namespace Altemiq.SemanticVersioning
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Parsing;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// The program class.
    /// </summary>
    internal static partial class Program
    {
        private const string DisableSemanticVersioningPropertyName = "DisableSemanticVersioning";

        private const string IsPackablePropertyName = "IsPackable";

        private const string MSBuildProjectNamePropertyName = "MSBuildProjectName";

        private const string AssemblyNamePropertyName = "AssemblyName";

        private const string PackageIdPropertyName = "PackageId";

        private const string TargetExtPropertyName = "TargetExt";

        private static readonly NuGet.Versioning.SemanticVersion Empty = new NuGet.Versioning.SemanticVersion(0, 0, 0);

        private delegate Task<int> ProcessProjectOrSolutionDelegate(
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
            bool noLogo,
            OutputTypes output,
            string buildNumberParameter,
            string versionSuffixParameter);

        private static Task<int> Main(string[] args)
        {
            var fileCommandBuilder = new CommandBuilder(new Command("file", "Calculated the differences between two assemblies") { Handler = System.CommandLine.Invocation.CommandHandler.Create(new Action<System.IO.FileInfo, System.IO.FileInfo, NuGet.Versioning.SemanticVersion, string, OutputTypes, string, string, bool>(FileFunction)) })
                .AddArgument(new Argument<System.IO.FileInfo>("first") { Description = "The first assembly" })
                .AddArgument(new Argument<System.IO.FileInfo>("second") { Description = "The second assembly" })
                .AddOption(new Option<string>(new string[] { "-b", "--build" }, "Ths build label"));

            var solutionCommandBuilder = new CommandBuilder(new Command("solution", "Calculates the version based on a solution file") { Handler = System.CommandLine.Invocation.CommandHandler.Create(new ProcessProjectOrSolutionDelegate(ProcessProjectOrSolution)) })
                .AddArgument(new Argument<System.IO.FileSystemInfo?>("projectOrSolution", GetFileSystemInformation) { Description = "The project or solution file to operate on. If a file is not specified, the command will search the current directory for one." })
                .AddOption(new Option<string?>(new string[] { "-c", "--configuration" }, "The configuration to use for analysing the project. The default for most projects is 'Debug'."))
                .AddOption(new Option<string?>("--platform", "The platform to use for analysing the project. The default for most projects is 'AnyCPU'."))
                .AddOption(new Option<string?>(new string[] { "-s", "--source" }, "Specifies the server URL.").WithArgumentName("SOURCE").WithArity(ArgumentArity.OneOrMore))
                .AddOption(new Option<bool>("--no-version-suffix", "Forces there to be no version suffix. This overrides --version-suffix"))
                .AddOption(new Option<string?>("--version-suffix", "Sets the pre-release value. If none is specified, the pre-release from the previous version is used.").WithArgumentName("VERSION_SUFFIX"))
                .AddOption(new Option<bool>("--no-cache", "Disable using the machine cache as the first package source."))
                .AddOption(new Option<bool>("--direct-download", "Download directly without populating any caches with metadata or binaries."))
                .AddOption(new Option<string?>("--package-id-regex", "The regular expression to match in the package id.").WithArgumentName("REGEX"))
                .AddOption(new Option<string>("--package-id-replace", "The text used to replace the match from --package-id-regex").WithArgumentName("VALUE"))
                .AddOption(new Option<string?>("--package-id", "The package ID to check for previous versions").WithArgumentName("PACKAGE_ID").WithArity(ArgumentArity.OneOrMore))
                .AddOption(new Option<string?>("--exclude", "A package ID to check exclude from analysis").WithArgumentName("PACKAGE_ID").WithArity(ArgumentArity.OneOrMore));

            var diffCommandBuilder = new CommandBuilder(new Command("diff", "Calculates the differences"))
                .AddCommand(fileCommandBuilder.Command)
                .AddCommand(solutionCommandBuilder.Command)
                .AddGlobalOption(new Option<NuGet.Versioning.SemanticVersion?>(new string[] { "-p", "--previous" }, "The previous version") { Argument = new Argument<NuGet.Versioning.SemanticVersion>(argumentResult => NuGet.Versioning.SemanticVersion.Parse(argumentResult.Tokens.Single().Value)) }.WithDefaultValue(null))
                .AddGlobalOption(new Option<string>("--build-number-parameter", "The parameter name for the build number").WithArgumentName("PARAMETER").WithDefaultValue("buildNumber"))
                .AddGlobalOption(new Option<string>("--version-suffix-parameter", "The parameter name for the version suffix").WithArgumentName("PARAMETER").WithDefaultValue("system.build.suffix"))
                .AddGlobalOption(new Option<OutputTypes>("--output", "The output type").WithArgumentName("OUTPUT_TYPE").WithDefaultValue(OutputTypes.TeamCity | OutputTypes.Diagnostic))
                .AddGlobalOption(new Option<bool>(new string[] { "/nologo", "--nologo" }, "Do not display the startup banner or the copyright message."));

            var commandLineBuilder = new CommandLineBuilder(new RootCommand(description: "Semantic Version generator"))
                .UseDefaults()
                .AddCommand(diffCommandBuilder.Command);

            return commandLineBuilder
                .Build()
                .InvokeAsync(args);

            static void FileFunction(
                System.IO.FileInfo first,
                System.IO.FileInfo second,
                NuGet.Versioning.SemanticVersion previous,
                string build,
                OutputTypes output,
                string buildNumberParameter,
                string versionSuffixParameter,
                bool noLogo)
            {
                if (!noLogo)
                {
                    WriteHeader();
                }

                var result = Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.Analyze(first.FullName, second.FullName, new[] { previous.ToString() }, build);
                WriteChanges(output, result.Differences);
                if (output.HasFlag(OutputTypes.TeamCity))
                {
                    WriteTeamCityVersion(NuGet.Versioning.SemanticVersion.Parse(result.VersionNumber), buildNumberParameter, versionSuffixParameter);
                }

                if (output.HasFlag(OutputTypes.Json))
                {
                    WriteJsonVersion(NuGet.Versioning.SemanticVersion.Parse(result.VersionNumber));
                }
            }

            static System.IO.FileSystemInfo? GetFileSystemInformation(ArgumentResult argumentResult)
            {
                var pathToken = argumentResult.Tokens.SingleOrDefault();
                if (pathToken is null || pathToken.Value is null)
                {
                    return default;
                }

                var path = pathToken.Value;
                if (System.IO.File.Exists(path) || System.IO.Directory.Exists(path))
                {
                    return (System.IO.File.GetAttributes(path) & System.IO.FileAttributes.Directory) != 0
                        ? (System.IO.FileSystemInfo)new System.IO.DirectoryInfo(path)
                        : new System.IO.FileInfo(path);
                }

                argumentResult.ErrorMessage = $"\"{pathToken}\" is not a valid file or directory";
                return default;
            }

            static Task<int> ProcessProjectOrSolution(
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
                bool noLogo,
                OutputTypes output,
                string buildNumberParameter,
                string versionSuffixParameter)
            {
                if (!noLogo)
                {
                    WriteHeader();
                }

                return ProcessProjectOrSolutionWithInstance(
                    projectOrSolution,
                    RegisterMSBuild(projectOrSolution),
                    configuration,
                    platform,
                    source,
                    packageId,
                    exclude,
                    packageIdRegex,
                    packageIdReplace,
                    versionSuffix,
                    previous,
                    noVersionSuffix,
                    noCache,
                    directDownload,
                    output,
                    buildNumberParameter,
                    versionSuffixParameter);
            }

            static async Task<int> ProcessProjectOrSolutionWithInstance(
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
                var version = new NuGet.Versioning.SemanticVersion(0, 0, 0);

                var packageIds = packageId ?? Enumerable.Empty<string>();
                var regex = string.IsNullOrEmpty(packageIdRegex) ? null : new System.Text.RegularExpressions.Regex(packageIdRegex);
                using var projectCollection = GetProjects(projectOrSolution, instance, configuration, platform);
                foreach (var project in projectCollection.LoadedProjects.Where(project =>
                    bool.TryParse(project.GetPropertyValue(IsPackablePropertyName), out var isPackable) && isPackable
                    && (!bool.TryParse(project.GetPropertyValue(DisableSemanticVersioningPropertyName), out var excludeFromSemanticVersioning) || !excludeFromSemanticVersioning)))
                {
                    var projectName = project.GetPropertyValue(MSBuildProjectNamePropertyName);
                    if (output.HasFlag(OutputTypes.Diagnostic))
                    {
                        Console.WriteLine(Properties.Resources.Checking, projectName);
                    }

                    var projectPackageId = project.GetPropertyValue(PackageIdPropertyName);
                    if (exclude?.Contains(projectPackageId) == true)
                    {
                        continue;
                    }

                    var projectDirectory = project.DirectoryPath;
                    var assemblyName = project.GetPropertyValue(AssemblyNamePropertyName);

                    // install the NuGet package
                    var projectPackageIds = new[] { projectPackageId }.Union(packageIds);
                    if (regex != null)
                    {
                        projectPackageIds = projectPackageIds.Union(new[] { regex.Replace(projectPackageId, packageIdReplace) });
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
                        var frameworks = currentFrameworks.Intersect(previousFrameworks);
                        if (previousFrameworks.Except(currentFrameworks).Any())
                        {
                            // we have removed frameworks, this is a breaking change
                            calculatedVersion = Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.CreateBreakingChange(previousStringVersions, GetVersionSuffix());
                        }
                        else if (currentFrameworks.Except(previousFrameworks).Any())
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

                        System.IO.Directory.Delete(installDir, true);
                    }

                    if (output.HasFlag(OutputTypes.Diagnostic))
                    {
                        Console.WriteLine(Properties.Resources.Calculated, projectName, calculatedVersion);
                    }

                    version = Max(version, calculatedVersion);
                }

                // write out the version and the suffix
                if (output.HasFlag(OutputTypes.TeamCity))
                {
                    WriteTeamCityVersion(version, buildNumberParameter, versionSuffixParameter);
                }

                if (output.HasFlag(OutputTypes.Json))
                {
                    WriteJsonVersion(version);
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
            }

            static void WriteHeader()
            {
                Console.WriteLine(Properties.Resources.Logo, VersionUtils.GetVersion());
                Console.WriteLine(Properties.Resources.Copyright);
            }

            static Microsoft.Build.Locator.VisualStudioInstance RegisterMSBuild(System.IO.FileSystemInfo projectOrSolution)
            {
                (Microsoft.Build.Locator.VisualStudioInstance Instance, NuGet.Versioning.SemanticVersion Version) instance = default;
                var globalJson = FindGlobalJson(projectOrSolution);
                if (globalJson is not null)
                {
                    // get the tool version
                    var instances = Microsoft.Build.Locator.MSBuildLocator
                        .QueryVisualStudioInstances()
                        .Select(instance => (Instance: instance, Version: new SemanticVersion(instance.Version)))
                        .ToArray();

                    var allowPrerelease = false;
                    var jsonDocument = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(globalJson));
                    if (jsonDocument.RootElement.TryGetProperty("sdk", out var sdkElement))
                    {
                        var requestedVersion = sdkElement.TryGetProperty("version", out var versionElement) && NuGet.Versioning.SemanticVersion.TryParse(versionElement.GetString(), out var tempVersion)
                            ? tempVersion
                            : default;
                        allowPrerelease = sdkElement.TryGetProperty("allowPrerelease", out var tempAllowPrerelease) && tempAllowPrerelease.GetBoolean();
                        if (requestedVersion != null)
                        {
                            instance = Array.Find(instances, instance => NuGet.Versioning.VersionComparer.VersionRelease.Equals(instance.Version, requestedVersion));
                            if (instance.Instance is null)
                            {
                                // find the patch version
                                var validInstances = instances
                                    .Where(instance => instance.Version.Major == requestedVersion.Major
                                        && instance.Version.Minor == requestedVersion.Minor
                                        && (instance.Version.Patch / 100) == (requestedVersion.Patch / 100)
                                        && (instance.Version.Patch % 100) >= (requestedVersion.Patch % 100))
                                    .ToArray();

                                var maxVersion = validInstances.Length > 0
                                    ? validInstances.Max(instance => instance.Version)
                                    : throw new Exception($"A compatible installed dotnet SDK for global.json version: [{requestedVersion}] from [{globalJson}] was not found{Environment.NewLine}Please install the [{requestedVersion}] SDK up update [{globalJson}] with an installed dotnet SDK:{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", instances.Select(instance => $"{instance.Instance.Version} [{instance.Instance.MSBuildPath}]"))}");
                                instance = Array.Find(instances, instance => NuGet.Versioning.VersionComparer.VersionRelease.Equals(instance.Version, maxVersion));
                            }
                        }
                    }
                }

                if (instance.Instance is not null)
                {
                    Microsoft.Build.Locator.MSBuildLocator.RegisterInstance(instance.Instance);
                    return instance.Instance;
                }

                return Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();
            }

            static Microsoft.Build.Evaluation.ProjectCollection GetProjects(System.IO.FileSystemInfo projectOrSolution, Microsoft.Build.Locator.VisualStudioInstance instance, string? configuration, string? platform)
            {
                // get the highest version
                var toolsets = Microsoft.Build.Locator.MSBuildLocator.QueryVisualStudioInstances()
                    .Select(instance =>
                    {
                        var path = instance.MSBuildPath;
                        var properties = new System.Collections.Generic.Dictionary<string, string>
                        {
                            { "MSBuildSDKsPath", System.IO.Path.Combine(path, "Sdks") },
                            { "RoslynTargetsPath", System.IO.Path.Combine(path, "Roslyn") },
                            { "MSBuildExtensionsPath", path },
                        };

                        var propsFile = System.IO.Directory.EnumerateFiles(path, "Microsoft.Common.props", System.IO.SearchOption.AllDirectories).First();
                        var currentToolsVersion = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(propsFile));
                        return (instance.Version, ToolSet: new Microsoft.Build.Evaluation.Toolset(currentToolsVersion, path, properties, Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection, path));
                    })
                    .ToDictionary(val => val.Version, val => val.ToolSet);

                var toolset = toolsets[instance.Version];
                var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
                projectCollection.AddToolset(toolset);
                projectCollection.DefaultToolsVersion = toolset.ToolsVersion;

                var projectOrSolutionPath = GetPath(projectOrSolution ?? new System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory()), projectOrSolution is null);
                var solution = string.Equals(projectOrSolutionPath.Extension, ".sln", StringComparison.OrdinalIgnoreCase)
                    ? Microsoft.Build.Construction.SolutionFile.Parse(projectOrSolutionPath.FullName)
                    : default;

                (string? ConfigurationName, string? PlatformName, bool IncludeInBuild) GetBuildConfiguration(string path)
                {
                    if (solution is null)
                    {
                        return (configuration, platform, true);
                    }

                    // get the project in solution
                    var projectInSolution = solution.ProjectsInOrder.First(p => p.AbsolutePath == path);
                    var configurationName = configuration ?? solution.GetDefaultConfigurationName();
                    var platformName = platform ?? solution.GetDefaultPlatformName();

                    var solutionConfiguration = solution.SolutionConfigurations.First(c => c.ConfigurationName == configurationName && c.PlatformName == platformName);

                    var projectConfiguration = projectInSolution.ProjectConfigurations[solutionConfiguration.FullName];

                    return (projectConfiguration.ConfigurationName, projectConfiguration.PlatformName, projectConfiguration.IncludeInBuild);
                }

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

                    projectCollection.LoadProject(projectPath, globalProperties, toolset.ToolsVersion);
                }

                return projectCollection;
            }

            static string? FindGlobalJson(System.IO.FileSystemInfo? path)
            {
                var directory = path switch
                {
                    System.IO.DirectoryInfo directoryInfo => directoryInfo.FullName,
                    System.IO.FileInfo fileInfo => fileInfo.DirectoryName,
                    _ => System.IO.Directory.GetCurrentDirectory(),
                };

                while (directory is not null)
                {
                    var filePath = System.IO.Path.Combine(directory, "global.json");
                    if (System.IO.File.Exists(filePath))
                    {
                        return filePath;
                    }

                    directory = System.IO.Path.GetDirectoryName(directory);
                }

                return default;
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

        private static Option<T> WithArgumentName<T>(this Option<T> option, string name)
        {
            option.Argument.Name = name;
            return option;
        }

        private static Option<T> WithDefaultValue<T>(this Option<T> option, T value)
        {
            option.Argument.SetDefaultValue(value);
            return option;
        }

        private static Option<T> WithArity<T>(this Option<T> option, IArgumentArity arity)
        {
            option.Argument.Arity = arity;
            return option;
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

            properties ??= new System.Collections.Generic.Dictionary<string, string>();
            properties.Add(name, value);
            return properties;
        }

        private sealed class SemanticVersion : NuGet.Versioning.SemanticVersion
        {
            public SemanticVersion(System.Version version)
                : base(version)
            {
            }
        }
    }
}