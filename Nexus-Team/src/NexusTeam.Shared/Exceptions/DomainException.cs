namespace NexusTeam.Shared.Exceptions
{
    using System;

    /// <summary>
    /// Base exception for domain-related errors.
    /// Should be used for business rule violations and domain logic errors.
    /// </summary>
    public class DomainException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainException"/> class.
        /// </summary>
        public DomainException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public DomainException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public DomainException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
