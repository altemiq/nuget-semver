// -----------------------------------------------------------------------
// <copyright file="ListDiffer{T}.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection;

/// <summary>
/// Compares two lists and creates two diff lists with added and removed elements.
/// </summary>
/// <typeparam name="T">The type.</typeparam>
internal class ListDiffer<T>
{
    private readonly Func<T, T, bool> comparer;

    /// <summary>
    /// Initialises a new instance of the <see cref="ListDiffer{T}" /> class.
    /// </summary>
    /// <param name="comparer">The comparer function to check for equality in the collections to be compared.</param>
    public ListDiffer(Func<T, T, bool> comparer) => this.comparer = comparer;

    /// <summary>
    /// Compare two lists A and B and add the new elements in B to added and the elements which occur only in A and not in B to the removed collection.
    /// </summary>
    /// <param name="firstList">The first list.</param>
    /// <param name="secondList">The second list.</param>
    /// <param name="added">New added elements in <paramref name="secondList"/>.</param>
    /// <param name="removed">Removed elements in <paramref name="secondList"/>.</param>
    public void Diff(System.Collections.IEnumerable firstList, System.Collections.IEnumerable secondList, Action<T> added, Action<T> removed)
    {
        foreach (T first in firstList)
        {
            if (!secondList.OfType<T>().Any(second => this.comparer(first, second)))
            {
                removed(first);
            }
        }

        foreach (T second in secondList)
        {
            if (!firstList.OfType<T>().Any(first => this.comparer(second, first)))
            {
                added(second);
            }
        }
    }
}