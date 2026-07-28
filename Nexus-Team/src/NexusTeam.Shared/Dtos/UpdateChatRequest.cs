namespace NexusTeam.Shared.Dtos
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request DTO for updating group chat properties (owner only).
    /// </summary>
    public class UpdateChatRequest
    {
        /// <summary>
        /// Gets or sets the new chat name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the new chat description.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the new avatar URL.
        /// </summary>
        [JsonPropertyName("avatarUrl")]
        public string? AvatarUrl { get; set; }
    }
}
