namespace NexusTeam.Shared.Contracts
{
    using System;

    /// <summary>
    /// WebSocket message contract for chat messages.
    /// Sent when a new message is created or received.
    /// JSON schema: { "type": "chat_message", "messageId": "...", "chatId": "...", "senderId": "...", "content": "...", "timestamp": "..." }.
    /// </summary>
    public class ChatMessageContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.ChatMessage;

        /// <summary>
        /// Gets or sets the unique message identifier.
        /// </summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat identifier.
        /// </summary>
        public string ChatId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sender user identifier.
        /// </summary>
        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message timestamp in ISO 8601 format.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the ID of the message being replied to (optional).
        /// </summary>
        public string? ReplyToId { get; set; }
    }
}
