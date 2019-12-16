// -----------------------------------------------------------------------
// <copyright file="OutputTypes.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
{
    /// <summary>
    /// The output type.
    /// </summary>
    [System.Flags]
    public enum OutputTypes
    {
        /// <summary>
        /// No flags.
        /// </summary>
        None = 0,

        /// <summary>
        /// Output breaking changes.
        /// </summary>
        BreakingChanges = 1,

        /// <summary>
        /// Output functional changes.
        /// </summary>
        FunctionalChanges = 2,

        /// <summary>
        /// Output TeamCity version.
        /// </summary>
        TeamCity = 4,
    }
}
