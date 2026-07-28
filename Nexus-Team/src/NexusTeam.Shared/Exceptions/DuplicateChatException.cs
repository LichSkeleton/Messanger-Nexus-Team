namespace NexusTeam.Shared.Exceptions
{
    using System;

    /// <summary>
    /// Exception thrown when attempting to create a chat with a name that already exists for the user.
    /// </summary>
    public class DuplicateChatException : DomainException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateChatException"/> class.
        /// </summary>
        public DuplicateChatException()
            : base("A chat with this name already exists.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateChatException"/> class.
        /// </summary>
        /// <param name="chatName">The duplicate chat name.</param>
        public DuplicateChatException(string chatName)
            : base($"A chat with the name '{chatName}' already exists for this user.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateChatException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public DuplicateChatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
