namespace NexusTeam.Server.Data.Models
{
    using System;
    using System.Collections.Generic;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    /// <summary>
    /// MongoDB model for Message.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Gets or sets the unique identifier for the message.
        /// </summary>
        [BsonId]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the chat ID this message belongs to.
        /// </summary>
        [BsonElement("chatId")]
        public string ChatId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sender user ID.
        /// </summary>
        [BsonElement("senderId")]
        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content of the message.
        /// </summary>
        [BsonElement("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message status (0=Sent, 1=Delivered, 2=Read, 3=Failed).
        /// </summary>
        [BsonElement("status")]
        public int Status { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the message was created.
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the message was edited (null if never edited).
        /// </summary>
        [BsonElement("editedAt")]
        public DateTime? EditedAt { get; set; }

        /// <summary>
        /// Gets or sets the ID of the message this is replying to (null if not a reply).
        /// </summary>
        [BsonElement("replyToId")]
        public string? ReplyToId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message is deleted.
        /// </summary>
        [BsonElement("isDeleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the reactions for this message.
        /// Key is emoji, value is list of user IDs who reacted.
        /// </summary>
        [BsonElement("reactions")]
        public Dictionary<string, List<string>>? Reactions { get; set; }
    }
}
