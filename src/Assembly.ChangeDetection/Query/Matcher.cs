// -----------------------------------------------------------------------
// <copyright file="Matcher.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.Query
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Partial String matcher class which supports wildcards.
    /// </summary>
    internal static class Matcher
    {
        private const string EscapedStar = "magic_star";

        private static readonly char[] NsTrimChars = { ' ', '*', '\t' };

        /// <summary>
        /// Gets the cached filter string regular expressions for later reuse.
        /// </summary>
        internal static IDictionary<string, Regex> Filter2Regex { get; } = new Dictionary<string, Regex>();

        /// <summary>
        /// Check if a given test string does match the pattern specified by the filterString. Besides
        /// normal string comparisons for the patterns *xxx, xxx*, *xxx* which are mapped to String.EndsWith,
        /// String.StartsWith and String.Contains are regular expressions used if the pattern is more complex
        /// like *xx*bbb.
        /// </summary>
        /// <param name="filterString">
        /// Filter string. A filter string of null or * will match any testString. If the teststring is
        /// null it will never match anything.
        /// </param>
        /// <param name="testString">String to check.</param>
        /// <param name="compMode">String Comparision mode.</param>
        /// <returns>true if the teststring does match, false otherwise.</returns>
        public static bool MatchWithWildcards(string? filterString, string? testString, StringComparison compMode)
        {
            if (filterString is null || filterString == "*")
            {
                return true;
            }

            if (testString is null)
            {
                return false;
            }

            if (IsRegexMatchNecessary(filterString))
            {
                return IsMatch(filterString, testString, compMode);
            }

            var bMatchEnd = false;
            if (filterString.StartsWith("*", compMode))
            {
                bMatchEnd = true;
            }

            var bMatchStart = false;
            if (filterString.EndsWith("*", compMode))
            {
                bMatchStart = true;
            }

            var filterSubstring = filterString.Trim(NsTrimChars);

            if (bMatchStart && bMatchEnd)
            {
                return compMode == StringComparison.OrdinalIgnoreCase || compMode == StringComparison.InvariantCultureIgnoreCase
                    ? testString.IndexOf(filterSubstring, StringComparison.OrdinalIgnoreCase) >= 0
                    : testString.Contains(filterSubstring);
            }

            if (bMatchStart)
            {
                return testString.StartsWith(filterSubstring, compMode);
            }

            if (bMatchEnd)
            {
                return testString.EndsWith(filterSubstring, compMode);
            }

            return string.Equals(testString, filterSubstring, compMode);
        }

        /// <summary>
        /// Checks if * occurs inside filter string and not only at start or end.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>The result.</returns>
        internal static bool IsRegexMatchNecessary(string filter)
        {
            var start = Math.Min(1, Math.Max(filter.Length - 1, 0));
            var len = Math.Max(filter.Length - 2, 0);
            return filter.IndexOf("*", start, len, StringComparison.Ordinal) != -1;
        }

        /// <summary>
        /// Returns wether the filter is a match.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="testString">The test string.</param>
        /// <param name="mode">The comparison mode.</param>
        /// <returns>The result.</returns>
        internal static bool IsMatch(string filter, string testString, StringComparison mode) => GenerateRegexFromFilter(filter, mode).IsMatch(testString);

        /// <summary>
        /// Generates the <see cref="Regex"/> from the filter.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="mode">The comparison mode.</param>
        /// <returns>The regular expression.</returns>
        internal static Regex GenerateRegexFromFilter(string filter, StringComparison mode)
        {
            if (Filter2Regex.TryGetValue(filter, out var regex))
            {
                return regex;
            }

            var rex = "^" + Regex.Escape(filter.Replace("*", EscapedStar)) + "$";
            regex = new Regex(rex.Replace(EscapedStar, ".*?"), (mode == StringComparison.CurrentCultureIgnoreCase || mode == StringComparison.InvariantCultureIgnoreCase || mode == StringComparison.OrdinalIgnoreCase) ? RegexOptions.IgnoreCase : RegexOptions.None);
            Filter2Regex.Add(filter, regex);
            return regex;
        }
    }
}