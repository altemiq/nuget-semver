// -----------------------------------------------------------------------
// <copyright file="IStandardStreamWriterWithOutput.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning
{
    /// <summary>
    /// The <see cref="System.CommandLine.IO.IStandardStreamWriter"/> with <see cref="OutputTypes"/>.
    /// </summary>
    internal interface IStandardStreamWriterWithOutput : System.CommandLine.IO.IStandardStreamWriter
    {
        /// <summary>
        /// Gets the output.
        /// </summary>
        OutputTypes Output { get; }

        /// <summary>
        /// Writes the value for the specified output, if allowed.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="output">The output.</param>
        public void Write(string value, OutputTypes output);

        /// <summary>
        /// Writes the value and new line for the specified output, if allowed.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="output">The output.</param>
        public void WriteLine(string value, OutputTypes output);
    }
}