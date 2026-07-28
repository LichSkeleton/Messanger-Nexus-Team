namespace NexusTeam.Shared.Dtos
{
    using System;
    using System.Text.Json.Serialization;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// Data transfer object for user information.
    /// Used for API responses and client-server communication.
    /// Excludes sensitive fields like password hash.
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the avatar URL.
        /// </summary>
        [JsonPropertyName("avatarUrl")]
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Gets or sets the current status.
        /// </summary>
        [JsonPropertyName("status")]
        public UserStatus Status { get; set; }

        /// <summary>
        /// Gets or sets when the user was last seen online.
        /// Serialized as ISO 8601 string.
        /// </summary>
        [JsonPropertyName("lastSeenAt")]
        public DateTime? LastSeenAt { get; set; }
    }
}
