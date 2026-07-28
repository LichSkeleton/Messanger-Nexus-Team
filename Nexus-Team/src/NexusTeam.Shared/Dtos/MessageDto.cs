namespace NexusTeam.Shared.Dtos
{
    using System;
    using System.Collections.Generic;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// Data transfer object for message information.
    /// Used for API responses and client-server communication.
    /// </summary>
    public class MessageDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the message.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat ID this message belongs to.
        /// </summary>
        public string ChatId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sender user ID.
        /// </summary>
        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message status.
        /// </summary>
        public MessageStatus Status { get; set; }

        /// <summary>
        /// Gets or sets when the message was created.
        /// Serialized as ISO 8601 string.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the message was edited.
        /// Serialized as ISO 8601 string.
        /// </summary>
        public DateTime? EditedAt { get; set; }

        /// <summary>
        /// Gets or sets the ID of the message this is replying to.
        /// </summary>
        public string? ReplyToId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message is deleted.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the list of attachments for this message.
        /// </summary>
        public List<MessageAttachmentDto> Attachments { get; set; } = new List<MessageAttachmentDto>();

        /// <summary>
        /// Gets or sets the reactions for this message.
        /// Key is emoji, value is list of user IDs who reacted.
        /// </summary>
        public Dictionary<string, List<string>> Reactions { get; set; } = new Dictionary<string, List<string>>();
    }
}
