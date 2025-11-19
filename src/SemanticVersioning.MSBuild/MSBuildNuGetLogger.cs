// -----------------------------------------------------------------------
// <copyright file="MSBuildNuGetLogger.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

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
        switch (level)
        {
            case NuGet.Common.LogLevel.Error:
                this.LogError(data);
                break;
            case NuGet.Common.LogLevel.Warning:
                this.LogWarning(data);
                break;
            case NuGet.Common.LogLevel.Debug or NuGet.Common.LogLevel.Verbose or NuGet.Common.LogLevel.Minimal:
                this.LogMessageCore(MessageImportance.Low, data);
                break;
            case NuGet.Common.LogLevel.Information:
                this.LogMessageCore(MessageImportance.Normal, data);
                break;
            default:
                this.LogMessageCore(default, data);
                break;
        }
    }

    /// <inheritdoc/>
    public void Log(NuGet.Common.ILogMessage message) => this.Log(message.Level, message.Message);

    /// <inheritdoc/>
    public System.Threading.Tasks.Task LogAsync(NuGet.Common.LogLevel level, string data) => System.Threading.Tasks.Task.Factory.StartNew(() => this.Log(level, data));

    /// <inheritdoc/>
    public System.Threading.Tasks.Task LogAsync(NuGet.Common.ILogMessage message) => System.Threading.Tasks.Task.Factory.StartNew(() => this.Log(message));

    /// <inheritdoc/>
    public void LogDebug(string data) => this.LogMessageCore(MessageImportance.Low, data);

    /// <inheritdoc/>
    public void LogError(string data)
    {
        (var code, data) = Process(data);
        this.logger.LogError(
            subcategory: null,
            errorCode: code,
            helpKeyword: null,
            file: null,
            lineNumber: 0,
            columnNumber: 0,
            endLineNumber: 0,
            endColumnNumber: 0,
            message: data);
    }

    /// <inheritdoc/>
    public void LogInformation(string data) => this.LogMessageCore(MessageImportance.Normal, data);

    /// <inheritdoc/>
    public void LogInformationSummary(string data) => this.LogMessageCore(MessageImportance.Normal, data);

    /// <inheritdoc/>
    public void LogMinimal(string data) => this.LogMessageCore(MessageImportance.Low, data);

    /// <inheritdoc/>
    public void LogVerbose(string data) => this.LogMessageCore(MessageImportance.Low, data);

    /// <inheritdoc/>
    public void LogWarning(string data)
    {
        (var code, data) = Process(data);
        this.logger.LogWarning(
            subcategory: null,
            warningCode: code,
            helpKeyword: null,
            file: null,
            lineNumber: 0,
            columnNumber: 0,
            endLineNumber: 0,
            endColumnNumber: 0,
            message: data);
    }

    private static (string? Code, string Data) Process(string data)
    {
        string? code = default;
        var split = data.Split('|');
        if (split.Length is 2)
        {
            code = split[0];
            data = split[1];
        }

        return (code, data);
    }

    private void LogMessageCore(MessageImportance importance, string data)
    {
        (var code, data) = Process(data);
        this.logger.LogMessage(
            subcategory: null,
            code: code,
            helpKeyword: null,
            file: null,
            lineNumber: 0,
            columnNumber: 0,
            endLineNumber: 0,
            endColumnNumber: 0,
            importance: importance,
            message: data);
    }
}