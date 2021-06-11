// -----------------------------------------------------------------------
// <copyright file="OptionExtensions.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning
{
    using System.CommandLine;

    /// <summary>
    /// The <see cref="Option{T}"/> extensions.
    /// </summary>
    internal static class OptionExtensions
    {
        /// <summary>
        /// Sets the argument name.
        /// </summary>
        /// <typeparam name="T">The type of argument.</typeparam>
        /// <param name="option">The option.</param>
        /// <param name="name">The argument name.</param>
        /// <returns>The option to chain.</returns>
        public static Option<T> WithArgumentName<T>(this Option<T> option, string name)
        {
            option.Argument.Name = name;
            return option;
        }

        /// <summary>
        /// Sets the default value.
        /// </summary>
        /// <typeparam name="T">The type of argument.</typeparam>
        /// <param name="option">The option.</param>
        /// <param name="value">The default value.</param>
        /// <returns>The option to chain.</returns>
        public static Option<T> WithDefaultValue<T>(this Option<T> option, T value)
        {
            option.Argument.SetDefaultValue(value);
            return option;
        }

        /// <summary>
        /// Sets the arity.
        /// </summary>
        /// <typeparam name="T">The type of argument.</typeparam>
        /// <param name="option">The option.</param>
        /// <param name="arity">The arity.</param>
        /// <returns>The option to chain.</returns>
        public static Option<T> WithArity<T>(this Option<T> option, IArgumentArity arity)
        {
            option.Argument.Arity = arity;
            return option;
        }
    }
}
