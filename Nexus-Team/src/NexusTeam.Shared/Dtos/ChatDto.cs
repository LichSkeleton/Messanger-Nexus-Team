namespace NexusTeam.Shared.Dtos
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// Data transfer object for chat information.
    /// Used for API responses and client-server communication.
    /// </summary>
    public class ChatDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the chat.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of chat.
        /// </summary>
        [JsonPropertyName("type")]
        public ChatType Type { get; set; }

        /// <summary>
        /// Gets or sets the chat name (for groups and channels).
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the chat description (for groups and channels).
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the avatar URL.
        /// </summary>
        [JsonPropertyName("avatarUrl")]
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Gets or sets the list of participant user IDs.
        /// </summary>
        [JsonPropertyName("participantIds")]
        public List<string> ParticipantIds { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the collection of participant user details.
        /// </summary>
        [JsonPropertyName("participants")]
        public List<UserDto> Participants { get; set; } = new List<UserDto>();

        /// <summary>
        /// Gets or sets the user ID of the creator.
        /// </summary>
        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the chat was created.
        /// Serialized as ISO 8601 string.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets when the last message was sent.
        /// Serialized as ISO 8601 string.
        /// </summary>
        [JsonPropertyName("lastMessageAt")]
        public DateTime? LastMessageAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this chat is pinned for the current user.
        /// Pin state is personal and never shared with other participants.
        /// </summary>
        [JsonPropertyName("isPinned")]
        public bool IsPinned { get; set; }
    }
}
