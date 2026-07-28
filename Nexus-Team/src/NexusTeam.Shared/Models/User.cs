namespace NexusTeam.Shared.Models
{
    using System;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// Represents a user in the chat system.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the username for login.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the hashed password.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name shown to other users.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL to the user's avatar image.
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Gets or sets the current status of the user.
        /// </summary>
        public UserStatus Status { get; set; } = UserStatus.Offline;

        /// <summary>
        /// Gets or sets the timestamp when the user was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the user was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the user was last seen online.
        /// </summary>
        public DateTime? LastSeenAt { get; set; }
    }
}
