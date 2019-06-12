// -----------------------------------------------------------------------
// <copyright file="DiffResult.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.Diff
{
    /// <summary>
    /// The diff result.
    /// </summary>
    /// <typeparam name="T">The type of object.</typeparam>
    internal class DiffResult<T>
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="DiffResult{T}"/> class.
        /// </summary>
        /// <param name="v1">The first version.</param>
        /// <param name="diffType">The diff type.</param>
        public DiffResult(T v1, DiffOperation diffType) => (this.ObjectV1, this.Operation) = (v1, diffType);

        /// <summary>
        /// Gets the operation.
        /// </summary>
        public DiffOperation Operation { get; }

        /// <summary>
        /// Gets the object.
        /// </summary>
        public T ObjectV1 { get; }

        /// <inheritdoc/>
        public override string ToString() => string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}, {1}", this.ObjectV1, this.Operation.IsAdded ? "added" : "removed");
    }
}