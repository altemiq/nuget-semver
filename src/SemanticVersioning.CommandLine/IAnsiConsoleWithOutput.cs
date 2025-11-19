// -----------------------------------------------------------------------
// <copyright file="IAnsiConsoleWithOutput.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning;

/// <summary>
/// The <see cref="Spectre.Console.IAnsiConsole"/> with <see cref="OutputTypes"/>.
/// </summary>
internal interface IAnsiConsoleWithOutput : Spectre.Console.IAnsiConsole
{
    /// <summary>
    /// Gets the output.
    /// </summary>
    OutputTypes Output { get; }
}