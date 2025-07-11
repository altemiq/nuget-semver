﻿// -----------------------------------------------------------------------
// <copyright file="SemanticVersionIncrement.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

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