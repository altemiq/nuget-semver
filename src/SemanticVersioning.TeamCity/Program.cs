// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Mondo.SemanticVersioning.TeamCity.Specs")]

namespace Mondo.SemanticVersioning.TeamCity
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Invocation;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// The program class.
    /// </summary>
    internal static class Program
    {
        private static Task<int> Main(string[] args)
        {
            var buildNumberParameterOption = new Option("--build-number-parameter", "The parameter name for the build number") { Argument = new Argument<string>("PARAMETER", "buildNumber") };
            var versionSuffixParameterOption = new Option("--version-suffix-parameter", "The parameter name for the version suffix") { Argument = new Argument<string>("PARAMETER", "system.build.suffix") };
            var fileCommand = new Command("file", "Calculated the differences between two assemblies");
            fileCommand
                .AddFluentArgument(new Argument<System.IO.FileInfo>() { Name = "first", Description = "The first assembly" })
                .AddFluentArgument(new Argument<System.IO.FileInfo>() { Name = "second", Description = "The second assembly" })
                .AddFluentOption(new Option(new string[] { "-p", "--previous" }, "The previous version") { Argument = new Argument<NuGet.Versioning.SemanticVersion>((SymbolResult symbolResult, out NuGet.Versioning.SemanticVersion value) => NuGet.Versioning.SemanticVersion.TryParse(symbolResult.Token.Value, out value)), })
                .AddFluentOption(new Option(new string[] { "-b", "--build" }, "Ths build label"))
                .AddFluentOption(buildNumberParameterOption)
                .AddFluentOption(versionSuffixParameterOption);

            fileCommand.Handler = CommandHandler.Create<System.IO.FileInfo, System.IO.FileInfo, NuGet.Versioning.SemanticVersion, string, string, string>((first, second, previous, build, buildNumberParameter, versionSuffixParameter) =>
            {
                WriteHeader();
                var result = Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.Analyze(first.FullName, second.FullName, new[] { previous.ToString() }, build);
                WriteVersion(NuGet.Versioning.SemanticVersion.Parse(result.VersionNumber), buildNumberParameter, versionSuffixParameter);
            });

            var solutionCommand = new Command("solution", "Calculates the version based on a solution file");
            solutionCommand
                .AddFluentArgument(new Argument<System.IO.FileSystemInfo>(GetFileSystemInformation) { Name = "projectOrSolution", Description = "The project or solution file to operate on. If a file is not specified, the command will search the current directory for one." })
                .AddFluentOption(new Option(new string[] { "-s", "--source" }, "Specifies the server URL.") { Argument = new Argument<string>("SOURCE") { Arity = ArgumentArity.OneOrMore } })
                .AddFluentOption(new Option("--no-version-suffix", "Forces there to be no version suffix. This overrides --version-suffix") { Argument = new Argument<bool> { Arity = ArgumentArity.ZeroOrOne } })
                .AddFluentOption(new Option("--version-suffix", "Sets the pre-release value. If none is specified, the pre-release from the previous version is used.") { Argument = new Argument<string>("VERSION_SUFFIX") })
                .AddFluentOption(new Option("--no-cache", "Disable using the machine cache as the first package source."))
                .AddFluentOption(new Option("--direct-download", "Download directly without populating any caches with metadata or binaries."))
                .AddFluentOption(new Option("--package-id-regex", "The regular expression to match in the package id.") { Argument = new Argument<string>("REGEX") })
                .AddFluentOption(new Option("--package-id-replace", "The text used to replace the match from --package-id-regex") { Argument = new Argument<string>("VALUE") })
                .AddFluentOption(new Option("--package-id", "The package ID to check for previous versions") { Argument = new Argument<string>("PACKAGE_ID") { Arity = ArgumentArity.OneOrMore } })
                .AddFluentOption(buildNumberParameterOption)
                .AddFluentOption(versionSuffixParameterOption);

            Func<System.IO.FileSystemInfo, System.Collections.Generic.IEnumerable<string>, System.Collections.Generic.IEnumerable<string>, string, string, string, bool, bool, bool, string, string, Task<int>> func = ProcessProjectOrSolution;
            solutionCommand.Handler = System.CommandLine.Binding.HandlerDescriptor.FromDelegate(func).GetCommandHandler();

            var diffCommand = new Command("diff", "Calculates the differences")
                .AddFluentCommand(fileCommand)
                .AddFluentCommand(solutionCommand);

            var rootCommand = new RootCommand(description: "Semantic Version generator");
            rootCommand.AddCommand(diffCommand);

            return rootCommand.InvokeAsync(args);
        }

        private static bool GetFileSystemInformation(SymbolResult symbolResult, out System.IO.FileSystemInfo value)
        {
            var path = symbolResult.Token.Value;
            if (path is null)
            {
                value = null;
                return true;
            }

            if (System.IO.File.Exists(path) || System.IO.Directory.Exists(path))
            {
                value = (System.IO.File.GetAttributes(path) & System.IO.FileAttributes.Directory) != 0
                    ? (System.IO.FileSystemInfo)new System.IO.DirectoryInfo(path)
                    : new System.IO.FileInfo(path);

                return true;
            }

            symbolResult.ErrorMessage = $"\"{path}\" is not a valid file or directory";
            value = null;
            return false;
        }

        private static async Task<int> ProcessProjectOrSolution(
            System.IO.FileSystemInfo projectOrSolution,
            System.Collections.Generic.IEnumerable<string> source,
            System.Collections.Generic.IEnumerable<string> packageId,
            string packageIdRegex,
            string packageIdReplace,
            string versionSuffix,
            bool noVersionSuffix,
            bool noCache,
            bool directDownload,
            string buildNumberParameter,
            string versionSuffixParameter)
        {
            WriteHeader();
            var version = new NuGet.Versioning.SemanticVersion(0, 0, 0);

            string GetVersionSuffix(string previousVersionRelease = default)
            {
                return noVersionSuffix ? string.Empty : (versionSuffix ?? previousVersionRelease);
            }

            async Task<string> TryInstallAsync(System.Collections.Generic.IEnumerable<string> packageIds)
            {
                try
                {
                    return await NuGetInstaller.InstallAsync(packageIds, source, noCache: noCache, directDownload: directDownload).ConfigureAwait(false);
                }
                catch (NuGet.Protocol.PackageNotFoundProtocolException ex)
                {
                    Console.WriteLine("  {0}", ex.Message);
                    return null;
                }
            }

            static string TrimEndingDirectorySeparator(string path)
            {
                return path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            }

            var packageIds = packageId ?? Enumerable.Empty<string>();
            var regex = string.IsNullOrEmpty(packageIdRegex) ? null : new System.Text.RegularExpressions.Regex(packageIdRegex);
            using var projectCollection = GetProjects(projectOrSolution);
            foreach (var project in projectCollection.LoadedProjects.Where(project => bool.TryParse(project.GetPropertyValue("IsPackable"), out var value) && value))
            {
                var projectName = project.GetPropertyValue("MSBuildProjectName");
                Console.WriteLine("Checking {0}", projectName);
                var projectDirectory = project.DirectoryPath;
                var outputPath = TrimEndingDirectorySeparator(System.IO.Path.Combine(project.DirectoryPath, project.GetPropertyValue("OutputPath").Replace('\\', System.IO.Path.DirectorySeparatorChar)));
                var assemblyName = project.GetPropertyValue("AssemblyName");

                // install the NuGet package
                var projectPackageId = project.GetPropertyValue("PackageId");
                var projectPackageIds = new[] { projectPackageId }.Union(packageIds);
                if (regex != null)
                {
                    projectPackageIds = projectPackageIds.Union(new[] { regex.Replace(projectPackageId, packageIdReplace) });
                }

                var installDir = await TryInstallAsync(projectPackageIds).ConfigureAwait(false);
                var previousVersions = NuGetInstaller.GetLatestVersionsAsync(projectPackageIds, source);
                if (installDir is null)
                {
                    var previousVersion = await previousVersions.MaxAsync().ConfigureAwait(false);
                    version = previousVersion is null
                        ? new NuGet.Versioning.SemanticVersion(1, 0, 0, GetVersionSuffix(Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.DefaultAlphaRelease)) // have this as being a 0.1.0 release
                        : new NuGet.Versioning.SemanticVersion(previousVersion.Major, previousVersion.Minor, previousVersion.Patch + 1, GetVersionSuffix(previousVersion.Release));
                }
                else
                {
                    var buildOutputTargetFolder = TrimEndingDirectorySeparator(System.IO.Path.Combine(installDir, project.GetPropertyValue("BuildOutputTargetFolder")));

                    static NuGet.Versioning.SemanticVersion Max(NuGet.Versioning.SemanticVersion first, NuGet.Versioning.SemanticVersion second)
                    {
                        return NuGet.Versioning.VersionComparer.VersionRelease.Compare(first, second) > 0 ? first : second;
                    }

                    var targetExt = project.GetProperty("TargetExt")?.EvaluatedValue ?? ".dll";
                    var previousStringVersions = await previousVersions.Select(previousVersion => previousVersion.ToString()).ToArrayAsync().ConfigureAwait(false);
                    foreach (var currentDll in System.IO.Directory.EnumerateFiles(outputPath, assemblyName + targetExt, new System.IO.EnumerationOptions { RecurseSubdirectories = true }))
                    {
                        var nugetDll = currentDll.Replace(outputPath, buildOutputTargetFolder, StringComparison.CurrentCulture);
                        var result = Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.Analyze(nugetDll, currentDll, previousStringVersions, GetVersionSuffix());
                        version = Max(version, NuGet.Versioning.SemanticVersion.Parse(result.VersionNumber));
                    }

                    System.IO.Directory.Delete(installDir, true);
                }
            }

            // write out the version and the suffix
            WriteVersion(version, buildNumberParameter, versionSuffixParameter);

            return 0;
        }

        private static void WriteHeader()
        {
            Console.WriteLine(Properties.Resources.Logo, VersionUtils.GetVersion());
            Console.WriteLine(Properties.Resources.Copyright);
        }

        private static void WriteVersion(NuGet.Versioning.SemanticVersion version, string buildNumberParameter, string versionSuffixParameter)
        {
            if (buildNumberParameter.Contains(".", StringComparison.Ordinal))
            {
                Console.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[setParameter name='{0}' value='{1:x.y.z}']", buildNumberParameter, version));
            }
            else
            {
                Console.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[{0} '{1:x.y.z}']", buildNumberParameter, version));
            }

            Console.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[setParameter name='{0}' value='{1:R}']", versionSuffixParameter, version));
        }

        private static Microsoft.Build.Evaluation.ProjectCollection GetProjects(System.IO.FileSystemInfo projectOrSolution)
        {
            var projectOrSolutionPath = GetPath(projectOrSolution ?? new System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory()), projectOrSolution is null);
            System.Collections.Generic.IEnumerable<string> projectPaths = string.Equals(projectOrSolutionPath.Extension, ".sln", StringComparison.OrdinalIgnoreCase)
                ? Microsoft.Build.Construction.SolutionFile.Parse(projectOrSolutionPath.FullName).ProjectsInOrder.Where(projectInSolution => projectInSolution.ProjectType == Microsoft.Build.Construction.SolutionProjectType.KnownToBeMSBuildFormat).Select(projectInSolution => projectInSolution.AbsolutePath).ToArray()
                : new string[] { projectOrSolutionPath.FullName };

            // get the highest version
            var directory = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                ? "C:\\Program Files\\dotnet\\sdk"
                : "/usr/share/dotnet/sdk";

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

            NuGet.Versioning.SemanticVersion toolsVersion = default;
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

                        if (validVersions.Length > 0)
                        {
                            toolsVersion = validVersions.Max();
                        }
                        else
                        {
                            throw new Exception($"A compatible installed dotnet SDK for global.json version: [{requestedVersion}] from [{globalJson}] was not found{Environment.NewLine}Please install the [{requestedVersion}] SDK up update [{globalJson}] with an installed dotnet SDK:{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", versions.Select(version => $"{version} [{directory}]"))}");
                        }
                    }
                }
            }

            if (toolsVersion is null)
            {
                toolsVersion = versions.Max(version => version);
            }

            var toolset = parsedToolsets[toolsVersion.ToString()];

            Environment.SetEnvironmentVariable("MSBuildSDKsPath", toolset.GetProperty("MSBuildSDKsPath", null).EvaluatedValue);

            var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();
            projectCollection.AddToolset(toolset);
            projectCollection.DefaultToolsVersion = toolset.ToolsVersion;

            foreach (var projectPath in projectPaths)
            {
                projectCollection.LoadProject(projectPath, toolset.ToolsVersion);
            }

            return projectCollection;
        }

        private static string FindGlobalJson(System.IO.FileSystemInfo path)
        {
            var directory = path switch
            {
                System.IO.DirectoryInfo directoryInfo => directoryInfo.FullName,
                System.IO.FileInfo fileInfo => fileInfo.DirectoryName,
                _ => System.IO.Directory.GetCurrentDirectory(),
            };

            do
            {
                var filePath = System.IO.Path.Combine(directory, "global.json");
                if (System.IO.File.Exists(filePath))
                {
                    return filePath;
                }
            }
            while ((directory = System.IO.Path.GetDirectoryName(directory)) != null);

            return null;
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

                        throw new CommandValidationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.MultipleInSpecifiedFolder, path));
                    }

                    // We did not find any solutions, so try and find individual projects
                    var projectFiles = directoryInfo.EnumerateFiles("*.csproj").Concat(directoryInfo.EnumerateFiles("*.fsproj")).Concat(directoryInfo.EnumerateFiles("*.vbproj")).ToArray();
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

                        throw new CommandValidationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.MultipleInSpecifiedFolder, path));
                    }

                    // At this point the path contains no solutions or projects, so throw an exception
                    throw new CommandValidationException(Properties.Resources.ProjectFileDoesNotExist);
                case System.IO.FileInfo fileInfo:
                    // If a .sln or .csproj file was passed, just return that
                    if (string.Equals(fileInfo.Extension, ".sln", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(fileInfo.Extension, ".csproj", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(fileInfo.Extension, ".vbproj", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(fileInfo.Extension, ".fsproj", StringComparison.OrdinalIgnoreCase))
                    {
                        return fileInfo;
                    }

                    // At this point, we know the file passed in is not a valid project or solution
                    throw new CommandValidationException(Properties.Resources.ProjectFileDoesNotExist);
            }

            return null;
        }
    }
}