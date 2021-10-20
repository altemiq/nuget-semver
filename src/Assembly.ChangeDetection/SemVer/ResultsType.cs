// -----------------------------------------------------------------------
// <copyright file="ResultsType.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.SemVer;

/// <summary>
/// The results type.
/// </summary>
public enum ResultsType
{
    /// <summary>
    /// Patch.
    /// </summary>
    Patch,

    /// <summary>
    /// Minor.
    /// </summary>
    Minor,

    /// <summary>
    /// Major.
    /// </summary>
    Major,
}