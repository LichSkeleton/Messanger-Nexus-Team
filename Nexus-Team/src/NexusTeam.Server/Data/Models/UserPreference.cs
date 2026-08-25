namespace NexusTeam.Server.Data.Models
{
    using System;
    using System.Collections.Generic;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    /// <summary>
    /// Represents user preferences stored in MongoDB.
    /// </summary>
    public class UserPreference
    {
        /// <summary>
        /// Gets or sets the MongoDB document ID.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether notifications are enabled.
        /// </summary>
        [BsonElement("notificationsEnabled")]
        public bool NotificationsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether sound is enabled.
        /// </summary>
        [BsonElement("soundEnabled")]
        public bool SoundEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the theme preference.
        /// </summary>
        [BsonElement("theme")]
        public string Theme { get; set; } = "light";

        /// <summary>
        /// Gets or sets the language code.
        /// </summary>
        [BsonElement("language")]
        public string Language { get; set; } = "en";

        /// <summary>
        /// Gets or sets muted chat IDs.
        /// </summary>
        [BsonElement("mutedChats")]
        public List<string> MutedChats { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the timestamp when the preference was created.
        /// </summary>
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the preference was last updated.
        /// </summary>
        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }
}
