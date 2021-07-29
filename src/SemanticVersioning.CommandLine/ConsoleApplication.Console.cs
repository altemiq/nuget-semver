// -----------------------------------------------------------------------
// <copyright file="ConsoleApplication.Console.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    using System;
    using System.CommandLine.IO;

    /// <content>
    /// Application class for writing to the console.
    /// </content>
    internal static partial class ConsoleApplication
    {
        private class ConsoleWithOutput : IConsoleWithOutput
        {
            private readonly System.CommandLine.IConsole console;

            public ConsoleWithOutput(System.CommandLine.IConsole console, OutputTypes output)
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

            private class StandardStreamWriterWithOutput : IStandardStreamWriterWithOutput
            {
                private readonly System.CommandLine.IConsole console;

                private readonly bool isError;

                public StandardStreamWriterWithOutput(System.CommandLine.IConsole console, OutputTypes output, bool isError = false)
                {
                    this.console = console;
                    this.Output = output;
                    this.isError = isError;
                }

                public OutputTypes Output { get; }

                public void Write(string value)
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

                public void Write(string value, OutputTypes output)
                {
                    if (this.Output.HasFlag(output))
                    {
                        this.Write(value);
                    }
                }

                public void WriteLine(string value, OutputTypes output)
                {
                    if (this.Output.HasFlag(output))
                    {
                        this.WriteLine(value);
                    }
                }
            }
        }

        private class TerminalWithOutput : ConsoleWithOutput, System.CommandLine.Rendering.ITerminal
        {
            private readonly System.CommandLine.Rendering.ITerminal terminal;

            public TerminalWithOutput(System.CommandLine.Rendering.ITerminal terminal, OutputTypes output)
                : base(terminal, output) => this.terminal = terminal;

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
    }
}