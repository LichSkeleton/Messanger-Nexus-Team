namespace NexusTeam.Server.Data.Models
{
    using System;
    using System.Collections.Generic;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    /// <summary>
    /// MongoDB model for Chat.
    /// </summary>
    public class Chat
    {
        /// <summary>
        /// Gets or sets the unique identifier for the chat.
        /// </summary>
        [BsonId]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the chat.
        /// </summary>
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of chat (private, group, channel).
        /// </summary>
        [BsonElement("type")]
        public string Type { get; set; } = "private";

        /// <summary>
        /// Gets or sets the description of the chat (for groups and channels).
        /// </summary>
        [BsonElement("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the avatar URL for the chat.
        /// </summary>
        [BsonElement("avatarUrl")]
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Gets or sets the list of participant user IDs.
        /// </summary>
        [BsonElement("participants")]
        public List<string> Participants { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the user ID of the chat creator.
        /// </summary>
        [BsonElement("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the chat was created.
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the chat was last updated.
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last message in the chat.
        /// </summary>
        [BsonElement("lastMessageAt")]
        public DateTime? LastMessageAt { get; set; }
    }
}
