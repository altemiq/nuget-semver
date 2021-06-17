// -----------------------------------------------------------------------
// <copyright file="MethodQuery.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Mondo.Assembly.ChangeDetection.Introspection;
    using Mono.Cecil;

    /// <summary>
    /// The method query.
    /// </summary>
    internal class MethodQuery : BaseQuery
    {
        private const string All = " * *(*)";

        private static readonly char[] ArgTrimChars = new char[] { '[', ']', ',' };

        private readonly IList<(Regex, string)>? argumentFilters;

        private Regex? returnTypeFilter;

        /// <summary>
        /// Initialises a new instance of the <see cref="MethodQuery"/> class which matches every method.
        /// </summary>
        public MethodQuery()
            : this("*")
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MethodQuery"/> class to match for specific methods for a given type.
        /// </summary>
        /// <remarks>The query format can be a simple string like
        /// * // get everything
        /// public void Function(int firstArg, bool secondArg)  // match specfic method
        /// public * *( * ) // match all public methods
        /// protected * *(* a) // match all protected methods with one parameter.
        /// </remarks>
        /// <param name="methodQuery">The method query.</param>
        public MethodQuery(string methodQuery)
            : base(methodQuery)
        {
            // Return everything if no filter is set
            if (string.Equals(methodQuery.Trim(), "*", StringComparison.Ordinal))
            {
                return;
            }

            // Get cached instance
            this.Parser = MethodDefParser;

            // otherwise we expect a filter query that looks like a function definition
            var m = this.Parser.Match(methodQuery.Trim());

            if (!m.Success)
            {
                throw new ArgumentException(string.Format(Properties.Resources.Culture, "Invalid method query: \"{0}\". The method query must be of the form <modifier> <return type> <function name>(<arguments>) e.g. public void F(*) match all public methods with name F with 0 or more arguments, or public * *(*) match any public method.", methodQuery), nameof(methodQuery));
            }

            this.CreateReturnTypeFilter(m);

            this.NameFilter = m.Groups["funcName"].Value;
            var idx = this.NameFilter.IndexOf('<');
            if (idx != -1)
            {
                this.NameFilter = this.NameFilter.Substring(0, idx);
            }

            if (string.IsNullOrEmpty(this.NameFilter))
            {
                this.NameFilter = default;
            }

            this.argumentFilters = InitArgumentFilter(m.Groups["args"].Value);

            this.SetModifierFilter(m);
        }

        /// <summary>
        /// Gets the query for all methods.
        /// </summary>
        public static MethodQuery AllMethods => new();

        /// <summary>
        /// Gets the query for protected methods.
        /// </summary>
        public static MethodQuery ProtectedMethods => new("protected " + All);

        /// <summary>
        /// Gets the query for internal methods.
        /// </summary>
        public static MethodQuery InternalMethods => new("internal " + All);

        /// <summary>
        /// Gets the query for public methods.
        /// </summary>
        public static MethodQuery PublicMethods => new("public " + All);

        /// <summary>
        /// Gets the query for private methods.
        /// </summary>
        public static MethodQuery PrivateMethods => new("private " + All);

        /// <summary>
        /// Gets or sets a value indicating whether this is virtual.
        /// </summary>
        protected internal bool? MyIsVirtual { get; set; }

        /// <summary>
        /// Gets a single method.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The method.</returns>
        public MethodDefinition? GetSingleMethod(TypeDefinition type) => this.GetMethods(type) switch
        {
            IList<MethodDefinition> matches when matches.Count > 1 => throw new InvalidOperationException(string.Format(Properties.Resources.Culture, Properties.Resources.GotMoreThanOneMatchingMethod, matches.Count)),
            IList<MethodDefinition> matches when matches.Count == 0 => default,
            IList<MethodDefinition> matches => matches[0],
            _ => default,
        };

        /// <summary>
        /// Gets the methods.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The methods.</returns>
        public virtual IList<MethodDefinition> GetMethods(TypeDefinition type) => type.Methods.Where(method => this.Match(type, method)).ToArray();

        /// <summary>
        /// Initialises the argument filter.
        /// </summary>
        /// <param name="argFilter">The input filter.</param>
        /// <returns>The filters.</returns>
        internal static IList<(Regex, string)>? InitArgumentFilter(string argFilter)
        {
            if (argFilter is null || string.Equals(argFilter, "*", StringComparison.Ordinal))
            {
                return default;
            }

            // To query for void methods
            if (argFilter.Length == 0)
            {
                return new List<(Regex, string)>();
            }

            var inGeneric = 0;

            var bIsType = true;
            var list = new List<(Regex, string)>();
            var curThing = new StringBuilder();
            var curType = default(string?);

            var prev = '\0';
            char current;
            for (var i = 0; i < argFilter.Length; i++)
            {
                current = argFilter[i];

                if (current != ' ')
                {
                    curThing.Append(current);
                }

                if (current == '<')
                {
                    inGeneric++;
                }
                else if (current == '>')
                {
                    inGeneric--;
                }

                if (inGeneric > 0)
                {
                    continue;
                }

                if (i > 0)
                {
                    prev = argFilter[i - 1];
                }

                // ignore subsequent spaces
                if (current == ' ' && prev == ' ')
                {
                    continue;
                }

                // Got end of file argument name
                if (current == ',' && curThing.Length > 0)
                {
                    curThing.Remove(curThing.Length - 1, 1);
                    var curArgName = curThing.ToString().Trim();
                    curThing.Length = 0;

                    if (curType is null || curArgName is null)
                    {
                        throw new ArgumentException(string.Format(Properties.Resources.Culture, "Method argument filter is of wrong format: {0}", argFilter), nameof(argFilter));
                    }

                    list.Add(AssignArrayBracketsToTypeName(curType, curArgName));
                    curType = default;
                    bIsType = true;
                }

                if (current == ' ' && curThing.Length > 0 && bIsType)
                {
                    curType = GenericTypeMapper.ConvertClrTypeName(curThing.ToString().Trim());
                    curThing.Length = 0;
                    bIsType = false;
                }
            }

            if (curType is not null)
            {
                list.Add(AssignArrayBracketsToTypeName(curType, curThing.ToString().Trim()));
            }

            return list;
        }

        /// <summary>
        /// Does the method match the return type.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>The result.</returns>
        internal bool MatchReturnType(MethodDefinition method) => this.returnTypeFilter?.IsMatch(method.ReturnType.FullName) != false;

        /// <summary>
        /// Does the method match the argument.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>The result.</returns>
        internal bool MatchArguments(MethodDefinition method)
        {
            // Query all methods regardless number of parameters
            if (this.argumentFilters is null)
            {
                return true;
            }

            if (this.argumentFilters.Count != method.Parameters.Count)
            {
                return false;
            }

            for (var i = 0; i < this.argumentFilters.Count; i++)
            {
                var curDef = method.Parameters[i];
                var curFilters = this.argumentFilters[i];

                if (!IsArgumentMatch(curFilters.Item1, curFilters.Item2, curDef.ParameterType.FullName, curDef.Name))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Does the type and method match.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="method">The method.</param>
        /// <returns>The result.</returns>
        internal bool Match(TypeDefinition type, MethodDefinition method)
        {
            var lret = this.MatchMethodModifiers(method);

            if (lret)
            {
                lret = this.MatchName(method.Name);
                if (string.Equals(method.Name, ".ctor", StringComparison.Ordinal))
                {
                    lret = this.MatchName(method.DeclaringType.Name);
                }
            }

            return lret && this.MatchReturnType(method) && this.MatchArguments(method) && IsNoEventMethod(type, method);
        }

        /// <inheritdoc/>
        protected override void SetModifierFilterCore(Match m) => this.MyIsVirtual = this.Captures(m, "virtual");

        /// <summary>
        /// Match the method modifiers.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>The result.</returns>
        protected bool MatchMethodModifiers(MethodDefinition method)
        {
            var lret = true;

            if (this.IsPublic.HasValue)
            {
                lret = method.IsPublic == this.IsPublic;
            }

            if (lret && this.IsInternal.HasValue)
            {
                lret = method.IsAssembly == this.IsInternal;
            }

            if (lret && this.IsPrivate.HasValue)
            {
                lret = method.IsPrivate == this.IsPrivate;
            }

            if (lret && this.IsProtectedInernal.HasValue)
            {
                lret = method.IsFamilyOrAssembly == this.IsProtectedInernal;
            }

            if (lret && this.IsProtected.HasValue)
            {
                lret = method.IsFamily == this.IsProtected;
            }

            if (lret && this.MyIsVirtual.HasValue)
            {
                lret = method.IsVirtual == this.MyIsVirtual;
            }

            if (lret && this.IsStatic.HasValue)
            {
                lret = method.IsStatic == this.IsStatic;
            }

            return lret;
        }

        private static string PrependStarToFilter(string filterstr) => string.IsNullOrEmpty(filterstr) || filterstr.StartsWith("*", StringComparison.Ordinal) ? filterstr : "*" + filterstr;

        private static bool IsNoEventMethod(TypeDefinition type, MethodDefinition method)
        {
            if (method.IsSpecialName)
            {
                // Is usually either a property or event add/remove method
                foreach (var ev in type.Events)
                {
                    if (ev.AddMethod.IsEqual(method)
                        || ev.RemoveMethod.IsEqual(method))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static string CreateRegexFilterFromTypeName(string filterstr) => string.IsNullOrEmpty(filterstr) || filterstr.StartsWith(".*", StringComparison.Ordinal) || filterstr.StartsWith("*", StringComparison.Ordinal)
            ? filterstr
            : ".*" + filterstr;

        private static bool IsArgumentMatch(Regex typeFilter, string argNameFilter, string typeName, string argName) => typeFilter.Match(typeName).Success
                && Matcher.MatchWithWildcards(argNameFilter, argName, StringComparison.OrdinalIgnoreCase);

        private static Regex CreateRegularExpressionFromTypeString(string newTypeName)
        {
            newTypeName = CreateRegexFilterFromTypeName(newTypeName);
            if (newTypeName.StartsWith("*", StringComparison.Ordinal))
            {
                newTypeName = "." + newTypeName;
            }

            newTypeName = GenericTypeMapper.TransformGenericTypeNames(newTypeName, CreateRegexFilterFromTypeName);

            newTypeName = Regex.Escape(newTypeName);

            // unescape added wild cards
            newTypeName = newTypeName.Replace("\\.\\*", ".*");
            return new Regex(newTypeName, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(3));
        }

        private static (Regex TypeFilter, string ArgName) AssignArrayBracketsToTypeName(string typeName, string argName)
        {
            var newTypeName = typeName;
            var newArgName = argName;

            if (argName.StartsWith("[", StringComparison.Ordinal))
            {
                newTypeName += argName.Substring(0, argName.LastIndexOf(']') + 1);
                newArgName = newArgName.Trim(ArgTrimChars);
            }

            newArgName = PrependStarToFilter(newArgName);
            var typeFilter = CreateRegularExpressionFromTypeString(newTypeName);

            return (typeFilter, newArgName);
        }

        private void CreateReturnTypeFilter(Match m)
        {
            var filter = m.Groups["retType"].Value.Replace(" ", string.Empty);

            if (!string.IsNullOrEmpty(filter))
            {
                this.returnTypeFilter = CreateRegularExpressionFromTypeString(filter);
            }
        }
    }
}