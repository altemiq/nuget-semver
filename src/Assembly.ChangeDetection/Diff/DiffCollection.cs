// -----------------------------------------------------------------------
// <copyright file="DiffCollection.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altavec.Assembly.ChangeDetection.Diff;

/// <summary>
/// The difference collection.
/// </summary>
/// <typeparam name="T">The type in the collection.</typeparam>
public class DiffCollection<T> : List<DiffResult<T>>
{
    /// <summary>
    /// Gets the added count.
    /// </summary>
    public int AddedCount => this.Count(obj => obj.Operation.IsAdded);

    /// <summary>
    /// Gets the removed count.
    /// </summary>
    public int RemovedCount => this.Count(obj => obj.Operation.IsRemoved);

    /// <summary>
    /// Gets the added items.
    /// </summary>
    public IEnumerable<DiffResult<T>> Added => this.Where(obj => obj.Operation.IsAdded);

    /// <summary>
    /// Gets the removed items.
    /// </summary>
    public IEnumerable<DiffResult<T>> Removed => this.Where(obj => obj.Operation.IsRemoved);

    /// <summary>
    /// Gets the added list.
    /// </summary>
    /// <returns>The added list.</returns>
    public IReadOnlyList<T> GetAddedList() => this.Added.Select(type => type.ObjectV1).ToList();

    /// <summary>
    /// Gets the removed list.
    /// </summary>
    /// <returns>The removed list.</returns>
    public IReadOnlyList<T> GetRemovedList() => this.Removed.Select(type => type.ObjectV1).ToList();
}