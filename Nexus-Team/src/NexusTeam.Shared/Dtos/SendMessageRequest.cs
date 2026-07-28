namespace NexusTeam.Shared.Dtos
{
    using System.Collections.Generic;

    /// <summary>
    /// Request DTO for sending a message.
    /// Sent from client to server via API or WebSocket.
    /// </summary>
    public class SendMessageRequest
    {
        /// <summary>
        /// Gets or sets the chat ID where the message should be sent.
        /// </summary>
        public string ChatId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the message this is replying to (optional).
        /// </summary>
        public string? ReplyToId { get; set; }

        /// <summary>
        /// Gets or sets the list of attachment IDs to include with this message.
        /// Attachments must be uploaded separately before sending the message.
        /// </summary>
        public List<string> AttachmentIds { get; set; } = new List<string>();
    }
}
