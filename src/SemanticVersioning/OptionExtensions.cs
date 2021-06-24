// -----------------------------------------------------------------------
// <copyright file="OptionExtensions.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning
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