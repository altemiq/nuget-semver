// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning.TeamCity
{
    using System;
    using System.Linq;
    using System.Reflection;
    using McMaster.Extensions.CommandLineUtils;

    /// <summary>
    /// The program class.
    /// </summary>
    [HelpOption(ShortName = "h", LongName = "help", Inherited = true)]
    [VersionOptionFromMember(MemberName = nameof(Version))]
    [Subcommand(typeof(DiffCommand))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "RCS1102:MakeClassStatic", Justification = "This is required for a generic method")]
    internal class Program
    {
        private static string Version => typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        private static System.Threading.Tasks.Task<int> Main(string[] args) => CommandLineApplication.ExecuteAsync<Program>(args);

        [Command(Name = "diff", Description = "Calculates the differences")]
        [Subcommand(typeof(FileCommand), typeof(SolutionCommand))]
        private class DiffCommand
        {
        }

        [Command(Name = "file", Description = "Calculated the differences between two assemblies")]
        private class FileCommand
        {
            [Argument(0, Name = "first", Description = "The first assembly")]
            public string First { get; set; }

            [Argument(1, Name = "second", Description = "The second assembly")]
            public string Second { get; set; }

            [Option(ShortName = "p", LongName = "previous", Description = "The previous version")]
            public string PreviousVersion { get; set; }

            [Option(ShortName = "b", LongName = "build", Description = "Ths build label")]
            public string Build { get; set; }

            private void OnExecute()
            {
                var result = Altemiq.Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.Analyze(this.First, this.Second, this.PreviousVersion, this.Build);
                Console.WriteLine($"##teamcity[buildNumber '{result.VersionNumber}']");
            }
        }

        [Command("solution", Description = "Calculates the version based on a solution file")]
        private class SolutionCommand
        {
            [Argument(0, Name = "solution", Description = "The solution file")]
            public string SolutionFile { get; set; }

            [Option(ShortName = "p", LongName = "previous", Description = "The previous version", ValueName = "previous_version")]
            public string PreviousVersion { get; set; }

            private static Microsoft.Build.Evaluation.ProjectCollection GetProjects(string solution)
            {
                var solutionFile = Microsoft.Build.Construction.SolutionFile.Parse(solution);
                var configuration = solutionFile.GetDefaultConfigurationName();
                var platform = solutionFile.GetDefaultPlatformName();

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

                foreach (var projectInSolution in solutionFile.ProjectsInOrder.Where(projectInSolution => projectInSolution.ProjectType == Microsoft.Build.Construction.SolutionProjectType.KnownToBeMSBuildFormat))
                {
                    projectCollection.LoadProject(projectInSolution.AbsolutePath);
                }

                return projectCollection;
            }

            private async System.Threading.Tasks.Task OnExecuteAsync()
            {
                var version = new Semver.SemVersion(0);
                foreach (var project in GetProjects(this.SolutionFile).LoadedProjects.Where(project => bool.TryParse(project.GetPropertyValue("IsPackable"), out var value) && value))
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

                    static Semver.SemVersion Max(Semver.SemVersion first, Semver.SemVersion second) => first.CompareTo(second) > 0 ? first : second;
                    var targetExt = project.GetProperty("TargetExt")?.EvaluatedValue ?? ".dll";

                    foreach (var currentDll in System.IO.Directory.EnumerateFiles(outputPath, assemblyName + targetExt, new System.IO.EnumerationOptions { RecurseSubdirectories = true }))
                    {
                        var nugetDll = currentDll.Replace(outputPath, libDir, StringComparison.CurrentCulture);
                        var result = Altemiq.Assembly.ChangeDetection.SemVer.SemanticVersionAnalyzer.Analyze(nugetDll, currentDll, this.PreviousVersion);
                        version = Max(version, Semver.SemVersion.Parse(result.VersionNumber));
                    }

                    System.IO.Directory.Delete(installDir, true);
                }

                Console.WriteLine($"##teamcity[buildNumber '{version}']");
            }
        }
    }
}