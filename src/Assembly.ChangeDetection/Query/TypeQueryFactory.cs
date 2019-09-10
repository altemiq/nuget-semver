// -----------------------------------------------------------------------
// <copyright file="TypeQueryFactory.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Parser for a list of type queries separated by ;. A type query can.
    /// </summary>
    internal class TypeQueryFactory
    {
        private static readonly Regex QueryParser = new Regex("^ *(?<modifiers>api +|nocompiler +|public +|internal +|class +|struct +|interface +|enum +)* *(?<typeName>[^ ]+) *$");

        /// <summary>
        /// Parse a list of type queries separated by ; and return the resulting type query list.
        /// </summary>
        /// <param name="queries">The queries.</param>
        /// <returns>The list of queries.</returns>
        public IList<TypeQuery> GetQueries(string queries) => this.GetQueries(queries, TypeQueryMode.None);

        /// <summary>
        /// Gets the queries.
        /// </summary>
        /// <param name="typeQueries">The type queries string.</param>
        /// <param name="additionalFlags">The additional flags.</param>
        /// <returns>The type queries.</returns>
        public IList<TypeQuery> GetQueries(string typeQueries, TypeQueryMode additionalFlags)
        {
            if (typeQueries == null)
            {
                throw new ArgumentNullException(nameof(typeQueries));
            }

            var trimedQuery = typeQueries.Trim();
            if (trimedQuery?.Length == 0)
            {
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.WasAnEmptyString, nameof(typeQueries)), nameof(typeQueries));
            }

            var queries = trimedQuery.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            return queries.Select(query =>
            {
                var m = QueryParser.Match(query);
                if (!m.Success)
                {
                    throw new ArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.IncorrectTypeQuery, query));
                }

                var mode = this.GetQueryMode(m);
                var (@namespace, type) = SplitNameSpaceAndType(m.Groups["typeName"].Value);
                var typeQuery = new TypeQuery(mode, @namespace, type);
                if (typeQuery.SearchMode == TypeQueryMode.None)
                {
                    typeQuery.SearchMode |= additionalFlags;
                }

                return typeQuery;
            }).ToArray();
        }

        /// <summary>
        /// Splites the namespace and type.
        /// </summary>
        /// <param name="fullQualifiedTypeName">The fully qualified name.</param>
        /// <returns>The namespace and type.</returns>
        internal static (string @namespace, string type) SplitNameSpaceAndType(string fullQualifiedTypeName)
        {
            if (string.IsNullOrEmpty(fullQualifiedTypeName))
            {
                throw new ArgumentNullException(nameof(fullQualifiedTypeName));
            }

            var parts = fullQualifiedTypeName.Trim().Split('.');
            if (parts.Length > 1)
            {
                return (string.Join(".", parts, 0, parts.Length - 1), parts[parts.Length - 1]);
            }

            return (default(string), parts[0]);
        }

        /// <summary>
        /// Captures the value.
        /// </summary>
        /// <param name="m">The match.</param>
        /// <param name="value">The value.</param>
        /// <returns>The result.</returns>
        protected internal virtual bool Captures(Match m, string value) => m.Groups["modifiers"].Captures.OfType<Capture>().Any(capture => value == capture.Value.TrimEnd());

        private TypeQueryMode GetQueryMode(Match m)
        {
            var mode = TypeQueryMode.None;

            if (this.Captures(m, "public"))
            {
                mode |= TypeQueryMode.Public;
            }

            if (this.Captures(m, "internal"))
            {
                mode |= TypeQueryMode.Internal;
            }

            if (this.Captures(m, "class"))
            {
                mode |= TypeQueryMode.Class;
            }

            if (this.Captures(m, "interface"))
            {
                mode |= TypeQueryMode.Interface;
            }

            if (this.Captures(m, "struct"))
            {
                mode |= TypeQueryMode.ValueType;
            }

            if (this.Captures(m, "enum"))
            {
                mode |= TypeQueryMode.Enum;
            }

            if (this.Captures(m, "nocompiler"))
            {
                mode |= TypeQueryMode.NotCompilerGenerated;
            }

            if (this.Captures(m, "api"))
            {
                mode |= TypeQueryMode.ApiRelevant;
            }

            return mode;
        }
    }
}