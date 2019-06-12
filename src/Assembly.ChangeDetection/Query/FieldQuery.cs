// -----------------------------------------------------------------------
// <copyright file="FieldQuery.cs" company="GeomaticTechnologies">
// Copyright (c) GeomaticTechnologies. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Mono.Cecil;

    /// <summary>
    /// The field query.
    /// </summary>
    internal class FieldQuery : BaseQuery
    {
        private const string All = " * *";

        private readonly string fieldTypeFilter;

        private bool excludeCompilerGeneratedFields;

        private bool? isConst;

        private bool? isReadonly;

        /// <summary>
        /// Initialises a new instance of the <see cref="FieldQuery"/> class that searches for all fields in a class.
        /// </summary>
        public FieldQuery()
            : this("* *")
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="FieldQuery"/> class the searches for specific fields in a class.
        /// </summary>
        /// <param name="query">Query string.</param>
        /// <remarks>
        /// The field query must contain at least the field type and name to query for. Access modifier
        /// are optional
        /// Example:
        /// public * *
        /// protectd * *
        /// static readonly protected * *
        /// string m_*
        /// * my* // Get all fields which field name begins with my.
        /// </remarks>
        public FieldQuery(string query)
            : base(query)
        {
            this.Parser = FieldQueryParser;

            var match = this.Parser.Match(query);
            if (!match.Success)
            {
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture, "The field query string {0} was not a valid query.", query));
            }

            this.excludeCompilerGeneratedFields = true;
            this.SetModifierFilter(match);
            this.fieldTypeFilter = GenericTypeMapper.ConvertClrTypeName(GetValue(match, "fieldType"));

            if (!this.fieldTypeFilter.StartsWith("*", StringComparison.Ordinal))
            {
                this.fieldTypeFilter = "*" + this.fieldTypeFilter;
            }

            if (this.fieldTypeFilter == "*")
            {
                this.fieldTypeFilter = null;
            }

            this.NameFilter = GetValue(match, "fieldName");
        }

        /// <summary>
        /// Gets all fields.
        /// </summary>
        public static FieldQuery AllFields { get; } = new FieldQuery();

        /// <summary>
        /// Gets all fields including compiler generated.
        /// </summary>
        public static FieldQuery AllFieldsIncludingCompilerGenerated { get; } = new FieldQuery("!nocompilergenerated * *");

        /// <summary>
        /// Gets the public fields.
        /// </summary>
        public static FieldQuery PublicFields { get; } = new FieldQuery("public " + All);

        /// <summary>
        /// Gets the protected fields.
        /// </summary>
        public static FieldQuery ProtectedFields { get; } = new FieldQuery("protected " + All);

        /// <summary>
        /// Gets the internal fields.
        /// </summary>
        public static FieldQuery InteralFields { get; } = new FieldQuery("internal " + All);

        /// <summary>
        /// Gets the private fields.
        /// </summary>
        public static FieldQuery PrivateFields { get; } = new FieldQuery("private " + All);

        /// <summary>
        /// Returns the matching fields.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The matching fields.</returns>
        public IList<FieldDefinition> GetMatchingFields(TypeDefinition type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.Fields.Where(field => this.Match(field, type)).ToArray();
        }

        /// <summary>
        /// Gets the match.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="type">The type.</param>
        /// <returns>The match.</returns>
        internal bool Match(FieldDefinition field, TypeDefinition type) => this.MatchFieldModifiers(field)
            && this.MatchFieldType(field)
            && this.MatchName(field.Name)
            && this.excludeCompilerGeneratedFields
            && !IsEventFieldOrPropertyBackingFieldOrEnumBackingField(field, type);

        /// <inheritdoc/>
        protected override void SetModifierFilter(Match m)
        {
            base.SetModifierFilter(m);
            this.isReadonly = this.Captures(m, "readonly");
            this.isConst = this.Captures(m, "const");
            var excludeCompilerGenerated = this.Captures(m, "nocompilergenerated");
            this.excludeCompilerGeneratedFields = excludeCompilerGenerated == null || excludeCompilerGenerated.Value;
        }

        private static bool IsEventFieldOrPropertyBackingFieldOrEnumBackingField(FieldDefinition field, TypeDefinition def)
        {
            // Is Property backing field
            if (field.Name.EndsWith(">k__BackingField", StringComparison.Ordinal))
            {
                return true;
            }

            if (field.IsSpecialName)
            {
                return true;
            }

            // Is event backing field for event delegate
            return def.Events.Any(ev => ev.Name == field.Name);
        }

        private bool MatchFieldType(FieldDefinition field)
        {
            if (string.IsNullOrEmpty(this.fieldTypeFilter) || this.fieldTypeFilter == "*")
            {
                return true;
            }

            return Matcher.MatchWithWildcards(this.fieldTypeFilter, field.FieldType.FullName, StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchFieldModifiers(FieldDefinition field)
        {
            var lret = true;

            if (lret && this.isConst.HasValue)
            {
                // Literal fields are always constant so there is no need to make a distinction here
                lret = this.isConst == field.HasConstant;
            }

            if (lret && this.IsInternal.HasValue)
            {
                lret = this.IsInternal == field.IsAssembly;
            }

            if (lret && this.IsPrivate.HasValue)
            {
                lret = this.IsPrivate == field.IsPrivate;
            }

            if (lret && this.IsProtected.HasValue)
            {
                lret = this.IsProtected == field.IsFamily;
            }

            if (lret && this.IsProtectedInernal.HasValue)
            {
                lret = this.IsProtectedInernal == field.IsFamilyOrAssembly;
            }

            if (lret && this.IsPublic.HasValue)
            {
                lret = this.IsPublic == field.IsPublic;
            }

            if (lret && this.isReadonly.HasValue)
            {
                lret = this.isReadonly == field.IsInitOnly;
            }

            if (lret && this.IsStatic.HasValue)
            {
                lret = this.IsStatic == (field.IsStatic && !field.HasConstant);
            }

            return lret;
        }
    }
}