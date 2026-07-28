namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// User avatar update notification.
    /// </summary>
    public class AvatarUpdateDto
    {
        /// <summary>
        /// Gets or sets the user ID whose avatar changed.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new avatar URL.
        /// </summary>
        public string? AvatarUrl { get; set; }
    }
}
