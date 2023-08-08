// -----------------------------------------------------------------------
// <copyright file="ConsoleApplication.Changes.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

using System.CommandLine.IO;
using System.Linq;

/// <content>
/// Application class for writing the changes.
/// </content>
internal static partial class ConsoleApplication
{
    /// <summary>
    /// Writes the changes.
    /// </summary>
    /// <param name="console">The console.</param>
    /// <param name="differences">The differences.</param>
    public static void WriteChanges(IConsoleWithOutput console, Endjin.ApiChange.Api.Diff.AssemblyDiffCollection differences)
    {
        var breakingChanges = console.Output.HasFlag(OutputTypes.BreakingChanges);
        var functionalChanges = console.Output.HasFlag(OutputTypes.FunctionalChanges);
        if (!breakingChanges
            && !functionalChanges)
        {
            return;
        }

        void PrintBreakingChange(Endjin.ApiChange.Api.Diff.DiffOperation operation, string message, int tabs = 0)
        {
            if (breakingChanges && operation.IsRemoved)
            {
                WriteLine(ConsoleColor.Red, message, tabs);
            }
        }

        void PrintFunctionalChange(Endjin.ApiChange.Api.Diff.DiffOperation operation, string message, int tabs = 0)
        {
            if (functionalChanges && operation.IsAdded)
            {
                WriteLine(ConsoleColor.Blue, message, tabs);
            }
        }

        void PrintDiff<T>(Endjin.ApiChange.Api.Diff.DiffResult<T> diffResult, int tabs = 0)
        {
            var message = $"{diffResult}";
            PrintFunctionalChange(diffResult.Operation, message, tabs);
            PrintBreakingChange(diffResult.Operation, message, tabs);
        }

        void WriteLine(ConsoleColor? consoleColor, string value, int tabs = 0)
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

        bool ShouldPrintChangedTypes(IList<Endjin.ApiChange.Api.Diff.TypeDiff> typeDifferences)
        {
            return typeDifferences.Any(ShouldPrintChangedType);
        }

        bool ShouldPrintChanged<T>(Endjin.ApiChange.Api.Diff.DiffCollection<T> collection)
        {
            return (breakingChanges && collection.Exists(method => method.Operation.IsRemoved))
                || (functionalChanges && collection.Exists(method => method.Operation.IsAdded));
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
                        WriteLine(ConsoleColor.Red, Properties.Resources.ChangedBaseType, 3);
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
}