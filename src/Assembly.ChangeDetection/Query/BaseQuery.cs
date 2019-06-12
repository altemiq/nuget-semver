// -----------------------------------------------------------------------
// <copyright file="BaseQuery.cs" company="GeomaticTechnologies">
// Copyright (c) GeomaticTechnologies. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.Query
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// The base query.
    /// </summary>
    internal class BaseQuery
    {
        // Common Regular expression part shared by the different queries
        private const string CommonModifiers = "!?static +|!?public +|!?protected +internal +|!?protected +|!?internal +|!?private +";

        private static Regex eventQueryParser;

        private static Regex fieldQueryParser;

        private static Regex methodDefParser;

        /// <summary>
        /// Initialises a new instance of the <see cref="BaseQuery"/> class.
        /// </summary>
        /// <param name="query">The query.</param>
        protected BaseQuery(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query), "query string was empty");
            }
        }

        /// <summary>
        /// Gets the event query parser.
        /// </summary>
        internal static Regex EventQueryParser => eventQueryParser ?? (eventQueryParser = new Regex("^ *(?<modifiers>!?virtual +|event +|" + CommonModifiers + ")* *(?<eventType>[^ ]+(<.*>)?) +(?<eventName>[^ ]+) *$"));

        /// <summary>
        /// Gets the field query parser.
        /// </summary>
        internal static Regex FieldQueryParser => fieldQueryParser ?? (fieldQueryParser = new Regex(" *(?<modifiers>!?nocompilergenerated +|!?const +|!?readonlys +|" + CommonModifiers + ")* *(?<fieldType>[^ ]+(<.*>)?) +(?<fieldName>[^ ]+) *$"));

        /// <summary>
        /// Gets the method query parser.
        /// </summary>
        internal static Regex MethodDefParser => methodDefParser ?? (methodDefParser = new Regex(" *(?<modifiers>!?virtual +|" + CommonModifiers + ")*" + @"(?<retType>.*<.*>( *\[\])?|[^ (\)]*( *\[\])?) +(?<funcName>.+)\( *(?<args>.*?) *\) *"));

        /// <summary>
        /// Gets a value indicating whether this instance is internal.
        /// </summary>
        protected internal bool? IsInternal { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is private.
        /// </summary>
        protected internal bool? IsPrivate { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is protected.
        /// </summary>
        protected internal bool? IsProtected { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is protected internal.
        /// </summary>
        protected internal bool? IsProtectedInernal { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is public.
        /// </summary>
        protected internal bool? IsPublic { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is static.
        /// </summary>
        protected internal bool? IsStatic { get; private set; }

        /// <summary>
        /// Gets or sets the parser.
        /// </summary>
        protected internal Regex Parser { get; set; }

        /// <summary>
        /// Gets or sets the name filter.
        /// </summary>
        protected internal string NameFilter { get; set; }

        /// <summary>
        /// Return whether the key is a match.
        /// </summary>
        /// <param name="m">The match.</param>
        /// <param name="key">The key.</param>
        /// <returns>The result.</returns>
        protected internal virtual bool IsMatch(Match m, string key) => m.Groups[key].Success;

        /// <summary>
        /// Return whether the value is captured.
        /// </summary>
        /// <param name="m">The match.</param>
        /// <param name="value">The value.</param>
        /// <returns>The result.</returns>
        protected internal virtual bool? Captures(Match m, string value)
        {
            var notValue = "!" + value;
            foreach (Capture capture in m.Groups["modifiers"].Captures)
            {
                if (value == capture.Value.TrimEnd())
                {
                    return true;
                }

                if (notValue == capture.Value.TrimEnd())
                {
                    return false;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="m">The match.</param>
        /// <param name="groupName">The group name.</param>
        /// <returns>The value.</returns>
        protected static string GetValue(Match m, string groupName) => m.Groups[groupName].Value;

        /// <summary>
        /// Sets the modifier filters.
        /// </summary>
        /// <param name="m">The match.</param>
        protected virtual void SetModifierFilter(Match m)
        {
            this.IsProtected = this.Captures(m, "protected");
            this.IsInternal = this.Captures(m, "internal");
            this.IsProtectedInernal = this.Captures(m, "protected internal");
            this.IsPublic = this.Captures(m, "public");
            this.IsPrivate = this.Captures(m, "private");
            this.IsStatic = this.Captures(m, "static");
        }

        /// <summary>
        /// Matches the name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The result.</returns>
        protected virtual bool MatchName(string name) => string.IsNullOrEmpty(this.NameFilter)
            || this.NameFilter == "*"
            || Matcher.MatchWithWildcards(this.NameFilter, name, StringComparison.OrdinalIgnoreCase);
    }
}