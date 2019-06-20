// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="GeomaticTechnologies">
// Copyright (c) GeomaticTechnologies. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
            var previousOption = new Option(new string[] { "-p", "--previous" }, "The previous version")
            {
                Argument = new Argument<NuGet.Versioning.SemanticVersion>((SymbolResult symbolResult, out NuGet.Versioning.SemanticVersion value) => NuGet.Versioning.SemanticVersion.TryParse(symbolResult.Token.Value, out value)),
            };

            var fileCommand = new Command("file", "Calculated the differences between two assemblies");
            fileCommand
                .AddFluentArgument(new Argument<System.IO.FileInfo>() { Name = "first", Description = "The first assembly" })
                .AddFluentArgument(new Argument<System.IO.FileInfo>() { Name = "second", Description = "The second assembly" })
                .AddFluentOption(previousOption)
                .AddFluentOption(new Option(new string[] { "-b", "--build" }, "Ths build label"));

            fileCommand.Handler = CommandHandler.Create<System.IO.FileInfo, System.IO.FileInfo, NuGet.Versioning.SemanticVersion, string>((first, second, previous, build) =>
            {
                var result = Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.Analyze(first.FullName, second.FullName, previous.ToString(), build);
                var version = NuGet.Versioning.SemanticVersion.Parse(result.VersionNumber);
                Console.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[system.build.number '{0:x.y.z}']", version));
                Console.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[system.build.suffix '{0:R}']", version));
            });

            var solutionCommand = new Command("solution", "Calculates the version based on a solution file");
            solutionCommand
                .AddFluentArgument(new Argument<System.IO.FileSystemInfo>(GetFileSystemInformation) { Name = "projectOrSolution", Description = "The project or solution file to operate on. If a file is not specified, the command will search the current directory for one." })
                .AddFluentOption(new Option(new string[] { "-s", "--source" }, "Specifies the server URL.") { Argument = new Argument<string>("SOURCE", "http://artifacts.geomatic.com.au/nuget/NuGet") { Arity = ArgumentArity.ZeroOrMore } })
                .AddFluentOption(new Option(new string[] { "--version-suffix" }, "Sets the pre-release value. If none is specified, the pre-release from the previous version is used.") { Argument = new Argument<string>("VERSION_SUFFIX") })
                .AddFluentOption(previousOption);

            solutionCommand.Handler = CommandHandler.Create<System.IO.FileSystemInfo, System.Collections.Generic.IEnumerable<string>, NuGet.Versioning.SemanticVersion, string>(ProcessProjectOrSolution);

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

        private static async Task<int> ProcessProjectOrSolution(System.IO.FileSystemInfo projectOrSolution, System.Collections.Generic.IEnumerable<string> source, NuGet.Versioning.SemanticVersion previous, string versionSuffix)
        {
            var version = new NuGet.Versioning.SemanticVersion(0, 0, 0);
            async Task<string> GetPreviousVersionStringAsync(string packageId)
            {
                if (previous != null)
                {
                    return previous.ToString();
                }

                var nugetVersion = await NuGetInstaller.GetLatestVersionAsync(packageId, source).ConfigureAwait(false);
                return nugetVersion.ToString();
            }

            using var projectCollection = GetProjects(projectOrSolution);
            foreach (var project in projectCollection.LoadedProjects.Where(project => bool.TryParse(project.GetPropertyValue("IsPackable"), out var value) && value))
            {
                var projectDirectory = project.DirectoryPath;
                var outputPath = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.Combine(project.DirectoryPath, project.GetPropertyValue("OutputPath")));
                var assemblyName = project.GetPropertyValue("AssemblyName");

                // install the NuGet package
                var packageId = project.GetPropertyValue("PackageId");
                var installDir = await NuGetInstaller.InstallAsync(packageId, source).ConfigureAwait(false);
                var buildOutputTargetFolder = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.Combine(installDir, project.GetPropertyValue("BuildOutputTargetFolder")));

                static NuGet.Versioning.SemanticVersion Max(NuGet.Versioning.SemanticVersion first, NuGet.Versioning.SemanticVersion second) => first.CompareTo(second, NuGet.Versioning.VersionComparison.VersionRelease) > 0 ? first : second;

                var targetExt = project.GetProperty("TargetExt")?.EvaluatedValue ?? ".dll";
                var previousString = await GetPreviousVersionStringAsync(packageId).ConfigureAwait(false);
                foreach (var currentDll in System.IO.Directory.EnumerateFiles(outputPath, assemblyName + targetExt, new System.IO.EnumerationOptions { RecurseSubdirectories = true }))
                {
                    var nugetDll = currentDll.Replace(outputPath, buildOutputTargetFolder, StringComparison.CurrentCulture);
                    var result = Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.Analyze(nugetDll, currentDll, previousString, versionSuffix);
                    version = Max(version, NuGet.Versioning.SemanticVersion.Parse(result.VersionNumber));
                }

                System.IO.Directory.Delete(installDir, true);
            }

            // write out the version and the suffix
            Console.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[system.build.number '{0:x.y.z}']", version));
            Console.WriteLine(string.Format(NuGet.Versioning.VersionFormatter.Instance, "##teamcity[system.build.suffix '{0:R}']", version));

            return 0;
        }

        private static Microsoft.Build.Evaluation.ProjectCollection GetProjects(System.IO.FileSystemInfo projectOrSolution)
        {
            var projectOrSolutionPath = GetPath(projectOrSolution ?? new System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory()), projectOrSolution is null);
            System.Collections.Generic.IEnumerable<string> projectPaths = string.Compare(projectOrSolutionPath.Extension, ".sln", StringComparison.OrdinalIgnoreCase) == 0
                ? Microsoft.Build.Construction.SolutionFile.Parse(projectOrSolutionPath.FullName).ProjectsInOrder.Where(projectInSolution => projectInSolution.ProjectType == Microsoft.Build.Construction.SolutionProjectType.KnownToBeMSBuildFormat).Select(projectInSolution => projectInSolution.AbsolutePath).ToArray()
                : new string[] { projectOrSolutionPath.FullName };

            var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection();

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
                    var currentToolsVersion = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(propsFile)) ?? projectCollection.DefaultToolsVersion;

                    return (toolsVersion: version.ToString(), toolset: new Microsoft.Build.Evaluation.Toolset(currentToolsVersion, path, properties, projectCollection, path));
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

            parsedToolsets.TryGetValue(toolsVersion.ToString(), out var toolset);

            Environment.SetEnvironmentVariable("MSBuildSDKsPath", toolset.GetProperty("MSBuildSDKsPath", null).EvaluatedValue);

            // get the version
            projectCollection.AddToolset(toolset);

            foreach (var projectPath in projectPaths)
            {
                projectCollection.LoadProject(projectPath, toolset.ToolsVersion);
            }

            return projectCollection;
        }

        private static string FindGlobalJson(System.IO.FileSystemInfo path)
        {
            string directory = path switch
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
                    if ((string.Compare(fileInfo.Extension, ".sln", StringComparison.OrdinalIgnoreCase) == 0)
                        || (string.Compare(fileInfo.Extension, ".csproj", StringComparison.OrdinalIgnoreCase) == 0)
                        || (string.Compare(fileInfo.Extension, ".vbproj", StringComparison.OrdinalIgnoreCase) == 0)
                        || (string.Compare(fileInfo.Extension, ".fsproj", StringComparison.OrdinalIgnoreCase) == 0))
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