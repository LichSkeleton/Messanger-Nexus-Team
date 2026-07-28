namespace NexusTeam.Shared.Exceptions
{
    using System;

    /// <summary>
    /// Exception thrown when attempting to create a user with a duplicate username or email.
    /// </summary>
    public class DuplicateUserException : DomainException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateUserException"/> class.
        /// </summary>
        public DuplicateUserException()
            : base("User with the same username or email already exists")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateUserException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public DuplicateUserException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateUserException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public DuplicateUserException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
