// -----------------------------------------------------------------------
// <copyright file="IStandardStreamWriterWithOutput.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altavec.SemanticVersioning;

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