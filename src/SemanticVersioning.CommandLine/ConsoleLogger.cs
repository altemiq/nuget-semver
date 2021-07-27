// -----------------------------------------------------------------------
// <copyright file="ConsoleLogger.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    using System;
    using System.CommandLine;
    using System.CommandLine.IO;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The console logger.
    /// </summary>
    internal class ConsoleLogger : ILogger
    {
        private readonly IConsole console;

        private readonly LogLevel logLevel;

        /// <summary>
        /// Initialises a new instance of the <see cref="ConsoleLogger"/> class.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="diagnostic">Set to <see langword="true"/> to have diagnostic output.</param>
        public ConsoleLogger(IConsole console, bool diagnostic)
        {
            this.console = console;
            this.logLevel = diagnostic ? LogLevel.Trace : LogLevel.Information;
        }

        /// <inheritdoc/>
        public IDisposable BeginScope<TState>(TState state) => new DummyDisposable();

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => logLevel <= this.logLevel;

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel >= LogLevel.Error)
            {
                this.console.Error.WriteLine(formatter(state, exception));
            }
            else
            {
                this.console.Out.WriteLine(formatter(state, exception));
            }
        }

        private sealed class DummyDisposable : System.IDisposable
        {
            void IDisposable.Dispose()
            {
                // Method intentionally left empty.
            }
        }
    }
}