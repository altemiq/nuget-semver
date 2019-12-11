// -----------------------------------------------------------------------
// <copyright file="ListExtensions.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The list extensions.
    /// </summary>
    internal static class ListExtensions
    {
        /// <summary>
        /// Gets the search directories.
        /// </summary>
        /// <param name="queries">The queries.</param>
        /// <returns>The sreach directories.</returns>
        public static string GetSearchDirs(this IEnumerable<FileQuery> queries) => queries != null ? string.Join(";", queries.Select(q => q.SearchDir)) : throw new ArgumentNullException(nameof(queries));

        /// <summary>
        /// Gets the queries.
        /// </summary>
        /// <param name="queries">The file queries.</param>
        /// <returns>The queries.</returns>
        public static string GetQueries(this IEnumerable<FileQuery> queries) => queries != null ? string.Join(" ", queries.Select(q => q.Query)) : throw new ArgumentNullException(nameof(queries));

        /// <summary>
        /// Gets the files.
        /// </summary>
        /// <param name="queries">The queries.</param>
        /// <returns>The files.</returns>
        public static IEnumerable<string> GetFiles(this IEnumerable<FileQuery> queries)
        {
            return queries != null ? GetFilesIterator() : throw new ArgumentNullException(nameof(queries));

            IEnumerable<string> GetFilesIterator()
            {
                foreach (var q in queries)
                {
                    foreach (var file in q.EnumerateFiles)
                    {
                        yield return file;
                    }
                }
            }
        }

        /// <summary>
        /// Returns wether the sequence has any matches.
        /// </summary>
        /// <param name="queries">The queries.</param>
        /// <returns><see langword="true"/> if any items in <paramref name="queries"/> have matches; otherwise <see langword="false"/>.</returns>
        public static bool HasMatches(this IEnumerable<FileQuery> queries) => queries != null ? queries.Any(q => q.HasMatches) : throw new ArgumentNullException(nameof(queries));

        /// <summary>
        /// Gets the matching file by name.
        /// </summary>
        /// <param name="queries">The queries.</param>
        /// <param name="fileName">The file name.</param>
        /// <returns>The file.</returns>
        public static string GetMatchingFileByName(this IEnumerable<FileQuery> queries, string fileName)
        {
            if (queries is null)
            {
                throw new ArgumentNullException(nameof(queries));
            }

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException(Properties.Resources.FileNameWasNullOrEmpty, nameof(fileName));
            }

            return queries.Select(query => query.GetMatchingFileByName(fileName)).FirstOrDefault(match => match != null);
        }

        /// <summary>
        /// Gets non-existing files in other query.
        /// </summary>
        /// <param name="queries">The queries.</param>
        /// <param name="otherQueries">The other queries.</param>
        /// <returns>The non-existent files.</returns>
        public static IEnumerable<string> GetNotExistingFilesInOtherQuery(this IEnumerable<FileQuery> queries, IEnumerable<FileQuery> otherQueries)
        {
            if (queries is null)
            {
                throw new ArgumentNullException(nameof(queries));
            }

            if (otherQueries is null)
            {
                throw new ArgumentNullException(nameof(otherQueries));
            }

            var query1 = new HashSet<string>(queries.GetFiles(), new FileNameComparer());
            var query2 = new HashSet<string>(otherQueries.GetFiles(), new FileNameComparer());

            var removedFiles = new HashSet<string>(query1, new FileNameComparer());
            removedFiles.ExceptWith(query2);

            return removedFiles.ToArray();
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type in the collection.</typeparam>
        /// <param name="source">The source list.</param>
        /// <param name="collection">The collection.</param>
        public static void AddRange<T>(this IList<T> source, IEnumerable<T> collection)
        {
            if (source is List<T> list)
            {
                list.AddRange(collection);
            }
            else
            {
                foreach (var item in collection)
                {
                    source.Add(item);
                }
            }
        }
    }
}