// -----------------------------------------------------------------------
// <copyright file="IStandardStreamWriterWithOutput.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Mondo.SemanticVersioning;

/// <summary>
/// The <see cref="System.CommandLine.IO.IStandardStreamWriter"/> with <see cref="OutputTypes"/>.
/// </summary>
internal interface IStandardStreamWriterWithOutput : System.CommandLine.IO.IStandardStreamWriter
{
    /// <summary>
    /// Gets the output.
    /// </summary>
    OutputTypes Output { get; }
}