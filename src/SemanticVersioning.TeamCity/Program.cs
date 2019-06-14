// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning.TeamCity
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
            var previousOption = new Option(new string[] { "-p", "--previous" }, "The previous version", new Argument<Semver.SemVersion>((SymbolResult symbolResult, out Semver.SemVersion value) => Semver.SemVersion.TryParse(symbolResult.Token.Value, out value)));

            var fileCommand = new Command("file", "Calculated the differences between two assemblies");
            fileCommand
                .AddFluentArgument(new Argument<System.IO.FileInfo>() { Name = "first", Description = "The first assembly" })
                .AddFluentArgument(new Argument<System.IO.FileInfo>() { Name = "second", Description = "The second assembly" })
                .AddFluentOption(previousOption)
                .AddFluentOption(new Option(new string[] { "-b", "--build" }, "Ths build label"));

            fileCommand.Handler = CommandHandler.Create<System.IO.FileInfo, System.IO.FileInfo, Semver.SemVersion, string>((first, second, previous, build) =>
            {
                var result = Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.Analyze(first.FullName, second.FullName, previous.ToString(), build);
                Console.WriteLine($"##teamcity[buildNumber '{result.VersionNumber}']");
            });

            var solutionCommand = new Command("solution", "Calculates the version based on a solution file");
            solutionCommand
                .AddFluentArgument(new Argument<System.IO.FileSystemInfo>(GetFileSystemInformation) { Name = "projectOrSolution", Description = "The project or solution file to operate on. If a file is not specified, the command will search the current directory for one." })
                .AddFluentOption(previousOption);

            solutionCommand.Handler = CommandHandler.Create<System.IO.FileSystemInfo, Semver.SemVersion>(ProcessProjectOrSolution);

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

        private static async Task<int> ProcessProjectOrSolution(System.IO.FileSystemInfo projectOrSolution, Semver.SemVersion previous)
        {
            var version = new Semver.SemVersion(0);
            using var projectCollection = GetProjects(projectOrSolution);
            foreach (var project in projectCollection.LoadedProjects.Where(project => bool.TryParse(project.GetPropertyValue("IsPackable"), out var value) && value))
            {
                var projectDirectory = project.DirectoryPath;
                var outputPath = System.IO.Path.Combine(project.DirectoryPath, project.GetPropertyValue("OutputPath"));
                var assemblyName = project.GetPropertyValue("AssemblyName");

                // install the NuGet package
                var packageId = project.GetProperty("PackageId").EvaluatedValue;

                var installDir = await NuGetInstaller.InstallAsync(packageId).ConfigureAwait(false);
                var libDir = System.IO.Path.Combine(installDir, project.GetPropertyValue("BuildOutputTargetFolder"));
                if (outputPath.EndsWith(System.IO.Path.DirectorySeparatorChar))
                {
                    libDir += System.IO.Path.DirectorySeparatorChar;
                }

                static Semver.SemVersion Max(Semver.SemVersion first, Semver.SemVersion second)
                {
                    return first.CompareTo(second) > 0 ? first : second;
                }

                var targetExt = project.GetProperty("TargetExt")?.EvaluatedValue ?? ".dll";

                foreach (var currentDll in System.IO.Directory.EnumerateFiles(outputPath, assemblyName + targetExt, new System.IO.EnumerationOptions { RecurseSubdirectories = true }))
                {
                    var nugetDll = currentDll.Replace(outputPath, libDir, StringComparison.CurrentCulture);
                    var result = Altemiq.Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.Analyze(nugetDll, currentDll, previous.ToString());
                    version = Max(version, Semver.SemVersion.Parse(result.VersionNumber));
                }

                System.IO.Directory.Delete(installDir, true);
            }

            Console.WriteLine($"##teamcity[buildNumber '{version}']");
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

            foreach (var path in System.IO.Directory.EnumerateDirectories(directory))
            {
                // set the version
                if (Semver.SemVersion.TryParse(System.IO.Path.GetFileName(path), out var version))
                {
                    var rootDirectory = System.IO.Path.Combine(directory, version.ToString());
                    var properties = new System.Collections.Generic.Dictionary<string, string>
                        {
                            { "MSBuildSDKsPath", System.IO.Path.Combine(rootDirectory, "Sdks") },
                            { "RoslynTargetsPath", System.IO.Path.Combine(rootDirectory, "Roslyn") },
                            { "MSBuildExtensionsPath", rootDirectory },
                        };

                    projectCollection.AddToolset(new Microsoft.Build.Evaluation.Toolset(version.ToString(), rootDirectory, properties, projectCollection, rootDirectory));
                }
            }

            var toolsVersion = projectCollection.Toolsets.Max(toolset => Semver.SemVersion.TryParse(toolset.ToolsVersion, out var tempVersion) ? tempVersion : null);
            var toolset = projectCollection.GetToolset(toolsVersion.ToString());

            Environment.SetEnvironmentVariable("MSBuildSDKsPath", toolset.GetProperty("MSBuildSDKsPath", null).EvaluatedValue);

            projectCollection.AddToolset(new Microsoft.Build.Evaluation.Toolset(
                projectCollection.DefaultToolsVersion,
                toolset.ToolsPath,
                toolset.Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.EvaluatedValue),
                projectCollection,
                string.Empty));

            foreach (var projectPath in projectPaths)
            {
                projectCollection.LoadProject(projectPath);
            }

            return projectCollection;
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