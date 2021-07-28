// -----------------------------------------------------------------------
// <copyright file="MSBuildLogger.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    using System;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The MSBuild logger.
    /// </summary>
    internal class MSBuildLogger : ILogger
    {
        private readonly Microsoft.Build.Utilities.TaskLoggingHelper logger;

        /// <summary>
        /// Initialises a new instance of the <see cref="MSBuildLogger"/> class.
        /// </summary>
        /// <param name="logger">The task logger helper.</param>
        public MSBuildLogger(Microsoft.Build.Utilities.TaskLoggingHelper logger) => this.logger = logger;

        /// <inheritdoc/>
        public IDisposable BeginScope<TState>(TState state) => throw new NotSupportedException();

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    this.logger.LogMessage(Microsoft.Build.Framework.MessageImportance.Low, message);
                    break;
                case LogLevel.Information:
                    this.logger.LogMessage(Microsoft.Build.Framework.MessageImportance.Normal, message);
                    break;
                case LogLevel.Warning:
                    this.logger.LogWarning(message);
                    break;
                case LogLevel.Error:
                    this.logger.LogError(message);
                    break;
                case LogLevel.Critical:
                    this.logger.LogCriticalMessage(subcategory: default, code: default, helpKeyword: default, file: default, lineNumber: default, columnNumber: default, endLineNumber: default, endColumnNumber: default, message);
                    break;
            }
        }
    }
}