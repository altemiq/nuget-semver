// -----------------------------------------------------------------------
// <copyright file="DiffAssemblies.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection
{
    using System;
    using System.Collections.Generic;
    using Altemiq.Assembly.ChangeDetection.Infrastructure;

    /// <summary>
    /// Gets the difference between two assemblies.
    /// </summary>
    internal static class DiffAssemblies
    {
        /// <summary>
        /// Executes this instance.
        /// </summary>
        /// <param name="oldFiles">The old files.</param>
        /// <param name="newFiles">The new files.</param>
        /// <returns>The difference colleciton.</returns>
        public static Diff.AssemblyDiffCollection Execute(IEnumerable<FileQuery> oldFiles, IEnumerable<FileQuery> newFiles)
        {
            var oldFilesQuery = new HashSet<string>(oldFiles.GetFiles(), new FileNameComparer());
            var newFilesQuery = new HashSet<string>(newFiles.GetFiles(), new FileNameComparer());

            // Get files which are present in one set and the other
            oldFilesQuery.IntersectWith(newFilesQuery);

            var result = new Diff.AssemblyDiffCollection();

            foreach (var fileName1 in oldFilesQuery)
            {
                if (fileName1.EndsWith(".XmlSerializers.dll", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fileName2 = newFiles.GetMatchingFileByName(fileName1);

                var assemblyV1 = Introspection.AssemblyLoader.LoadCecilAssembly(fileName1);
                var assemblyV2 = Introspection.AssemblyLoader.LoadCecilAssembly(fileName2);

                if (assemblyV1 != null && assemblyV2 != null)
                {
                    var differ = new Diff.AssemblyDiffer(assemblyV1, assemblyV2);
                    var differences = differ.GenerateTypeDiff(Query.QueryAggregator.PublicApiQueries);
                    result.AddedRemovedTypes.AddRange(differences.AddedRemovedTypes);
                    result.ChangedTypes.AddRange(differences.ChangedTypes);
                }

                assemblyV1?.Dispose();
                assemblyV2?.Dispose();
            }

            return result;
        }
    }
}