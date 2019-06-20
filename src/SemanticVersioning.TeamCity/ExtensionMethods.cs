// -----------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning.TeamCity
{
    using System.CommandLine;

    /// <summary>
    /// Extension methods.
    /// </summary>
    internal static class ExtensionMethods
    {
        /// <summary>
        /// Adds the argument fluently.
        /// </summary>
        /// <typeparam name="T">The command type.</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="argument">The argument.</param>
        /// <returns>The input command.</returns>
        public static T AddFluentArgument<T>(this T command, Argument argument)
            where T : Command
        {
            command.AddArgument(argument);
            return command;
        }

        /// <summary>
        /// Adds the option fluently.
        /// </summary>
        /// <typeparam name="T">The command type.</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="option">The option.</param>
        /// <returns>The input command.</returns>
        public static T AddFluentOption<T>(this T command, Option option)
            where T : Command
        {
            command.AddOption(option);
            return command;
        }

        /// <summary>
        /// Adds the command fluently.
        /// </summary>
        /// <typeparam name="T">The command type.</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="commandtoAdd">The command to add.</param>
        /// <returns>The input command.</returns>
        public static T AddFluentCommand<T>(this T command, Command commandtoAdd)
            where T : Command
        {
            command.AddCommand(commandtoAdd);
            return command;
        }
    }
}
