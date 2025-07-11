// -----------------------------------------------------------------------
// <copyright file="TypeDiff.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.Diff;

using Altemiq.Assembly.ChangeDetection.Introspection;
using Altemiq.Assembly.ChangeDetection.Query;
using Mono.Cecil;

/// <summary>
/// The type diff.
/// </summary>
public sealed class TypeDiff
{
    private static readonly TypeDefinition NoType = new("noType", name: null, TypeAttributes.Class, baseType: null);

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

    /// <inheritdoc/>
    public override string ToString() => string.Format(Properties.Resources.Culture, "Type: {0}, Changed Methods: {1}, Fields: {2}, Events: {3}, Interfaces: {4}", this.TypeV1, this.Methods.Count, this.Fields.Count, this.Events.Count, this.Interfaces.Count);

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
    internal static TypeDiff GenerateDiff(TypeDefinition typeV1, TypeDefinition typeV2, QueryAggregator diffQueries)
    {
        if (typeV1 is null)
        {
            throw new ArgumentNullException(nameof(typeV1));
        }

        if (typeV2 is null)
        {
            throw new ArgumentNullException(nameof(typeV2));
        }

        if (diffQueries is null || diffQueries.FieldQueries.Count == 0 || diffQueries.MethodQueries.Count == 0)
        {
            throw new ArgumentException(string.Format(Properties.Resources.Culture, Properties.Resources.DiffQueriesWasNull, nameof(diffQueries)), nameof(diffQueries));
        }

        var diff = new TypeDiff(typeV1, typeV2);

        diff.DoDiff(diffQueries);

        if (diff.HasChangedBaseType || diff.Events.Count != 0 || diff.Fields.Count != 0 || diff.Interfaces.Count != 0 || diff.Methods.Count != 0)
        {
            return diff;
        }

        return None;
    }

    private static bool IsSameBaseType(TypeDefinition t1, TypeDefinition t2)
    {
        return CompareNull(t1, t2)
            && CompareNull(t1.BaseType, t2.BaseType)
            && string.Equals(t1.BaseType.FullName, t2.BaseType.FullName, StringComparison.Ordinal);

        static bool CompareNull(object o1, object o2)
        {
            return o1 is null ? o2 is null : o2 is not null;
        }
    }

    private static bool CompareFieldsByTypeAndName(FieldDefinition fieldV1, FieldDefinition fieldV2) => fieldV1.IsEqual(fieldV2);

    private static bool CompareMethodByNameAndTypesIncludingGenericArguments(MethodDefinition m1, MethodDefinition m2) => m1.IsEqual(m2);

    private static bool CompareEvents(EventDefinition evV1, EventDefinition evV2) => evV1.IsEqual(evV2);

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
        foreach (var baseV1 in this.TypeV1.Interfaces.Select(i => i.InterfaceType)
                     .Where(baseV1 => !this.TypeV2.Interfaces.Select(i => i.InterfaceType).Any(baseV2 => baseV2.IsEqual(baseV1))))
        {
            this.Interfaces.Add(new DiffResult<TypeReference>(baseV1, new DiffOperation(isAdded: false)));
        }

        // search for added interfaces
        foreach (var baseV2 in this.TypeV2.Interfaces.Select(i => i.InterfaceType)
                     .Where(baseV2 => !this.TypeV1.Interfaces.Select(i => i.InterfaceType).Any(baseV1 => baseV1.IsEqual(baseV2))))
        {
            this.Interfaces.Add(new DiffResult<TypeReference>(baseV2, new DiffOperation(isAdded: true)));
        }
    }

    private void DiffFields(QueryAggregator diffQueries)
    {
        var fieldsV1 = diffQueries.ExecuteAndAggregateFieldQueries(this.TypeV1);
        var fieldsV2 = diffQueries.ExecuteAndAggregateFieldQueries(this.TypeV2);

        var fieldDiffer = new ListDiffer<FieldDefinition>(CompareFieldsByTypeAndName);
        fieldDiffer.Diff(fieldsV1, fieldsV2, addedField => this.Fields.Add(new DiffResult<FieldDefinition>(addedField, new DiffOperation(isAdded: true))), removedField => this.Fields.Add(new DiffResult<FieldDefinition>(removedField, new DiffOperation(isAdded: false))));
    }

    private void DiffMethods(QueryAggregator diffQueries)
    {
        var methodsV1 = diffQueries.ExecuteAndAggregateMethodQueries(this.TypeV1);
        var methodsV2 = diffQueries.ExecuteAndAggregateMethodQueries(this.TypeV2);

        var differ = new ListDiffer<MethodDefinition>(CompareMethodByNameAndTypesIncludingGenericArguments);

        differ.Diff(methodsV1, methodsV2, added => this.Methods.Add(new DiffResult<MethodDefinition>(added, new DiffOperation(isAdded: true))), removed => this.Methods.Add(new DiffResult<MethodDefinition>(removed, new DiffOperation(isAdded: false))));
    }

    private void DiffEvents(QueryAggregator diffQueries)
    {
        var eventsV1 = diffQueries.ExecuteAndAggregateEventQueries(this.TypeV1);
        var eventsV2 = diffQueries.ExecuteAndAggregateEventQueries(this.TypeV2);

        var differ = new ListDiffer<EventDefinition>(CompareEvents);

        differ.Diff(eventsV1, eventsV2, added => this.Events.Add(new DiffResult<EventDefinition>(added, new DiffOperation(isAdded: true))), removed => this.Events.Add(new DiffResult<EventDefinition>(removed, new DiffOperation(isAdded: false))));
    }
}