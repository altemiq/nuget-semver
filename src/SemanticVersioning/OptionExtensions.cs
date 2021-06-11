// -----------------------------------------------------------------------
// <copyright file="OptionExtensions.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning
{
    /// <summary>
    /// The <see cref="System.CommandLine.Option{T}"/> extensions.
    /// </summary>
    internal static class OptionExtensions
    {
        /// <summary>
        /// Adds the alias.
        /// </summary>
        /// <typeparam name="T">The type of argument.</typeparam>
        /// <param name="option">The option.</param>
        /// <param name="alias">The alias.</param>
        /// <returns>The option to chain.</returns>
        public static T WithAlias<T>(this T option, string alias)
            where T : System.CommandLine.Option
        {
            option.AddAlias(alias);
            return option;
        }
    }
}
