// -----------------------------------------------------------------------
// <copyright file="TypeQueryExtensions.cs" company="GeomaticTechnologies">
// Copyright (c) GeomaticTechnologies. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.Assembly.ChangeDetection.Query
{
    using System.Collections.Generic;
    using System.Linq;
    using Mono.Cecil;

    /// <summary>
    /// The type query extensions.
    /// </summary>
    internal static class TypeQueryExtensions
    {
        /// <summary>
        /// Gets the matching types.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The types.</returns>
        public static IEnumerable<TypeDefinition> GetMatchingTypes(this IEnumerable<TypeQuery> list, AssemblyDefinition assembly) => list.SelectMany(query => query.GetTypes(assembly));
    }
}