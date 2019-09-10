// -----------------------------------------------------------------------
// <copyright file="TypeQueryMode.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.Query
{
    /// <summary>
    /// The type query mode.
    /// </summary>
    [System.Flags]
    internal enum TypeQueryMode
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// Public.
        /// </summary>
        Public = 1,

        /// <summary>
        /// Internal.
        /// </summary>
        Internal = 2,

        /// <summary>
        /// Not compiler generated.
        /// </summary>
        NotCompilerGenerated = 4,

        /// <summary>
        /// Interface.
        /// </summary>
        Interface = 8,

        /// <summary>
        /// Class.
        /// </summary>
        Class = 16,

        /// <summary>
        /// Value type.
        /// </summary>
        ValueType = 32,

        /// <summary>
        /// Enum.
        /// </summary>
        Enum = 64,

        /// <summary>
        /// API relevant.
        /// </summary>
        ApiRelevant = Public | NotCompilerGenerated | Interface | Class | ValueType | Enum,

        /// <summary>
        /// All.
        /// </summary>
        All = Public | Internal | Interface | Class | ValueType | Enum,
    }
}