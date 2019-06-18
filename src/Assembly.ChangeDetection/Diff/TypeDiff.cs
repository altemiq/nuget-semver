// -----------------------------------------------------------------------
// <copyright file="TypeDiff.cs" company="GeomaticTechnologies">
// Copyright (c) GeomaticTechnologies. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.Diff
{
    using System;
    using System.Linq;
    using Mondo.Assembly.ChangeDetection.Introspection;
    using Mondo.Assembly.ChangeDetection.Query;
    using Mono.Cecil;

    /// <summary>
    /// The type diff.
    /// </summary>
    internal sealed class TypeDiff
    {
        private static readonly TypeDefinition NoType = new TypeDefinition("noType", null, TypeAttributes.Class, null);

        private TypeDiff(TypeDefinition v1, TypeDefinition v2)
        {
            this.TypeV1 = v1;
            this.TypeV2 = v2;

            this.Methods = new DiffCollection<MethodDefinition>();
            this.Events = new DiffCollection<EventDefinition>();
            this.Fields = new DiffCollection<FieldDefinition>();
            this.Interfaces = new DiffCollection<TypeReference>();
        }

        /// <summary>
        /// Gets the default return object when the diff did not return any results.
        /// </summary>
        public static TypeDiff None { get; } = new TypeDiff(NoType, NoType);

        /// <summary>
        /// Gets the first type.
        /// </summary>
        public TypeDefinition TypeV1 { get; }

        /// <summary>
        /// Gets the second type.
        /// </summary>
        public TypeDefinition TypeV2 { get; }

        /// <summary>
        /// Gets the methods.
        /// </summary>
        public DiffCollection<MethodDefinition> Methods { get; }

        /// <summary>
        /// Gets the events.
        /// </summary>
        public DiffCollection<EventDefinition> Events { get; }

        /// <summary>
        /// Gets the fields.
        /// </summary>
        public DiffCollection<FieldDefinition> Fields { get; }

        /// <summary>
        /// Gets the interfaces.
        /// </summary>
        public DiffCollection<TypeReference> Interfaces { get; }

        /// <summary>
        /// Gets a value indicating whether the base type has changes.
        /// </summary>
        public bool HasChangedBaseType { get; private set; }

        /// <summary>Checks if the type has changes.
        /// <list type="bullet">
        ///   <item><description>On type level</description>.</item>
        ///   <item><description>Base Types, implemented interfaces, generic parameters.</description></item>
        ///   <item><description>On method level.</description></item>
        ///   <item><description>Method modifiers, return type, generic parameters, parameter count, parameter types (also generics)</description></item>
        ///   <item><description>On field level</description></item>
        ///   <item><description>Field types</description></item>
        /// </list>
        /// </summary>
        /// <param name="typeV1">The type v1.</param>
        /// <param name="typeV2">The type v2.</param>
        /// <param name="diffQueries">The diff queries.</param>
        /// <returns>The type difference.</returns>
        public static TypeDiff GenerateDiff(TypeDefinition typeV1, TypeDefinition typeV2, QueryAggregator diffQueries)
        {
            if (typeV1 == null)
            {
                throw new ArgumentNullException(nameof(typeV1));
            }

            if (typeV2 == null)
            {
                throw new ArgumentNullException(nameof(typeV2));
            }

            if (diffQueries == null || diffQueries.FieldQueries.Count == 0 || diffQueries.MethodQueries.Count == 0)
            {
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.DiffQueriesWasNull, nameof(diffQueries)), nameof(diffQueries));
            }

            var diff = new TypeDiff(typeV1, typeV2);

            diff.DoDiff(diffQueries);

            if (diff.HasChangedBaseType || diff.Events.Count != 0 || diff.Fields.Count != 0 || diff.Interfaces.Count != 0 || diff.Methods.Count != 0)
            {
                return diff;
            }

            return None;
        }

        /// <inheritdoc/>
        public override string ToString() => string.Format(System.Globalization.CultureInfo.CurrentCulture, "Type: {0}, Changed Methods: {1}, Fields: {2}, Events: {3}, Interfaces: {4}", this.TypeV1, this.Methods.Count, this.Fields.Count, this.Events.Count, this.Interfaces.Count);

        private static bool IsSameBaseType(TypeDefinition t1, TypeDefinition t2)
        {
            if (t1 is null && t2 is null)
            {
                return true;
            }

            if ((t1 == null && t2 != null) || (t1 != null && t2 == null))
            {
                return false;
            }

            if (t1.BaseType == null && t2.BaseType == null)
            {
                return true;
            }

            // compare base type
            if ((t1.BaseType != null && t2.BaseType == null) || (t1.BaseType == null && t2.BaseType != null))
            {
                return false;
            }

            return t1.BaseType.FullName == t2.BaseType.FullName;
        }

        private void DoDiff(QueryAggregator diffQueries)
        {
            // Interfaces have no base type
            if (!this.TypeV1.IsInterface)
            {
                this.HasChangedBaseType = !IsSameBaseType(this.TypeV1, this.TypeV2);
            }

            this.DiffImplementedInterfaces();
            this.DiffFields(diffQueries);
            this.DiffMethods(diffQueries);
            this.DiffEvents(diffQueries);
        }

        private void DiffImplementedInterfaces()
        {
            // search for removed interfaces
            foreach (var baseV1 in this.TypeV1.Interfaces.Select(i => i.InterfaceType))
            {
                if (!this.TypeV2.Interfaces.Select(i => i.InterfaceType).Any(baseV2 => baseV2.IsEqual(baseV1)))
                {
                    this.Interfaces.Add(new DiffResult<TypeReference>(baseV1, new DiffOperation(false)));
                }
            }

            // search for added interfaces
            foreach (var baseV2 in this.TypeV2.Interfaces.Select(i => i.InterfaceType))
            {
                if (!this.TypeV1.Interfaces.Select(i => i.InterfaceType).Any(baseV1 => baseV1.IsEqual(baseV2)))
                {
                    this.Interfaces.Add(new DiffResult<TypeReference>(baseV2, new DiffOperation(true)));
                }
            }
        }

        private void DiffFields(QueryAggregator diffQueries)
        {
            var fieldsV1 = diffQueries.ExecuteAndAggregateFieldQueries(this.TypeV1);
            var fieldsV2 = diffQueries.ExecuteAndAggregateFieldQueries(this.TypeV2);

            var fieldDiffer = new ListDiffer<FieldDefinition>(this.CompareFieldsByTypeAndName);
            fieldDiffer.Diff(fieldsV1, fieldsV2, addedField => this.Fields.Add(new DiffResult<FieldDefinition>(addedField, new DiffOperation(true))), removedField => this.Fields.Add(new DiffResult<FieldDefinition>(removedField, new DiffOperation(false))));
        }

        private bool CompareFieldsByTypeAndName(FieldDefinition fieldV1, FieldDefinition fieldV2) => fieldV1.IsEqual(fieldV2);

        private void DiffMethods(QueryAggregator diffQueries)
        {
            var methodsV1 = diffQueries.ExecuteAndAggregateMethodQueries(this.TypeV1);
            var methodsV2 = diffQueries.ExecuteAndAggregateMethodQueries(this.TypeV2);

            var differ = new ListDiffer<MethodDefinition>(this.CompareMethodByNameAndTypesIncludingGenericArguments);

            differ.Diff(methodsV1, methodsV2, added => this.Methods.Add(new DiffResult<MethodDefinition>(added, new DiffOperation(true))), removed => this.Methods.Add(new DiffResult<MethodDefinition>(removed, new DiffOperation(false))));
        }

        private bool CompareMethodByNameAndTypesIncludingGenericArguments(MethodDefinition m1, MethodDefinition m2) => m1.IsEqual(m2);

        private void DiffEvents(QueryAggregator diffQueries)
        {
            var eventsV1 = diffQueries.ExecuteAndAggregateEventQueries(this.TypeV1);
            var eventsV2 = diffQueries.ExecuteAndAggregateEventQueries(this.TypeV2);

            var differ = new ListDiffer<EventDefinition>(this.CompareEvents);

            differ.Diff(eventsV1, eventsV2, added => this.Events.Add(new DiffResult<EventDefinition>(added, new DiffOperation(true))), removed => this.Events.Add(new DiffResult<EventDefinition>(removed, new DiffOperation(false))));
        }

        private bool CompareEvents(EventDefinition evV1, EventDefinition evV2) => evV1.IsEqual(evV2);
    }
}