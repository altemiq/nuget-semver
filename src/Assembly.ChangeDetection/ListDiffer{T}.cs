// -----------------------------------------------------------------------
// <copyright file="ListDiffer{T}.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection;

/// <summary>
/// Compares two lists and creates two diff lists with added and removed elements.
/// </summary>
/// <typeparam name="T">The type.</typeparam>
internal class ListDiffer<T>
{
    private readonly System.Func<T, T, bool> comparer;

    /// <summary>
    /// Initialises a new instance of the <see cref="ListDiffer{T}" /> class.
    /// </summary>
    /// <param name="comparer">The comparer function to check for equality in the collections to be compared.</param>
    public ListDiffer(System.Func<T, T, bool> comparer) => this.comparer = comparer;

    /// <summary>
    /// Compare two lists A and B and add the new elements in B to added and the elements elements which occur only in A and not in B to the removed collection.
    /// </summary>
    /// <param name="firstList">The first list.</param>
    /// <param name="secondList">The second list.</param>
    /// <param name="added">New added elements in <paramref name="secondList"/>.</param>
    /// <param name="removed">Removed elements in <paramref name="secondList"/>.</param>
    public void Diff(System.Collections.IEnumerable firstList, System.Collections.IEnumerable secondList, System.Action<T> added, System.Action<T> removed)
    {
        foreach (T first in firstList)
        {
            var found = false;
            foreach (T second in secondList)
            {
                if (this.comparer(first, second))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                removed(first);
            }
        }

        foreach (T second in secondList)
        {
            var found = false;
            foreach (T first in firstList)
            {
                if (this.comparer(second, first))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                added(second);
            }
        }
    }
}