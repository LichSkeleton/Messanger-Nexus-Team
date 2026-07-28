namespace NexusTeam.Server.Data.Models
{
    using System;
    using System.Collections.Generic;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    /// <summary>
    /// MongoDB model for ChatFolder.
    /// </summary>
    public class ChatFolder
    {
        /// <summary>
        /// Gets or sets the unique identifier for the folder.
        /// </summary>
        [BsonId]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the folder name.
        /// </summary>
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user ID who owns this folder.
        /// </summary>
        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of chat IDs in this folder.
        /// </summary>
        [BsonElement("chatIds")]
        public List<string> ChatIds { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the timestamp when the folder was created.
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the folder was last updated.
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }
}
