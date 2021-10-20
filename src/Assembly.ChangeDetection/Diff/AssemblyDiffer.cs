// -----------------------------------------------------------------------
// <copyright file="AssemblyDiffer.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.Diff;

using System;
using System.Collections.Generic;
using System.Linq;
using Altemiq.Assembly.ChangeDetection.Infrastructure;
using Altemiq.Assembly.ChangeDetection.Introspection;
using Altemiq.Assembly.ChangeDetection.Query;
using Mono.Cecil;

/// <summary>
/// The assembly differ.
/// </summary>
internal class AssemblyDiffer
{
    private readonly AssemblyDiffCollection myDiff = new();

    private readonly AssemblyDefinition myV1;

    private readonly AssemblyDefinition myV2;

    /// <summary>
    /// Initialises a new instance of the <see cref="AssemblyDiffer"/> class.
    /// </summary>
    /// <param name="v1">The first version.</param>
    /// <param name="v2">The second version.</param>
    public AssemblyDiffer(AssemblyDefinition v1, AssemblyDefinition v2)
    {
        this.myV1 = v1 ?? throw new ArgumentNullException(nameof(v1));
        this.myV2 = v2 ?? throw new ArgumentNullException(nameof(v2));
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="AssemblyDiffer" /> class.
    /// </summary>
    /// <param name="assemblyFileV1">The assembly file v1.</param>
    /// <param name="assemblyFileV2">The assembly file v2.</param>
    public AssemblyDiffer(string assemblyFileV1, string assemblyFileV2)
    {
        if (string.IsNullOrEmpty(assemblyFileV1))
        {
            throw new ArgumentNullException(nameof(assemblyFileV1));
        }

        if (string.IsNullOrEmpty(assemblyFileV2))
        {
            throw new ArgumentNullException(nameof(assemblyFileV2));
        }

        this.myV1 = AssemblyLoader.LoadCecilAssembly(assemblyFileV1)
            ?? throw new ArgumentException(string.Format(Properties.Resources.Culture, "Could not load assemblyV1 {0}", assemblyFileV1), nameof(assemblyFileV1));
        this.myV2 = AssemblyLoader.LoadCecilAssembly(assemblyFileV2)
            ?? throw new ArgumentException(string.Format(Properties.Resources.Culture, "Could not load assemblyV2 {0}", assemblyFileV2), nameof(assemblyFileV2));
    }

    /// <summary>
    /// Generates the type differences.
    /// </summary>
    /// <param name="queries">The queries.</param>
    /// <returns>The assembly diff collection.</returns>
    public AssemblyDiffCollection GenerateTypeDiff(QueryAggregator queries)
    {
        if (queries is null)
        {
            throw new ArgumentNullException(nameof(queries));
        }

        if (queries.TypeQueries.Count == 0)
        {
            throw new ArgumentException(Properties.Resources.QueriesContainsNoTypeQueries, nameof(queries));
        }

        var typesV1 = queries.ExeuteAndAggregateTypeQueries(this.myV1);
        var typesV2 = queries.ExeuteAndAggregateTypeQueries(this.myV2);

        var differ = new ListDiffer<TypeDefinition>(this.ShallowTypeComapare);

        differ.Diff(typesV1, typesV2, this.OnAddedType, this.OnRemovedType);

        this.DiffTypes(typesV1, typesV2, queries);

        return this.myDiff;
    }

    private static TypeDefinition GetTypeByDefinition(TypeDefinition search, IEnumerable<TypeDefinition> types) => types.FirstOrDefault(type => type.IsEqual(search));

    private void OnAddedType(TypeDefinition type) => this.myDiff.AddedRemovedTypes.Add(new DiffResult<TypeDefinition>(type, new DiffOperation(isAdded: true)));

    private void OnRemovedType(TypeDefinition type) => this.myDiff.AddedRemovedTypes.Add(new DiffResult<TypeDefinition>(type, new DiffOperation(isAdded: false)));

    private bool ShallowTypeComapare(TypeDefinition v1, TypeDefinition v2) => string.Equals(v1.FullName, v2.FullName, StringComparison.Ordinal);

    private void DiffTypes(IEnumerable<TypeDefinition> typesV1, IEnumerable<TypeDefinition> typesV2, QueryAggregator queries) =>
        this.myDiff.ChangedTypes.AddRange(typesV1
            .Select(typeV1 => (first: typeV1, second: GetTypeByDefinition(typeV1, typesV2)))
            .Where(types => types.second is not null)
            .Select(types => TypeDiff.GenerateDiff(types.first, types.second, queries))
            .Where(diffed => TypeDiff.None != diffed));
}