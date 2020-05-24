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

        private const string TargetFrameworkPropertyName = "TargetFramework";

        private const string IsPackablePropertyName = "IsPackable";

        private const string MSBuildProjectNamePropertyName = "MSBuildProjectName";

        private const string AssemblyNamePropertyName = "AssemblyName";

        private const string PackageIdPropertyName = "PackageId";

        private const string TargetExtPropertyName = "TargetExt";

        private static Task<int> Main(string[] args)
        {
            var buildNumberParameterOption = new Option<string>("--build-number-parameter", "The parameter name for the build number") { Argument = new Argument<string>("PARAMETER", () => "buildNumber") };
            var versionSuffixParameterOption = new Option<string>("--version-suffix-parameter", "The parameter name for the version suffix") { Argument = new Argument<string>("PARAMETER", () => "system.build.suffix") };
            var outputTypeOption = new Option<OutputTypes>("--output", "The output type") { Argument = new Argument<OutputTypes>("OUTPUT_TYPE", () => OutputTypes.TeamCity | OutputTypes.Diagnostic) };
            var noLogoOption = new Option<bool>(new string[] { "/nologo", "--nologo" }, "Do not display the startup banner or the copyright message.");
            var fileCommand = new CommandBuilder(new Command("file", "Calculated the differences between two assemblies"))
                .AddArgument(new Argument<System.IO.FileInfo>() { Name = "first", Description = "The first assembly" })
                .AddArgument(new Argument<System.IO.FileInfo>() { Name = "second", Description = "The second assembly" })
                .AddOption(new Option<NuGet.Versioning.SemanticVersion>(new string[] { "-p", "--previous" }, "The previous version") { Argument = new Argument<NuGet.Versioning.SemanticVersion>((argumentResult) => NuGet.Versioning.SemanticVersion.Parse(argumentResult.Tokens.Single().Value)) })
                .AddOption(new Option<string>(new string[] { "-b", "--build" }, "Ths build label"))
                .AddOption(outputTypeOption)
                .AddOption(buildNumberParameterOption)
                .AddOption(versionSuffixParameterOption)
                .AddOption(noLogoOption)
                .Command;

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

            Action<
                System.IO.FileInfo,
                System.IO.FileInfo,
                NuGet.Versioning.SemanticVersion,
                string,
                OutputTypes,
                string,
                string,
                bool> action = FileFunction;
            fileCommand.Handler = System.CommandLine.Binding.HandlerDescriptor.FromDelegate(action).GetCommandHandler();

            var solutionCommand = new CommandBuilder(new Command("solution", "Calculates the version based on a solution file"))
                .AddArgument(new Argument<System.IO.FileSystemInfo?>(GetFileSystemInformation) { Name = "projectOrSolution", Description = "The project or solution file to operate on. If a file is not specified, the command will search the current directory for one." })
                .AddOption(new Option<string>(new string[] { "-s", "--source" }, "Specifies the server URL.") { Argument = new Argument<string>("SOURCE") { Arity = ArgumentArity.OneOrMore } })
                .AddOption(new Option<bool>("--no-version-suffix", "Forces there to be no version suffix. This overrides --version-suffix"))
                .AddOption(new Option<string>("--version-suffix", "Sets the pre-release value. If none is specified, the pre-release from the previous version is used.") { Argument = new Argument<string>("VERSION_SUFFIX") })
                .AddOption(new Option<bool>("--no-cache", "Disable using the machine cache as the first package source."))
                .AddOption(new Option<bool>("--direct-download", "Download directly without populating any caches with metadata or binaries."))
                .AddOption(new Option<string>("--package-id-regex", "The regular expression to match in the package id.") { Argument = new Argument<string>("REGEX") })
                .AddOption(new Option<string>("--package-id-replace", "The text used to replace the match from --package-id-regex") { Argument = new Argument<string>("VALUE") })
                .AddOption(new Option<string>("--package-id", "The package ID to check for previous versions") { Argument = new Argument<string>("PACKAGE_ID") { Arity = ArgumentArity.OneOrMore } })
                .AddOption(new Option<NuGet.Versioning.SemanticVersion>(new string[] { "-p", "--previous" }, "The previous version") { Argument = new Argument<NuGet.Versioning.SemanticVersion>((argumentResult) => NuGet.Versioning.SemanticVersion.Parse(argumentResult.Tokens.Single().Value)) })
                .AddOption(new Option<string>("--exclude", "A package ID to check exclude from analysis") { Argument = new Argument<string>("PACKAGE_ID") { Arity = ArgumentArity.OneOrMore } })
                .AddOption(outputTypeOption)
                .AddOption(buildNumberParameterOption)
                .AddOption(versionSuffixParameterOption)
                .AddOption(noLogoOption)
                .Command;

            Func<
                System.IO.FileSystemInfo,
                System.Collections.Generic.IEnumerable<string>,
                System.Collections.Generic.IEnumerable<string>,
                System.Collections.Generic.IEnumerable<string>,
                string,
                string,
                string,
                NuGet.Versioning.SemanticVersion?,
                bool,
                bool,
                bool,
                bool,
                OutputTypes,
                string,
                string,
                Task<int>> func = ProcessProjectOrSolution;
            solutionCommand.Handler = System.CommandLine.Binding.HandlerDescriptor.FromDelegate(func).GetCommandHandler();

            var diffCommand = new CommandBuilder(new Command("diff", "Calculates the differences"))
                .AddCommand(fileCommand)
                .AddCommand(solutionCommand)
                .Command;

            return new CommandLineBuilder(new RootCommand(description: "Semantic Version generator"))
                .AddCommand(diffCommand)
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
        }

        private static System.IO.FileSystemInfo? GetFileSystemInformation(ArgumentResult argumentResult)
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

        private static async Task<int> ProcessProjectOrSolution(
            System.IO.FileSystemInfo projectOrSolution,
            System.Collections.Generic.IEnumerable<string> source,
            System.Collections.Generic.IEnumerable<string> packageId,
            System.Collections.Generic.IEnumerable<string> exclude,
            string packageIdRegex,
            string packageIdReplace,
            string versionSuffix,
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

            var version = new NuGet.Versioning.SemanticVersion(0, 0, 0);

            string? GetVersionSuffix(string? previousVersionRelease = default)
            {
                return noVersionSuffix ? string.Empty : (versionSuffix ?? previousVersionRelease);
            }

            async Task<string?> TryInstallAsync(System.Collections.Generic.IEnumerable<string> packageIds, string projectDirectory)
            {
                try
                {
                    return await NuGetInstaller.InstallAsync(packageIds, source, version: previous, noCache: noCache, directDownload: directDownload, root: projectDirectory).ConfigureAwait(false);
                }
                catch (NuGet.Protocol.PackageNotFoundProtocolException ex)
                {
                    Console.WriteLine("  {0}", ex.Message);
                    return default;
                }
            }

            static string TrimEndingDirectorySeparator(string path)
            {
                return path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            }

            var packageIds = packageId ?? Enumerable.Empty<string>();
            var regex = string.IsNullOrEmpty(packageIdRegex) ? null : new System.Text.RegularExpressions.Regex(packageIdRegex);
            using var projectCollection = GetProjects(projectOrSolution);
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

                static NuGet.Versioning.SemanticVersion Max(NuGet.Versioning.SemanticVersion first, NuGet.Versioning.SemanticVersion second)
                {
                    return NuGet.Versioning.VersionComparer.VersionRelease.Compare(first, second) > 0 ? first : second;
                }

                static async System.Collections.Generic.IAsyncEnumerable<T> Create<T>(T value)
                {
                    await Task.CompletedTask.ConfigureAwait(false);
                    yield return value;
                }

                var installDir = await TryInstallAsync(projectPackageIds, projectDirectory).ConfigureAwait(false);
                var previousVersions = previous is null
                    ? NuGetInstaller.GetLatestVersionsAsync(projectPackageIds, source, root: projectDirectory)
                    : Create(previous);
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

                    foreach (var framework in frameworks)
                    {
                        foreach (var currentDll in System.IO.Directory.EnumerateFiles(System.IO.Path.Combine(packageOutputPath, framework ?? string.Empty), assemblyName + targetExt, new System.IO.EnumerationOptions { RecurseSubdirectories = false }))
                        {
                            var nugetDll = currentDll.Replace(packageOutputPath, buildOutputTargetFolder, StringComparison.CurrentCulture);
                            var result = Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.Analyze(nugetDll, currentDll, previousStringVersions, GetVersionSuffix());
                            calculatedVersion = Max(calculatedVersion, NuGet.Versioning.SemanticVersion.Parse(result.VersionNumber));
                            WriteChanges(output, result.Differences);
                        }
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
        }

        private static void WriteHeader()
        {
            Console.WriteLine(Properties.Resources.Logo, VersionUtils.GetVersion());
            Console.WriteLine(Properties.Resources.Copyright);
        }

        private static Microsoft.Build.Evaluation.ProjectCollection GetProjects(System.IO.FileSystemInfo projectOrSolution)
        {
            // get the highest version
            var directory = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                ? System.IO.Path.Combine(Environment.ExpandEnvironmentVariables("%PROGRAMFILES%"), "dotnet", "sdk")
                : System.IO.Path.Combine(char.ToString(System.IO.Path.DirectorySeparatorChar), "usr", "share", "dotnet", "sdk");

            var parsedToolsets = System.IO.Directory.EnumerateDirectories(directory)
                .Where(path => NuGet.Versioning.SemanticVersion.TryParse(System.IO.Path.GetFileName(path), out _))
                .Select(path =>
                {
                    var version = NuGet.Versioning.SemanticVersion.Parse(System.IO.Path.GetFileName(path));
                    var properties = new System.Collections.Generic.Dictionary<string, string>
                        {
                            { "MSBuildSDKsPath", System.IO.Path.Combine(path, "Sdks") },
                            { "RoslynTargetsPath", System.IO.Path.Combine(path, "Roslyn") },
                            { "MSBuildExtensionsPath", path },
                        };

                    var propsFile = System.IO.Directory.EnumerateFiles(path, "Microsoft.Common.props", System.IO.SearchOption.AllDirectories).First();
                    var currentToolsVersion = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(propsFile));

                    return (toolsVersion: version.ToString(), toolset: new Microsoft.Build.Evaluation.Toolset(currentToolsVersion, path, properties, Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection, path));
                }).ToDictionary(value => value.toolsVersion, value => value.toolset);

            NuGet.Versioning.SemanticVersion? toolsVersion = default;
            var versions = parsedToolsets.Keys.Where(value => NuGet.Versioning.SemanticVersion.TryParse(value, out var _)).Select(NuGet.Versioning.SemanticVersion.Parse).ToArray();
            var globalJson = FindGlobalJson(projectOrSolution);
            if (globalJson != null)
            {
                // get the tool version
                if (System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(globalJson)).RootElement.TryGetProperty("sdk", out var sdkElement)
                    && sdkElement.TryGetProperty("version", out var versionElement)
                    && NuGet.Versioning.SemanticVersion.TryParse(versionElement.GetString(), out var requestedVersion))
                {
                    toolsVersion = Array.Find(versions, version => NuGet.Versioning.VersionComparer.VersionRelease.Equals(version, requestedVersion));
                    if (toolsVersion is null)
                    {
                        // find the patch version
                        var validVersions = versions.Where(version =>
                            version.Major == requestedVersion.Major
                            && version.Minor == requestedVersion.Minor
                            && (version.Patch / 100) == (requestedVersion.Patch / 100)
                            && (version.Patch % 100) >= (requestedVersion.Patch % 100)).ToArray();

                        toolsVersion = validVersions.Length > 0
                            ? validVersions.Max()
                            : throw new Exception($"A compatible installed dotnet SDK for global.json version: [{requestedVersion}] from [{globalJson}] was not found{Environment.NewLine}Please install the [{requestedVersion}] SDK up update [{globalJson}] with an installed dotnet SDK:{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", versions.Select(version => $"{version} [{directory}]"))}");
                    }
                }
            }

            toolsVersion ??= versions.Max(version => version);

            var toolset = parsedToolsets[toolsVersion.ToString()];

            Environment.SetEnvironmentVariable("MSBuildSDKsPath", toolset.GetProperty("MSBuildSDKsPath", null).EvaluatedValue);

            var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
            projectCollection.AddToolset(toolset);
            projectCollection.DefaultToolsVersion = toolset.ToolsVersion;

            var projectOrSolutionPath = GetPath(projectOrSolution ?? new System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory()), projectOrSolution is null);
            var solution = string.Equals(projectOrSolutionPath.Extension, ".sln", StringComparison.OrdinalIgnoreCase)
                ? Microsoft.Build.Construction.SolutionFile.Parse(projectOrSolutionPath.FullName)
                : default;

            (string ConfigurationName, string PlatformName, string FullName, bool IncludeInBuild) GetBuildConfiguration(string path)
            {
                if (solution is null)
                {
                    return ("Debug", "AnyCPU", "Debug|AnyCPU", true);
                }

                // get the project in solution
                var projectInSolution = solution.ProjectsInOrder.FirstOrDefault(p => p.AbsolutePath == path);
                var defaultConfigurationName = solution.GetDefaultConfigurationName();
                var defaultPlatformName = solution.GetDefaultPlatformName();

                var defaultSolutionConfiguration = solution.SolutionConfigurations.First(c => c.ConfigurationName == defaultConfigurationName && c.PlatformName == defaultPlatformName);

                var projectConfiguration = projectInSolution.ProjectConfigurations[defaultSolutionConfiguration.FullName];

                return (projectConfiguration.ConfigurationName, projectConfiguration.PlatformName, projectConfiguration.FullName, projectConfiguration.IncludeInBuild);
            }

            System.Collections.Generic.IEnumerable<string> projectPaths = solution is null
                ? new string[] { projectOrSolutionPath.FullName }
                : solution.ProjectsInOrder.Where(projectInSolution => projectInSolution.ProjectType == Microsoft.Build.Construction.SolutionProjectType.KnownToBeMSBuildFormat).Select(projectInSolution => projectInSolution.AbsolutePath).ToArray();

            foreach (var projectPath in projectPaths)
            {
                var (configurationName, platformName, _, includeInBuild) = GetBuildConfiguration(projectPath);
                if (!includeInBuild)
                {
                    continue;
                }

                // set the configuration and platform
                var globalProperties = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "Configuration", configurationName },
                    { "Platform", platformName },
                };

                projectCollection.LoadProject(projectPath, globalProperties, toolset.ToolsVersion);
            }

            return projectCollection;
        }

        private static string? FindGlobalJson(System.IO.FileSystemInfo? path)
        {
            var directory = path switch
            {
                System.IO.DirectoryInfo directoryInfo => directoryInfo.FullName,
                System.IO.FileInfo fileInfo => fileInfo.DirectoryName,
                _ => System.IO.Directory.GetCurrentDirectory(),
            };

            while (true)
            {
                var filePath = System.IO.Path.Combine(directory, "global.json");
                if (System.IO.File.Exists(filePath))
                {
                    return filePath;
                }

                var directoryName = System.IO.Path.GetDirectoryName(directory);
                if (directoryName is null)
                {
                    return default;
                }

                directory = directoryName;
            }
        }

        private static System.IO.FileInfo GetPath(System.IO.FileSystemInfo path, bool currentDirectory)
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