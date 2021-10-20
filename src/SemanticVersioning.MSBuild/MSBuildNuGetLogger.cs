// -----------------------------------------------------------------------
// <copyright file="MSBuildNuGetLogger.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning;

/// <summary>
/// The MSBuild NuGet logger.
/// </summary>
internal sealed class MSBuildNuGetLogger : NuGet.Common.ILogger
{
    private readonly TaskLoggingHelper logger;

    /// <summary>
    /// Initialises a new instance of the <see cref="MSBuildNuGetLogger"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public MSBuildNuGetLogger(TaskLoggingHelper logger) => this.logger = logger;

    /// <inheritdoc/>
    public void Log(NuGet.Common.LogLevel level, string data)
    {
        if (level == NuGet.Common.LogLevel.Error)
        {
            this.LogError(data);
        }
        else if (level == NuGet.Common.LogLevel.Warning)
        {
            this.LogWarning(data);
        }
        else
        {
            var messageImportance = level switch
            {
                NuGet.Common.LogLevel.Debug or NuGet.Common.LogLevel.Verbose or NuGet.Common.LogLevel.Minimal => Microsoft.Build.Framework.MessageImportance.Low,
                NuGet.Common.LogLevel.Information => Microsoft.Build.Framework.MessageImportance.Normal,
                NuGet.Common.LogLevel.Warning or NuGet.Common.LogLevel.Error => Microsoft.Build.Framework.MessageImportance.High,
                _ => default,
            };

            this.logger.LogMessage(messageImportance, data);
        }
    }

    /// <inheritdoc/>
    public void Log(NuGet.Common.ILogMessage message) => this.Log(message.Level, message.Message);

    /// <inheritdoc/>
    public System.Threading.Tasks.Task LogAsync(NuGet.Common.LogLevel level, string data) => System.Threading.Tasks.Task.Factory.StartNew(() => this.Log(level, data));

    /// <inheritdoc/>
    public System.Threading.Tasks.Task LogAsync(NuGet.Common.ILogMessage message) => System.Threading.Tasks.Task.Factory.StartNew(() => this.Log(message));

    /// <inheritdoc/>
    public void LogDebug(string data) => this.logger.LogMessage(Microsoft.Build.Framework.MessageImportance.Low, data);

    /// <inheritdoc/>
    public void LogError(string data) => this.logger.LogError(data);

    /// <inheritdoc/>
    public void LogInformation(string data) => this.logger.LogMessage(Microsoft.Build.Framework.MessageImportance.Normal, data);

    /// <inheritdoc/>
    public void LogInformationSummary(string data) => this.logger.LogMessage(Microsoft.Build.Framework.MessageImportance.Normal, data);

    /// <inheritdoc/>
    public void LogMinimal(string data) => this.logger.LogMessage(Microsoft.Build.Framework.MessageImportance.Low, data);

    /// <inheritdoc/>
    public void LogVerbose(string data) => this.logger.LogMessage(Microsoft.Build.Framework.MessageImportance.Low, data);

    /// <inheritdoc/>
    public void LogWarning(string data) => this.logger.LogWarning(data);
}