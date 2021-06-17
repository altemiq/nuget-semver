// -----------------------------------------------------------------------
// <copyright file="BaseQuery.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.Query
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// The base query.
    /// </summary>
    internal abstract class BaseQuery
    {
        // Common Regular expression part shared by the different queries
        private const string CommonModifiers = "!?static +|!?public +|!?protected +internal +|!?protected +|!?internal +|!?private +";

        /// <summary>
        /// Initialises a new instance of the <see cref="BaseQuery"/> class.
        /// </summary>
        /// <param name="query">The query.</param>
        protected BaseQuery(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }
        }

        /// <summary>
        /// Gets the event query parser.
        /// </summary>
        internal static Regex EventQueryParser { get; } = new Regex("^ *(?<modifiers>!?virtual +|event +|" + CommonModifiers + ")* *(?<eventType>[^ ]+(<.*>)?) +(?<eventName>[^ ]+) *$", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(3));

        /// <summary>
        /// Gets the field query parser.
        /// </summary>
        internal static Regex FieldQueryParser { get; } = new Regex(" *(?<modifiers>!?nocompilergenerated +|!?const +|!?readonlys +|" + CommonModifiers + ")* *(?<fieldType>[^ ]+(<.*>)?) +(?<fieldName>[^ ]+) *$", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(3));

        /// <summary>
        /// Gets the method query parser.
        /// </summary>
        internal static Regex MethodDefParser { get; } = new Regex(" *(?<modifiers>!?virtual +|" + CommonModifiers + ")*" + @"(?<retType>.*<.*>( *\[\])?|[^ (\)]*( *\[\])?) +(?<funcName>.+)\( *(?<args>.*?) *\) *", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(3));

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
        protected internal Regex? Parser { get; set; }

        /// <summary>
        /// Gets or sets the name filter.
        /// </summary>
        protected internal string? NameFilter { get; set; }

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
                if (string.Equals(value, capture.Value.TrimEnd(), StringComparison.Ordinal))
                {
                    return true;
                }

                if (string.Equals(notValue, capture.Value.TrimEnd(), StringComparison.Ordinal))
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
        protected void SetModifierFilter(Match m)
        {
            this.IsProtected = this.Captures(m, "protected");
            this.IsInternal = this.Captures(m, "internal");
            this.IsProtectedInernal = this.Captures(m, "protected internal");
            this.IsPublic = this.Captures(m, "public");
            this.IsPrivate = this.Captures(m, "private");
            this.IsStatic = this.Captures(m, "static");
            this.SetModifierFilterCore(m);
        }

        /// <summary>
        /// Sets the modifier filters.
        /// </summary>
        /// <param name="m">The match.</param>
        protected abstract void SetModifierFilterCore(Match m);

        /// <summary>
        /// Matches the name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The result.</returns>
        protected virtual bool MatchName(string name) => string.IsNullOrEmpty(this.NameFilter)
            || string.Equals(this.NameFilter, "*", StringComparison.Ordinal)
            || Matcher.MatchWithWildcards(this.NameFilter!, name, StringComparison.OrdinalIgnoreCase);
    }
}