// -----------------------------------------------------------------------
// <copyright file="CommandValidationException.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning
{
    /// <summary>
    /// The command validation exception.
    /// </summary>
    [System.Serializable]
    public class CommandValidationException : System.Exception
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="CommandValidationException"/> class.
        /// </summary>
        public CommandValidationException()
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="CommandValidationException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public CommandValidationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="CommandValidationException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public CommandValidationException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="CommandValidationException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected CommandValidationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}