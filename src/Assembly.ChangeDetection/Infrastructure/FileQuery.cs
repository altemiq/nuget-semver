// -----------------------------------------------------------------------
// <copyright file="FileQuery.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// The file query.
    /// </summary>
    internal class FileQuery
    {
        private readonly SearchOption searchOption;

        /// <summary>
        /// Initialises a new instance of the <see cref="FileQuery"/> class.
        /// </summary>
        /// <param name="query">The query.</param>
        public FileQuery(string query)
            : this(query, SearchOption.TopDirectoryOnly)
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="FileQuery"/> class.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="searchOption">The search option.</param>
        public FileQuery(string query, SearchOption searchOption)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }

            this.searchOption = searchOption;
            this.Query = Environment.ExpandEnvironmentVariables(query);

            var gacidx = query.IndexOf("gac:\\", StringComparison.OrdinalIgnoreCase);
            if (gacidx == 0)
            {
                if (query.Contains("*"))
                {
                    throw new ArgumentException(string.Format(Properties.Resources.Culture, "Wildcards are not supported in Global Assembly Cache search: {0}", query));
                }

                var fileName = query.Substring(5);

                var dirName = GetFileNameWithOutDllExtension(fileName);

                if (Directory.Exists(Path.Combine(GAC_32, dirName)))
                {
                    this.Query = Path.Combine(Path.Combine(GAC_32, dirName), fileName);
                }

                if (Directory.Exists(Path.Combine(GAC_MSIL, dirName)))
                {
                    this.Query = Path.Combine(Path.Combine(GAC_MSIL, dirName), fileName);
                }

                this.searchOption = SearchOption.AllDirectories;
            }
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="FileQuery"/> class.
        /// </summary>
        /// <param name="searchDir">The search directory.</param>
        /// <param name="filemask">The file mask.</param>
        public FileQuery(string searchDir, string filemask)
        {
            if (string.IsNullOrEmpty(searchDir))
            {
                throw new ArgumentNullException(nameof(searchDir));
            }

            if (string.IsNullOrEmpty(filemask))
            {
                throw new ArgumentNullException(nameof(filemask));
            }

            this.Query = Path.Combine(Environment.ExpandEnvironmentVariables(searchDir), filemask);
        }

        /// <summary>
        /// Gets the search directory.
        /// </summary>
        public string SearchDir
        {
            get
            {
                // relative directory given use current working directory
                if (!this.Query.Contains(System.IO.Path.DirectorySeparatorChar))
                {
                    return Directory.GetCurrentDirectory();
                }

                if (this.Query.StartsWith("GAC:\\", StringComparison.OrdinalIgnoreCase))
                {
                    return "GAC:\\";
                }

                // absolute directory path is already fully specified
                return Path.GetFullPath(Path.GetDirectoryName(this.Query));
            }
        }

        /// <summary>
        /// Gets the file mask.
        /// </summary>
        public string FileMask => Path.GetFileName(this.Query);

        /// <summary>
        /// Gets the query.
        /// </summary>
        public string Query { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has matches.
        /// </summary>
        public bool HasMatches => Directory.EnumerateFiles(this.SearchDir, this.FileMask, this.searchOption).Any();

        /// <summary>
        /// Gets the enumeration of the files.
        /// </summary>
        public IEnumerable<string> EnumerateFiles => Directory.Exists(this.SearchDir) ? Directory.EnumerateFiles(this.SearchDir, this.FileMask, this.searchOption) : Enumerable.Empty<string>();

        private static string GAC_32 => Path.Combine(Environment.GetEnvironmentVariable("WINDIR"), "assembly\\GAC_32");

        private static string GAC_MSIL => Path.Combine(Environment.GetEnvironmentVariable("WINDIR"), "assembly\\GAC_MSIL");

        /// <summary>
        /// Parses the query list.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The query list.</returns>
        public static IList<FileQuery> ParseQueryList(string query) => ParseQueryList(query, null, SearchOption.TopDirectoryOnly);

        /// <summary>
        /// Parses the query list.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="rootDir">The root directory.</param>
        /// <param name="searchOption">The search options.</param>
        /// <returns>The query list.</returns>
        public static IList<FileQuery> ParseQueryList(string query, string? rootDir, SearchOption searchOption) => query
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(q => new FileQuery(string.IsNullOrEmpty(rootDir) ? q.Trim() : Path.Combine(rootDir, q.Trim()), searchOption))
            .ToList();

        /// <summary>
        /// Gets the files.
        /// </summary>
        /// <returns>The files.</returns>
        public string[] GetFiles() => this.EnumerateFiles.ToArray();

        /// <summary>
        /// Gets the matching file by name.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The file.</returns>
        public string GetMatchingFileByName(string fileName) => this.EnumerateFiles
            .FirstOrDefault(file => string.Equals(Path.GetFileName(file), Path.GetFileName(fileName), StringComparison.OrdinalIgnoreCase));

        private static string GetFileNameWithOutDllExtension(string file) => file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
            ? file.Substring(0, file.Length - 4)
            : file;
    }
}