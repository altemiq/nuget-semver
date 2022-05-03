// -----------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning;

/// <summary>
/// <see cref="IStandardStreamWriterWithOutput"/> extension methods.
/// </summary>
internal static class ExtensionMethods
{
    /// <summary>
    /// Writes the specified string to the stream if <see cref="IStandardStreamWriterWithOutput.Output"/> contains <paramref name="output"/>.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The value.</param>
    /// <param name="output">The output.</param>
    public static void Write(this IStandardStreamWriterWithOutput writer, string? value, OutputTypes output)
    {
        if (writer.Output.HasFlag(output))
        {
            writer.Write(value);
        }
    }

    /// <summary>
    /// Writes the specified string to the stream, followed by the current environment's line terminator, if <see cref="IStandardStreamWriterWithOutput.Output"/> contains <paramref name="output"/>.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The value.</param>
    /// <param name="output">The output.</param>
    public static void WriteLine(this IStandardStreamWriterWithOutput writer, string? value, OutputTypes output)
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
    public static void WriteLine(this IStandardStreamWriterWithOutput writer, string? value)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (value is null)
        {
            System.CommandLine.IO.StandardStreamWriter.WriteLine(writer);
        }
        else
        {
            System.CommandLine.IO.StandardStreamWriter.WriteLine(writer, value);
        }
    }
}