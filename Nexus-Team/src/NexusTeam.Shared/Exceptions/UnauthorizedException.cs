namespace NexusTeam.Shared.Exceptions
{
    using System;

    /// <summary>
    /// Exception thrown when a user is not authorized to access a resource.
    /// Typically used for HTTP 401 Unauthorized responses.
    /// </summary>
    public class UnauthorizedException : DomainException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
        /// </summary>
        public UnauthorizedException()
            : base("Unauthorized access")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public UnauthorizedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public UnauthorizedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
