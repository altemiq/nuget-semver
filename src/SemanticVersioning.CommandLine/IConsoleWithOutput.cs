// -----------------------------------------------------------------------
// <copyright file="IConsoleWithOutput.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altavec.SemanticVersioning;

/// <summary>
/// The <see cref="System.CommandLine.IConsole"/> with <see cref="OutputTypes"/>.
/// </summary>
internal interface IConsoleWithOutput : System.CommandLine.IConsole
{
    /// <summary>
    /// Gets the output.
    /// </summary>
    OutputTypes Output { get; }

    /// <summary>
    /// Gets the output stream writer.
    /// </summary>
    new IStandardStreamWriterWithOutput Out { get; }

    /// <summary>
    /// Gets the error stream writer.
    /// </summary>
    new IStandardStreamWriterWithOutput Error { get; }
}