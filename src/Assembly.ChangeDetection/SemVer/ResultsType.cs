// -----------------------------------------------------------------------
// <copyright file="ResultsType.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altavec.Assembly.ChangeDetection.SemVer;

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