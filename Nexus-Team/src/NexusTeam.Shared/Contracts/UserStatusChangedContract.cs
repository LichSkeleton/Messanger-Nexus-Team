namespace NexusTeam.Shared.Contracts
{
    using System;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// WebSocket message contract for user status change notifications.
    /// Sent when a user changes their online/away/DND status.
    /// JSON schema: { "type": "user_status_changed", "userId": "...", "username": "...", "status": "...", "timestamp": "..." }.
    /// </summary>
    public class UserStatusChangedContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.UserStatusChanged;

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new status.
        /// </summary>
        public UserStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the status change in ISO 8601 format.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
