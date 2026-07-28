namespace NexusTeam.Shared.Contracts
{
    using System;

    /// <summary>
    /// WebSocket message contract for user left notifications.
    /// Sent when a user leaves a chat or goes offline.
    /// JSON schema: { "type": "user_left", "userId": "...", "chatId": "...", "username": "...", "timestamp": "..." }.
    /// </summary>
    public class UserLeftContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.UserLeft;

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat identifier (optional, null for global offline status).
        /// </summary>
        public string? ChatId { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp in ISO 8601 format.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
