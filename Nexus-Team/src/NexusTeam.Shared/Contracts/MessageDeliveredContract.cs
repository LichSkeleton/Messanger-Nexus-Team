namespace NexusTeam.Shared.Contracts
{
    using System;

    /// <summary>
    /// WebSocket message contract for message delivery notifications.
    /// Sent when a message has been delivered to recipient(s).
    /// JSON schema: { "type": "message_delivered", "messageId": "...", "chatId": "...", "deliveredAt": "..." }.
    /// </summary>
    public class MessageDeliveredContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.MessageDelivered;

        /// <summary>
        /// Gets or sets the message identifier that was delivered.
        /// </summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat identifier.
        /// </summary>
        public string ChatId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the message was delivered in ISO 8601 format.
        /// </summary>
        public DateTime DeliveredAt { get; set; }
    }
}
