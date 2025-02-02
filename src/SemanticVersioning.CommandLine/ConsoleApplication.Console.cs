// -----------------------------------------------------------------------
// <copyright file="ConsoleApplication.Console.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

using System.CommandLine.IO;

/// <content>
/// Application class for writing to the console.
/// </content>
internal static partial class ConsoleApplication
{
    /// <summary>
    /// The basic console.
    /// </summary>
    public sealed class Console : System.CommandLine.IConsole
    {
        private Console(TextWriter output, TextWriter error)
        {
            this.Out = StandardStreamWriter.Create(output);
            this.Error = StandardStreamWriter.Create(error);
        }

        /// <inheritdoc/>
        public IStandardStreamWriter Out { get; }

        /// <inheritdoc/>
        public bool IsOutputRedirected => this.Out != System.Console.Out || System.Console.IsOutputRedirected;

        /// <inheritdoc/>
        public IStandardStreamWriter Error { get; }

        /// <inheritdoc/>
        public bool IsErrorRedirected => this.Error != System.Console.Error || System.Console.IsInputRedirected;

        /// <inheritdoc/>
        public bool IsInputRedirected => true;

        /// <summary>
        /// Creates an instance of the <see cref="System.CommandLine.IConsole"/>.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="error">The error.</param>
        /// <returns>The console.</returns>
        public static System.CommandLine.IConsole Create(TextWriter output, TextWriter error) => new Console(output, error);
    }

    private class ConsoleWithOutput : IConsoleWithOutput
    {
        private readonly System.CommandLine.IConsole console;

        protected ConsoleWithOutput(System.CommandLine.IConsole console, OutputTypes output)
        {
            this.console = console;
            this.Output = output;
            this.Out = new StandardStreamWriterWithOutput(this.console, this.Output);
            this.Error = new StandardStreamWriterWithOutput(this.console, this.Output, isError: true);
        }

        public OutputTypes Output { get; }

        public bool IsOutputRedirected => this.console.IsOutputRedirected;

        public bool IsErrorRedirected => this.console.IsErrorRedirected;

        public bool IsInputRedirected => this.console.IsInputRedirected;

        public IStandardStreamWriterWithOutput Out { get; }

        public IStandardStreamWriterWithOutput Error { get; }

        IStandardStreamWriter IStandardOut.Out => this.console.Out;

        IStandardStreamWriter IStandardError.Error => this.console.Error;

        public static IConsoleWithOutput Create(System.CommandLine.IConsole console, OutputTypes outputTypes)
        {
            if (console is System.CommandLine.Rendering.ITerminal terminal)
            {
                return new TerminalWithOutput(terminal, outputTypes);
            }

            return new ConsoleWithOutput(console, outputTypes);
        }

        private sealed class StandardStreamWriterWithOutput(System.CommandLine.IConsole console, OutputTypes output, bool isError = false) : IStandardStreamWriterWithOutput
        {
            private readonly System.CommandLine.IConsole console = console;

            private readonly bool isError = isError;

            public OutputTypes Output { get; } = output;

            public void Write(string? value)
            {
                if (this.isError)
                {
                    this.console.Error.Write(value);
                }
                else
                {
                    this.console.Out.Write(value);
                }
            }
        }
    }

    private sealed class TerminalWithOutput(System.CommandLine.Rendering.ITerminal terminal, OutputTypes output) : ConsoleWithOutput(terminal, output), System.CommandLine.Rendering.ITerminal
    {
        private readonly System.CommandLine.Rendering.ITerminal terminal = terminal;

        public ConsoleColor BackgroundColor
        {
            get => this.terminal.BackgroundColor;
            set => this.terminal.BackgroundColor = value;
        }

        public ConsoleColor ForegroundColor
        {
            get => this.terminal.ForegroundColor;
            set => this.terminal.ForegroundColor = value;
        }

        public int CursorLeft
        {
            get => this.terminal.CursorLeft;
            set => this.terminal.CursorLeft = value;
        }

        public int CursorTop
        {
            get => this.terminal.CursorTop;
            set => this.terminal.CursorTop = value;
        }

        public void Clear() => this.terminal.Clear();

        public void HideCursor() => this.terminal.HideCursor();

        public void ResetColor() => this.terminal.ResetColor();

        public void SetCursorPosition(int left, int top) => this.terminal.SetCursorPosition(left, top);

        public void ShowCursor() => this.terminal.ShowCursor();
    }

    private sealed class NuGetConsole(System.CommandLine.IConsole console) : NuGet.Common.ILogger
    {
        private readonly System.CommandLine.IConsole console = console;

        public void Log(NuGet.Common.LogLevel level, string data)
        {
            // trim off the code
            data = data.Substring(data.IndexOf('|', StringComparison.Ordinal) + 1);

            switch (level)
            {
                case NuGet.Common.LogLevel.Debug:
                    this.LogDebug(data);
                    break;
                case NuGet.Common.LogLevel.Verbose:
                    this.LogVerbose(data);
                    break;
                case NuGet.Common.LogLevel.Information:
                    this.LogInformation(data);
                    break;
                case NuGet.Common.LogLevel.Minimal:
                    this.LogMinimal(data);
                    break;
                case NuGet.Common.LogLevel.Warning:
                    this.LogWarning(data);
                    break;
                case NuGet.Common.LogLevel.Error:
                    this.LogError(data);
                    break;
            }
        }

        public void Log(NuGet.Common.ILogMessage message) => this.Log(message.Level, message.Message);

        public Task LogAsync(NuGet.Common.LogLevel level, string data) => Task.Factory.StartNew(() => this.Log(level, data));

        public Task LogAsync(NuGet.Common.ILogMessage message) => Task.Factory.StartNew(() => this.Log(message));

        public void LogDebug(string data) => this.WriteLine(ConsoleColor.Blue, data);

        public void LogError(string data) => this.console.Error.WriteLine(data);

        public void LogInformation(string data) => this.WriteLine(default, data);

        public void LogInformationSummary(string data) => this.WriteLine(default, data);

        public void LogMinimal(string data) => this.WriteLine(ConsoleColor.DarkGray, data);

        public void LogVerbose(string data) => this.WriteLine(ConsoleColor.Gray, data);

        public void LogWarning(string data) => this.WriteLine(ConsoleColor.DarkYellow, data);

        private void WriteLine(ConsoleColor? consoleColor, string value)
        {
            if (this.console is System.CommandLine.Rendering.ITerminal terminal)
            {
                if (consoleColor.HasValue)
                {
                    terminal.ForegroundColor = consoleColor.Value;
                }

                terminal.Out.WriteLine(value);
                terminal.ResetColor();
            }
            else
            {
                this.console.Out.WriteLine(value);
            }
        }
    }
}