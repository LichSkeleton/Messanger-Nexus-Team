namespace NexusTeam.Shared.Models
{
    using System;
    using System.Collections.Generic;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// Represents a chat conversation (direct message, group, or channel).
    /// </summary>
    public class Chat
    {
        /// <summary>
        /// Gets or sets the unique identifier for the chat.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of chat.
        /// </summary>
        public ChatType Type { get; set; }

        /// <summary>
        /// Gets or sets the chat name (for groups and channels).
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the chat description (for groups and channels).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the URL to the chat's avatar or icon.
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Gets or sets the list of participant user IDs.
        /// </summary>
        public List<string> ParticipantIds { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the user ID of the chat creator/owner.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the chat was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the chat was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last message in the chat.
        /// </summary>
        public DateTime? LastMessageAt { get; set; }
    }
}
