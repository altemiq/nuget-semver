// -----------------------------------------------------------------------
// <copyright file="MSBuildTestHelper.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning.MSBuild;

internal static class MSBuildTestHelper
{
    public static (Microsoft.Build.Execution.BuildResult Result, IDictionary<string, string> Properties) BuildProject(string projectPath, string configuration = "Debug", string target = "Build")
    {
        var globalProps = new Dictionary<string, string>
        {
            { "Configuration", configuration },
            { "VersioningTaskAssembly", Path.Combine(PathHelper.GetSolutionFolder()!, "src", "SemanticVersioning.MSBuild", "bin", "Debug", "netstandard2.0", "Altemiq.SemanticVersioning.MSBuild.dll")  },
            { "ComputeSemanticVersion", bool.TrueString },
            { "VersionRestoreSources", PathHelper.GetSource("only-release") },
        };

        var logger = new PropertyCaptureLogger();        
        var result = Microsoft.Build.Execution.BuildManager.DefaultBuildManager.Build(
            new Microsoft.Build.Execution.BuildParameters(new Microsoft.Build.Evaluation.ProjectCollection(globalProps))
            {
                Loggers =
                [
                    logger,
                    new Microsoft.Build.Logging.ConsoleLogger(),
                ]
            },
            new Microsoft.Build.Execution.BuildRequestData(
                projectPath,
                globalProps,
                null,
                [target],
                null));

        return (result, logger.Properties);
    }

    private class PropertyCaptureLogger : Microsoft.Build.Framework.ILogger
    {
        Microsoft.Build.Framework.LoggerVerbosity Microsoft.Build.Framework.ILogger.Verbosity { get; set; }

        string? Microsoft.Build.Framework.ILogger.Parameters { get; set; }

        public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

        void Microsoft.Build.Framework.ILogger.Initialize(Microsoft.Build.Framework.IEventSource eventSource) =>
            eventSource.MessageRaised += (sender, e) =>
            {
                if (e.Message.StartsWith("TEST-PROPERTY-CAPTURE:"))
                {
                    // split this
                    var propertyAndValue = e.Message.Substring(22);
                    var index = propertyAndValue.IndexOf('=');
                    this.Properties.Add(
                        propertyAndValue[..index],
                        propertyAndValue[(index + 1)..]);
                }
            };

        void Microsoft.Build.Framework.ILogger.Shutdown()
        {
        }
    }
}