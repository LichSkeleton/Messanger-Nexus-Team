namespace NexusTeam.Shared.Contracts
{
    using System;

    /// <summary>
    /// WebSocket message contract for message read notifications.
    /// Sent when a message has been read by recipient(s).
    /// JSON schema: { "type": "message_read", "messageId": "...", "chatId": "...", "userId": "...", "readAt": "..." }.
    /// </summary>
    public class MessageReadContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.MessageRead;

        /// <summary>
        /// Gets or sets the message identifier that was read.
        /// </summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat identifier.
        /// </summary>
        public string ChatId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier who read the message.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the message was read in ISO 8601 format.
        /// </summary>
        public DateTime ReadAt { get; set; }
    }
}
