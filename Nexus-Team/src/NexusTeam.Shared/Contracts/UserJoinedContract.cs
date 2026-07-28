namespace NexusTeam.Shared.Contracts
{
    using System;

    /// <summary>
    /// WebSocket message contract for user joined notifications.
    /// Sent when a user joins a chat or comes online.
    /// JSON schema: { "type": "user_joined", "userId": "...", "chatId": "...", "username": "...", "timestamp": "..." }.
    /// </summary>
    public class UserJoinedContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.UserJoined;

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat identifier (optional, null for global online status).
        /// </summary>
        public string? ChatId { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp in ISO 8601 format.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
