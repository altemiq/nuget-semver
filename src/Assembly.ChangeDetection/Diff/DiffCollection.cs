// -----------------------------------------------------------------------
// <copyright file="DiffCollection.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.Diff
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The difference collection.
    /// </summary>
    /// <typeparam name="T">The type in the collection.</typeparam>
    internal class DiffCollection<T> : List<DiffResult<T>>
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
        public IList<T> AddedList => this.Added.Select(type => type.ObjectV1).ToList();

        /// <summary>
        /// Gets the removed list.
        /// </summary>
        public IList<T> RemovedList => this.Removed.Select(type => type.ObjectV1).ToList();
    }
}