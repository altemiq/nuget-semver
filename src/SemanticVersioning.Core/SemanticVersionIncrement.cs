// -----------------------------------------------------------------------
// <copyright file="SemanticVersionIncrement.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altavec.SemanticVersioning;

/// <summary>
/// The <see cref="NuGet.Versioning.SemanticVersion"/> increment location.
/// </summary>
public enum SemanticVersionIncrement
{
    /// <summary>
    /// Increments the patch version.
    /// </summary>
    Patch,

    /// <summary>
    /// Increments the release label.
    /// </summary>
    ReleaseLabel,
}