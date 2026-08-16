namespace NexusTeam.Shared.Dtos
{
    using System.Collections.Generic;

    /// <summary>
    /// User preferences data transfer object.
    /// </summary>
    public class UserPreferenceDto
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether notifications are enabled.
        /// </summary>
        public bool NotificationsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether sound is enabled.
        /// </summary>
        public bool SoundEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the theme preference.
        /// </summary>
        public string Theme { get; set; } = "light";

        /// <summary>
        /// Gets or sets the language code.
        /// </summary>
        public string Language { get; set; } = "en";

        /// <summary>
        /// Gets or sets muted chat IDs.
        /// </summary>
        public List<string> MutedChats { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets pinned chat IDs, in display order (first item is shown at the top).
        /// </summary>
        public List<string> PinnedChats { get; set; } = new List<string>();
    }
}
