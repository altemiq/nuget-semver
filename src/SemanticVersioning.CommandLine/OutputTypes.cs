// -----------------------------------------------------------------------
// <copyright file="OutputTypes.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning;

/// <summary>
/// The output type.
/// </summary>
[Flags]
public enum OutputTypes
{
    /// <summary>
    /// No flags.
    /// </summary>
    None = 0,

    /// <summary>
    /// Output breaking changes.
    /// </summary>
    BreakingChanges = 1 << 0,

    /// <summary>
    /// Output functional changes.
    /// </summary>
    FunctionalChanges = 1 << 1,

    /// <summary>
    /// Output TeamCity version.
    /// </summary>
    TeamCity = 1 << 2,

    /// <summary>
    /// Output Json version.
    /// </summary>
    Json = 1 << 3,

    /// <summary>
    /// Diagnostic output.
    /// </summary>
    Diagnostic = 1 << 4,

    /// <summary>
    /// All outputs.
    /// </summary>
    All = BreakingChanges | FunctionalChanges | TeamCity | Json | Diagnostic,
}