// -----------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

/// <summary>
/// <see cref="IAnsiConsoleWithOutput"/> extension methods.
/// </summary>
internal static class ExtensionMethods
{
    /// <summary>
    /// Writes the specified string to the stream if <see cref="IAnsiConsoleWithOutput.Output"/> contains <paramref name="output"/>.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The value.</param>
    /// <param name="output">The output.</param>
    public static void Write(this IAnsiConsoleWithOutput writer, string? value, OutputTypes output)
    {
        if (writer.Output.HasFlag(output) && value is not null)
        {
            Spectre.Console.AnsiConsoleExtensions.Write(writer, value);
        }
    }

    /// <summary>
    /// Writes the specified string to the stream, followed by the current environment's line terminator, if <see cref="IAnsiConsoleWithOutput.Output"/> contains <paramref name="output"/>.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The value.</param>
    /// <param name="output">The output.</param>
    public static void WriteLine(this IAnsiConsoleWithOutput writer, string? value, OutputTypes output)
    {
        if (writer.Output.HasFlag(output))
        {
            writer.WriteLine(value);
        }
    }

    /// <summary>
    /// Writes the specified string to the stream, followed by the current environment's line terminator.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The value.</param>
    public static void WriteLine(this IAnsiConsoleWithOutput writer, string? value)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (value is null)
        {
            Spectre.Console.AnsiConsoleExtensions.WriteLine(writer);
        }
        else
        {
            Spectre.Console.AnsiConsoleExtensions.WriteLine(writer, value);
        }
    }
}