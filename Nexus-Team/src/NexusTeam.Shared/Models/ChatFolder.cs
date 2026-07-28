namespace NexusTeam.Shared.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a folder for organizing chats.
    /// </summary>
    public class ChatFolder
    {
        /// <summary>
        /// Gets or sets the unique identifier for the folder.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the folder name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user ID who owns this folder.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of chat IDs in this folder.
        /// </summary>
        public List<string> ChatIds { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the timestamp when the folder was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the folder was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
