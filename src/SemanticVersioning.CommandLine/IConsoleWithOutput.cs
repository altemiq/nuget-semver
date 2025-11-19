// -----------------------------------------------------------------------
// <copyright file="IConsoleWithOutput.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

/// <summary>
/// The console with <see cref="OutputTypes"/>.
/// </summary>
internal interface IConsoleWithOutput
{
    /// <summary>
    /// Gets the output.
    /// </summary>
    OutputTypes Output { get; }

    /// <summary>
    /// Gets the output stream writer.
    /// </summary>
    IAnsiConsoleWithOutput Out { get; }

    /// <summary>
    /// Gets the error stream writer.
    /// </summary>
    IAnsiConsoleWithOutput Error { get; }
}