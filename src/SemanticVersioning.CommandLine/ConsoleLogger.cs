// -----------------------------------------------------------------------
// <copyright file="ConsoleLogger.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    using System.CommandLine;
    using System.CommandLine.IO;
    using System.Linq;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The console logger.
    /// </summary>
    internal class ConsoleLogger : ILogger
    {
        private readonly IConsole console;

        private readonly LogLevel logLevel;

        private readonly OutputTypes outputTypes;

        /// <summary>
        /// Initialises a new instance of the <see cref="ConsoleLogger"/> class.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="outputTypes">The output types..</param>
        public ConsoleLogger(IConsole console, OutputTypes outputTypes)
        {
            this.console = console;
            this.outputTypes = outputTypes;
            this.logLevel = outputTypes.HasFlag(OutputTypes.Diagnostic)
                ? LogLevel.Trace
                : LogLevel.Information;
        }

        /// <inheritdoc/>
        public System.IDisposable BeginScope<TState>(TState state) => new DummyDisposable();

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => logLevel <= this.logLevel;

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception exception, System.Func<TState, System.Exception, string> formatter)
        {
            if (formatter(state, exception) is string { Length: > 0 } message)
            {
                GetWriter()(message);
            }

            if (state is Endjin.ApiChange.Api.Diff.AssemblyDiffCollection differences)
            {
                WriteChanges(this.console, this.outputTypes, differences);
            }

            System.Action<string> GetWriter()
            {
                return logLevel >= LogLevel.Error
                    ? this.console.Error.WriteLine
                    : this.console.Out.WriteLine;
            }
        }

        /// <summary>
        /// Writes the changes.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="outputTypes">The output types.</param>
        /// <param name="differences">The differences.</param>
        internal static void WriteChanges(IConsole console, OutputTypes outputTypes, Endjin.ApiChange.Api.Diff.AssemblyDiffCollection differences)
        {
            var breakingChanges = outputTypes.HasFlag(OutputTypes.BreakingChanges);
            var functionalChanges = outputTypes.HasFlag(OutputTypes.FunctionalChanges);
            if (!breakingChanges
                && !functionalChanges)
            {
                return;
            }

            void PrintBreakingChange(Endjin.ApiChange.Api.Diff.DiffOperation operation, string message, int tabs = 0)
            {
                if (breakingChanges && operation.IsRemoved)
                {
                    WriteLine(System.ConsoleColor.Red, message, tabs);
                }
            }

            void PrintFunctionalChange(Endjin.ApiChange.Api.Diff.DiffOperation operation, string message, int tabs = 0)
            {
                if (functionalChanges && operation.IsAdded)
                {
                    WriteLine(System.ConsoleColor.Blue, message, tabs);
                }
            }

            void PrintDiff<T>(Endjin.ApiChange.Api.Diff.DiffResult<T> diffResult, int tabs = 0)
            {
                var message = $"{diffResult}";
                PrintFunctionalChange(diffResult.Operation, message, tabs);
                PrintBreakingChange(diffResult.Operation, message, tabs);
            }

            void WriteLine(System.ConsoleColor? consoleColor, string value, int tabs = 0)
            {
                var message = string.Concat(new string('\t', tabs), value);
                if (console is System.CommandLine.Rendering.ITerminal terminal)
                {
                    if (consoleColor.HasValue)
                    {
                        terminal.ForegroundColor = consoleColor.Value;
                    }

                    terminal.Out.WriteLine(message);
                    terminal.ResetColor();
                }
                else
                {
                    console.Out.WriteLine(message);
                }
            }

            bool ShouldPrintChangedBaseType(bool changedBaseType)
            {
                return breakingChanges && changedBaseType;
            }

            bool ShouldPrintChangedTypes(System.Collections.Generic.IList<Endjin.ApiChange.Api.Diff.TypeDiff> typeDifferences)
            {
                return typeDifferences.Any(ShouldPrintChangedType);
            }

            bool ShouldPrintChanged<T>(Endjin.ApiChange.Api.Diff.DiffCollection<T> collection)
            {
                return (breakingChanges && collection.Any(method => method.Operation.IsRemoved))
                    || (functionalChanges && collection.Any(method => method.Operation.IsAdded));
            }

            bool ShouldPrintChangedType(Endjin.ApiChange.Api.Diff.TypeDiff typeDiff)
            {
                return ShouldPrintChangedBaseType(typeDiff.HasChangedBaseType)
                    || ShouldPrintChanged(typeDiff.Methods)
                    || ShouldPrintChanged(typeDiff.Fields)
                    || ShouldPrintChanged(typeDiff.Events)
                    || ShouldPrintChanged(typeDiff.Interfaces);
            }

            bool ShouldPrintCollection(Endjin.ApiChange.Api.Diff.AssemblyDiffCollection assemblyDiffCollection)
            {
                return (breakingChanges && assemblyDiffCollection.AddedRemovedTypes.RemovedCount != 0)
                    || (functionalChanges && assemblyDiffCollection.AddedRemovedTypes.AddedCount != 0)
                    || ShouldPrintChangedTypes(assemblyDiffCollection.ChangedTypes);
            }

            if (ShouldPrintCollection(differences))
            {
                foreach (var addedRemovedType in differences.AddedRemovedTypes)
                {
                    PrintDiff(addedRemovedType, 1);
                }

                if (ShouldPrintChangedTypes(differences.ChangedTypes))
                {
                    WriteLine(default, Properties.Resources.ChangedTypes, 1);
                    foreach (var changedType in differences.ChangedTypes.Where(ShouldPrintChangedType))
                    {
                        WriteLine(default, $"{changedType.TypeV1}", 2);
                        if (ShouldPrintChangedBaseType(changedType.HasChangedBaseType))
                        {
                            WriteLine(System.ConsoleColor.Red, Properties.Resources.ChangedBaseType, 3);
                        }

                        if (ShouldPrintChanged(changedType.Methods))
                        {
                            WriteLine(default, Properties.Resources.Methods, 3);
                            ForEach(changedType.Methods, method => PrintDiff(method, 4));
                        }

                        if (ShouldPrintChanged(changedType.Fields))
                        {
                            WriteLine(default, Properties.Resources.Fields, 3);
                            ForEach(changedType.Fields, field => PrintDiff(field, 4));
                        }

                        if (ShouldPrintChanged(changedType.Events))
                        {
                            WriteLine(default, Properties.Resources.Events, 3);
                            ForEach(changedType.Events, @event => PrintDiff(@event, 4));
                        }

                        if (ShouldPrintChanged(changedType.Interfaces))
                        {
                            WriteLine(default, Properties.Resources.Interfaces, 3);
                            ForEach(changedType.Interfaces, @interface => PrintDiff(@interface, 4));
                        }

                        static void ForEach<T>(System.Collections.Generic.IEnumerable<T> source, System.Action<T> action)
                        {
                            foreach (var item in source)
                            {
                                action(item);
                            }
                        }
                    }
                }
            }
        }

        private sealed class DummyDisposable : System.IDisposable
        {
            void System.IDisposable.Dispose()
            {
                // Method intentionally left empty.
            }
        }
    }
}