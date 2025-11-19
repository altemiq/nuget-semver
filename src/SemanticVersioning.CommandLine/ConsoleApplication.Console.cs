// -----------------------------------------------------------------------
// <copyright file="ConsoleApplication.Console.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

using Spectre.Console;
using Spectre.Console.Rendering;

/// <content>
/// Application class for writing to the console.
/// </content>
internal static partial class ConsoleApplication
{
    /// <summary>
    /// The console class.
    /// </summary>
    public static class Console
    {
        /// <summary>
        /// Creates a console.
        /// </summary>
        /// <param name="out">The output.</param>
        /// <param name="error">The error.</param>
        /// <param name="output">The kind of output.</param>
        /// <returns>The console.</returns>
        public static IConsoleWithOutput Create(TextWriter @out, TextWriter error, OutputTypes output)
        {
            // create the ANSI consoles
            var ansiOut = AnsiConsole.Create(new() { Out = new AnsiConsoleOutput(@out) });
            var ansiError = AnsiConsole.Create(new() { Out = new AnsiConsoleOutput(error) });

            return ConsoleWithOutput.Create(ansiOut, ansiError, output);
        }
    }

    private sealed class ConsoleWithOutput : IConsoleWithOutput
    {
        private ConsoleWithOutput(IAnsiConsole @out, IAnsiConsole error, OutputTypes output)
        {
            this.Output = output;
            this.Out = new AnsiConsoleWithOutput(@out, this.Output);
            this.Error = new AnsiConsoleWithOutput(error, this.Output);
        }

        public OutputTypes Output { get; }

        public IAnsiConsoleWithOutput Out { get; }

        public IAnsiConsoleWithOutput Error { get; }

        public static IConsoleWithOutput Create(IAnsiConsole @out, IAnsiConsole error, OutputTypes outputTypes) => new ConsoleWithOutput(@out, error, outputTypes);

        private sealed class AnsiConsoleWithOutput(IAnsiConsole console, OutputTypes output) : IAnsiConsoleWithOutput
        {
            public OutputTypes Output { get; } = output;

            public Profile Profile => console.Profile;

            public IAnsiConsoleCursor Cursor => console.Cursor;

            public IAnsiConsoleInput Input => console.Input;

            public IExclusivityMode ExclusivityMode => console.ExclusivityMode;

            public RenderPipeline Pipeline => console.Pipeline;

            public void Clear(bool home) => console.Clear(home);

            public void Write(IRenderable renderable) => console.Write(renderable);
        }
    }

    private sealed class NuGetConsole(IConsoleWithOutput console) : NuGet.Common.ILogger
    {
        private readonly Style debugStyle = new(foreground: ConsoleColor.Blue);
        private readonly Style minimalStyle = new(foreground: ConsoleColor.DarkGray);
        private readonly Style verboseStyle = new(foreground: ConsoleColor.Gray);
        private readonly Style warningStyle = new(foreground: ConsoleColor.DarkYellow);

        public void Log(NuGet.Common.LogLevel level, string data)
        {
            // trim off the code
            data = data[(data.IndexOf('|', StringComparison.Ordinal) + 1)..];

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

        public void LogDebug(string data) => console.Out.WriteLine(data, this.debugStyle);

        public void LogError(string data) => console.Error.WriteLine(data);

        public void LogInformation(string data) => console.Out.WriteLine(data);

        public void LogInformationSummary(string data) => console.Out.WriteLine(data);

        public void LogMinimal(string data) => console.Out.WriteLine(data, this.minimalStyle);

        public void LogVerbose(string data) => console.Out.WriteLine(data, this.verboseStyle);

        public void LogWarning(string data) => console.Out.WriteLine(data, this.warningStyle);
    }
}