namespace NexusTeam.Shared.Dtos
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Data transfer object for chat folder information.
    /// </summary>
    public class ChatFolderDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the folder.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the folder name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user ID who owns this folder.
        /// </summary>
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of chat IDs in this folder.
        /// </summary>
        [JsonPropertyName("chatIds")]
        public List<string> ChatIds { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the timestamp when the folder was created.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the folder was last updated.
        /// </summary>
        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }
}
