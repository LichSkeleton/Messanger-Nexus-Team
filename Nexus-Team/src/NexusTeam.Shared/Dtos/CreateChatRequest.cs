namespace NexusTeam.Shared.Dtos
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// Request data transfer object for creating a new chat.
    /// Used when a user initiates a new conversation.
    /// </summary>
    public class CreateChatRequest
    {
        /// <summary>
        /// Gets or sets the chat name (for groups and channels).
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat description (for groups and channels).
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the type of chat to create.
        /// </summary>
        [JsonPropertyName("type")]
        public ChatType Type { get; set; }

        /// <summary>
        /// Gets or sets the list of participant user IDs (excluding the creator).
        /// The creator will be automatically added to the participant list.
        /// </summary>
        [JsonPropertyName("participantIds")]
        public List<string> ParticipantIds { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the avatar URL.
        /// </summary>
        [JsonPropertyName("avatarUrl")]
        public string? AvatarUrl { get; set; }
    }
}
