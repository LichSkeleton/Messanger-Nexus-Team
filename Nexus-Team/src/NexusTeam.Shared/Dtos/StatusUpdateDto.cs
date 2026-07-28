namespace NexusTeam.Shared.Dtos
{
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// User status update notification.
    /// </summary>
    public class StatusUpdateDto
    {
        /// <summary>
        /// Gets or sets the user ID whose status changed.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new status.
        /// </summary>
        public UserStatus Status { get; set; }
    }
}
