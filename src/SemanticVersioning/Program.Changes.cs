// -----------------------------------------------------------------------
// <copyright file="Program.Changes.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    using System;
    using System.Linq;

    /// <content>
    /// Program class for writing the changes.
    /// </content>
    internal static partial class Program
    {
        private static void WriteChanges(OutputTypes outputTypes, Assembly.ChangeDetection.Diff.AssemblyDiffCollection differences)
        {
            var breakingChanges = outputTypes.HasFlag(OutputTypes.BreakingChanges);
            var functionalChanges = outputTypes.HasFlag(OutputTypes.FunctionalChanges);
            if (!breakingChanges
                && !functionalChanges)
            {
                return;
            }

            void PrintBreakingChange(Assembly.ChangeDetection.Diff.DiffOperation operation, string message, int tabs = 0)
            {
                if (breakingChanges && operation.IsRemoved)
                {
                    WriteLine(ConsoleColor.Red, message, tabs);
                }
            }

            void PrintFunctionalChange(Assembly.ChangeDetection.Diff.DiffOperation operation, string message, int tabs = 0)
            {
                if (functionalChanges && operation.IsAdded)
                {
                    WriteLine(ConsoleColor.Blue, message, tabs);
                }
            }

            void PrintDiff<T>(Assembly.ChangeDetection.Diff.DiffResult<T> diffResult, int tabs = 0)
            {
                var message = $"{diffResult}";
                PrintFunctionalChange(diffResult.Operation, message, tabs);
                PrintBreakingChange(diffResult.Operation, message, tabs);
            }

            var originalColour = Console.ForegroundColor;
            void WriteLine(ConsoleColor consoleColor, string value, int tabs = 0)
            {
                Console.ForegroundColor = consoleColor;
                Console.WriteLine(string.Concat(new string('\t', tabs), value));
                Console.ForegroundColor = originalColour;
            }

            bool ShouldPrintChangedBaseType(bool changedBaseType)
            {
                return breakingChanges && changedBaseType;
            }

            bool ShouldPrintChangedTypes(System.Collections.Generic.IList<Assembly.ChangeDetection.Diff.TypeDiff> typeDifferences)
            {
                return typeDifferences.Any(ShouldPrintChangedType);
            }

            bool ShouldPrintChanged<T>(Assembly.ChangeDetection.Diff.DiffCollection<T> collection)
            {
                return (breakingChanges && collection.Any(method => method.Operation.IsRemoved))
                    || (functionalChanges && collection.Any(method => method.Operation.IsAdded));
            }

            bool ShouldPrintChangedType(Assembly.ChangeDetection.Diff.TypeDiff typeDiff)
            {
                return ShouldPrintChangedBaseType(typeDiff.HasChangedBaseType)
                    || ShouldPrintChanged(typeDiff.Methods)
                    || ShouldPrintChanged(typeDiff.Fields)
                    || ShouldPrintChanged(typeDiff.Events)
                    || ShouldPrintChanged(typeDiff.Interfaces);
            }

            bool ShouldPrintCollection(Assembly.ChangeDetection.Diff.AssemblyDiffCollection assemblyDiffCollection)
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
                    WriteLine(originalColour, Properties.Resources.ChangedTypes, 1);
                    foreach (var changedType in differences.ChangedTypes.Where(ShouldPrintChangedType))
                    {
                        WriteLine(originalColour, $"{changedType.TypeV1}", 2);
                        if (ShouldPrintChangedBaseType(changedType.HasChangedBaseType))
                        {
                            WriteLine(ConsoleColor.Red, Properties.Resources.ChangedBaseType, 3);
                        }

                        if (ShouldPrintChanged(changedType.Methods))
                        {
                            WriteLine(originalColour, Properties.Resources.Methods, 3);
                            foreach (var method in changedType.Methods)
                            {
                                PrintDiff(method, 4);
                            }
                        }

                        if (ShouldPrintChanged(changedType.Fields))
                        {
                            WriteLine(originalColour, Properties.Resources.Fields, 3);
                            foreach (var field in changedType.Fields)
                            {
                                PrintDiff(field, 4);
                            }
                        }

                        if (ShouldPrintChanged(changedType.Events))
                        {
                            WriteLine(originalColour, Properties.Resources.Events, 3);
                            foreach (var @event in changedType.Events)
                            {
                                PrintDiff(@event, 4);
                            }
                        }

                        if (ShouldPrintChanged(changedType.Interfaces))
                        {
                            WriteLine(originalColour, Properties.Resources.Interfaces, 3);
                            foreach (var @interface in changedType.Interfaces)
                            {
                                PrintDiff(@interface, 4);
                            }
                        }
                    }
                }
            }
        }
    }
}
