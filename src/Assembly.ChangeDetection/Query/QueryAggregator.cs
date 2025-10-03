// -----------------------------------------------------------------------
// <copyright file="QueryAggregator.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.Query;

using Introspection;
using Mono.Cecil;

/// <summary>
/// The query aggregator.
/// </summary>
internal class QueryAggregator
{
    /// <summary>
    /// Gets the public API queries.
    /// Contains also internal types, fields and methods since the InteralsVisibleToAttribute can open visibility.
    /// </summary>
    public static QueryAggregator PublicApiQueries
    {
        get
        {
            var agg = new QueryAggregator();

            agg.TypeQueries.Add(new TypeQuery(TypeQueryMode.ApiRelevant));

            agg.MethodQueries.Add(MethodQuery.PublicMethods);
            agg.MethodQueries.Add(MethodQuery.ProtectedMethods);

            agg.FieldQueries.Add(FieldQuery.PublicFields);
            agg.FieldQueries.Add(FieldQuery.ProtectedFields);

            agg.EventQueries.Add(EventQuery.PublicEvents);
            agg.EventQueries.Add(EventQuery.ProtectedEvents);

            return agg;
        }
    }

    /// <summary>
    /// Gets the external visible API queries.
    /// </summary>
    public static QueryAggregator AllExternallyVisibleApis
    {
        get
        {
            var agg = PublicApiQueries;
            agg.TypeQueries.Add(new TypeQuery(TypeQueryMode.Internal));
            agg.MethodQueries.Add(MethodQuery.InternalMethods);
            agg.FieldQueries.Add(FieldQuery.InteralFields);
            agg.EventQueries.Add(EventQuery.InternalEvents);
            return agg;
        }
    }

    /// <summary>
    /// Gets the event queries.
    /// </summary>
    public IList<EventQuery> EventQueries { get; } = new List<EventQuery>();

    /// <summary>
    /// Gets the field queries.
    /// </summary>
    public IList<FieldQuery> FieldQueries { get; } = new List<FieldQuery>();

    /// <summary>
    /// Gets the method queries.
    /// </summary>
    public IList<MethodQuery> MethodQueries { get; } = new List<MethodQuery>();

    /// <summary>
    /// Gets the type queries.
    /// </summary>
    public IList<TypeQuery> TypeQueries { get; } = new List<TypeQuery>();

    /// <summary>
    /// Executes and aggregates the type queries.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <returns>The type definitions.</returns>
    public IList<TypeDefinition> ExeuteAndAggregateTypeQueries(AssemblyDefinition assembly)
    {
        var result = this.TypeQueries.SelectMany(query => query.GetTypes(assembly));

        if (this.TypeQueries.Count > 1)
        {
            result = result.Distinct(new TypeNameComparer());
        }

        return result.ToArray();
    }

    /// <summary>
    /// Executes and aggregates the method queries.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The method definitions.</returns>
    public IList<MethodDefinition> ExecuteAndAggregateMethodQueries(TypeDefinition type)
    {
        var result = this.MethodQueries.SelectMany(query => query.GetMethods(type));

        if (this.MethodQueries.Count > 1)
        {
            result = result.Distinct(new MethodComparer());
        }

        return result.ToArray();
    }

    /// <summary>
    /// Executes and aggregates the field queries.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The field definitions.</returns>
    public IList<FieldDefinition> ExecuteAndAggregateFieldQueries(TypeDefinition type)
    {
        var result = this.FieldQueries.SelectMany(query => query.GetMatchingFields(type));

        if (this.FieldQueries.Count > 1)
        {
            result = result.Distinct(new FieldComparer());
        }

        return result.ToArray();
    }

    /// <summary>
    /// Executes and aggregates the event queries.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The event definitions.</returns>
    public IList<EventDefinition> ExecuteAndAggregateEventQueries(TypeDefinition type)
    {
        var result = this.EventQueries.SelectMany(query => query.GetMatchingEvents(type));

        if (this.EventQueries.Count > 1)
        {
            result = result.Distinct(new EventComparer());
        }

        return result.ToArray();
    }

    private sealed class TypeNameComparer : IEqualityComparer<TypeDefinition>
    {
        public bool Equals(TypeDefinition x, TypeDefinition y) => string.Equals(x.FullName, y.FullName, StringComparison.Ordinal);

        public int GetHashCode(TypeDefinition obj) => StringComparer.Ordinal.GetHashCode(obj.Name);
    }

    private sealed class MethodComparer : IEqualityComparer<MethodDefinition>
    {
        public bool Equals(MethodDefinition x, MethodDefinition y) => x.IsEqual(y);

        public int GetHashCode(MethodDefinition obj) => StringComparer.Ordinal.GetHashCode(obj.Name);
    }

    private sealed class FieldComparer : IEqualityComparer<FieldDefinition>
    {
        public bool Equals(FieldDefinition x, FieldDefinition y) => x.IsEqual(y);

        public int GetHashCode(FieldDefinition obj) => StringComparer.Ordinal.GetHashCode(obj.Name);
    }

    private sealed class EventComparer : IEqualityComparer<EventDefinition>
    {
        public bool Equals(EventDefinition x, EventDefinition y) => x.AddMethod.IsEqual(y.AddMethod);

        public int GetHashCode(EventDefinition obj) => StringComparer.Ordinal.GetHashCode(obj.AddMethod.Name);
    }
}