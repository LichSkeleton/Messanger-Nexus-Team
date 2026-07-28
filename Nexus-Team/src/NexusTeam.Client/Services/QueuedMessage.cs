namespace NexusTeam.Client.Services
{
    using System;

    /// <summary>
    /// Represents a message queued for sending when connection is restored.
    /// </summary>
    public class QueuedMessage
    {
        /// <summary>
        /// Gets or sets the unique message ID.
        /// </summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat ID.
        /// </summary>
        public string ChatId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the message being replied to.
        /// </summary>
        public string? ReplyToId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the message was queued.
        /// </summary>
        public DateTime QueuedAt { get; set; }

        /// <summary>
        /// Gets or sets the number of send attempts.
        /// </summary>
        public int Attempts { get; set; }
    }
}
