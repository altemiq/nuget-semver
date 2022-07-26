// -----------------------------------------------------------------------
// <copyright file="DiffOperation.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altavec.Assembly.ChangeDetection.Diff;

/// <summary>
/// The diff operation.
/// </summary>
public class DiffOperation
{
    /// <summary>
    /// Initialises a new instance of the <see cref="DiffOperation"/> class.
    /// </summary>
    /// <param name="isAdded"><see langword="true"/> if this was added; otherwise <see langword="false"/>.</param>
    public DiffOperation(bool isAdded) => this.IsAdded = isAdded;

    /// <summary>
    /// Gets a value indicating whether this diff is added.
    /// </summary>
    public bool IsAdded { get; }

    /// <summary>
    /// Gets a value indicating whether this diff is removed.
    /// </summary>
    public bool IsRemoved => !this.IsAdded;
}