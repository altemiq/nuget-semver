// -----------------------------------------------------------------------
// <copyright file="MSBuildTestHelper.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning.MSBuild;

internal class MSBuildTestHelper
{
    private readonly Lock @lock = new();

    public (Microsoft.Build.Execution.BuildResult Result, IDictionary<string, string> Properties) BuildProject(string projectPath, string configuration = "Debug", string target = "Build")
    {
        // isolate the project
        var projectDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        if (File.Exists(projectPath))
        {
            Directory.CreateDirectory(projectDirectory);
            foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(projectPath)!, "*"))
            {
                File.Copy(file, Path.Combine(projectDirectory, Path.GetFileName(file)));
            }

            projectPath = Path.Combine(projectDirectory, Path.GetFileName(projectPath));
        }
        else if (Directory.Exists(projectPath))
        {
            Directory.CreateDirectory(projectDirectory);
            foreach (var file in Directory.EnumerateFiles(projectPath, "*"))
            {
                File.Copy(file, Path.Combine(projectDirectory, Path.GetFileName(file)));
            }

            projectPath = projectDirectory;
        }

        if (Directory.Exists(projectDirectory))
        {
            var repositoryDirectory = LibGit2Sharp.Repository.Init(projectDirectory);
            using var repository = new LibGit2Sharp.Repository(repositoryDirectory);
            var author = new LibGit2Sharp.Signature(
                new LibGit2Sharp.Identity("No One", "noone@nowhere.com"),
                DateTimeOffset.Now);
            var commit = repository.Commit(
                "Initial Commit",
                author,
                author,
                new LibGit2Sharp.CommitOptions());
        }

        // do a basic build first
        var semanticVersioningProjectDirectory = Path.Combine(PathHelper.GetSolutionFolder()!, "src", "SemanticVersioning.MSBuild");
        var globalProps = new Dictionary<string, string>
        {
            { "Configuration", configuration },
            { "SemanticVersioningProjectDirectory", semanticVersioningProjectDirectory + Path.DirectorySeparatorChar },
        };

        Microsoft.Build.Execution.BuildResult result;
        lock (this.@lock)
        {
            result = Microsoft.Build.Execution.BuildManager.DefaultBuildManager.Build(
                new Microsoft.Build.Execution.BuildParameters(new Microsoft.Build.Evaluation.ProjectCollection(globalProps))
                {
                    Loggers = [new Microsoft.Build.Logging.ConsoleLogger()]
                },
                new Microsoft.Build.Execution.BuildRequestData(
                    projectPath,
                    globalProps,
                    null,
                    ["Restore", "Build"],
                    null));
        }

        if (result.OverallResult is not Microsoft.Build.Execution.BuildResultCode.Success )
        {
            return (result, new Dictionary<string, string>());
        }

        globalProps.Add("VersioningTaskAssembly", Path.Combine(semanticVersioningProjectDirectory, "bin", "Debug", "netstandard2.0", "Altemiq.SemanticVersioning.MSBuild.dll")  );
        globalProps.Add("ComputeSemanticVersion", bool.TrueString );
        globalProps.Add("VersionRestoreSources", PathHelper.GetSource("only-release") );
        
        var logger = new PropertyCaptureLogger();
        lock (this.@lock)
        {
            result = Microsoft.Build.Execution.BuildManager.DefaultBuildManager.Build(
            new Microsoft.Build.Execution.BuildParameters(new Microsoft.Build.Evaluation.ProjectCollection(globalProps))
            {
                Loggers =
                [
                    logger,
                    new Microsoft.Build.Logging.ConsoleLogger(),
                    new Microsoft.Build.Logging.BinaryLogger { Parameters = Path.Combine(TestContext.OutputDirectory ?? string.Empty, $"{TestContext.Current?.Id}.binlog") },
                ]
            },
            new Microsoft.Build.Execution.BuildRequestData(
                projectPath,
                globalProps,
                null,
                [target],
                null));
        }

        if (Directory.Exists(projectDirectory))
        {
            try
            {
                Directory.Delete(projectDirectory, true);
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return (result, logger.Properties);
    }

    private class PropertyCaptureLogger : Microsoft.Build.Framework.ILogger
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> properties = new();

        Microsoft.Build.Framework.LoggerVerbosity Microsoft.Build.Framework.ILogger.Verbosity { get; set; }

        string? Microsoft.Build.Framework.ILogger.Parameters { get; set; }

        public IDictionary<string, string> Properties => this.properties;

        void Microsoft.Build.Framework.ILogger.Initialize(Microsoft.Build.Framework.IEventSource eventSource) =>
            eventSource.MessageRaised += (sender, e) =>
            {
                if (e.Message.StartsWith("TEST-PROPERTY-CAPTURE:"))
                {
                    var propertyAndValue = e.Message.AsSpan(22);
                    var index = propertyAndValue.IndexOf('=');
                    var value = propertyAndValue[(index + 1)..].ToString();
                    this.properties.AddOrUpdate(
                        propertyAndValue[..index].ToString(),
                        static (key, value) => value,
                        static (key, old, value) => value,
                        value);
                }
            };

        void Microsoft.Build.Framework.ILogger.Shutdown()
        {
        }
    }
}