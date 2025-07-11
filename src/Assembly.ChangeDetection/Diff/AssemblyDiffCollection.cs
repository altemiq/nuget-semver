// -----------------------------------------------------------------------
// <copyright file="AssemblyDiffCollection.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.Diff;

using System.Diagnostics;
using Mono.Cecil;

/// <summary>
/// The assembly diff collection.
/// </summary>
[DebuggerDisplay("Add {AddedRemovedTypes.AddedCount} Remove {AddedRemovedTypes.RemovedCount} Changed {ChangedTypes.Count}")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "This is intentional")]
public class AssemblyDiffCollection
{
    /// <summary>
    /// Gets the added or removed types.
    /// </summary>
    public DiffCollection<TypeDefinition> AddedRemovedTypes { get; } = new DiffCollection<TypeDefinition>();

    /// <summary>
    /// Gets the changed types.
    /// </summary>
    public IList<TypeDiff> ChangedTypes { get; } = new List<TypeDiff>();
}