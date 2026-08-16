namespace NexusTeam.Shared.Dtos
{
    using System.Collections.Generic;

    /// <summary>
    /// Result of a group membership change (leave, add, or remove).
    /// </summary>
    public class ChatMembershipChangeResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the chat was deleted
        /// because the last participant left.
        /// </summary>
        public bool ChatDeleted { get; set; }

        /// <summary>
        /// Gets or sets system messages describing the membership change.
        /// </summary>
        public List<MessageDto> SystemMessages { get; set; } = new List<MessageDto>();

        /// <summary>
        /// Gets or sets the updated chat DTO for remaining participants.
        /// </summary>
        public ChatDto? Chat { get; set; }

        /// <summary>
        /// Gets or sets user IDs that were added to the group.
        /// </summary>
        public List<string> AddedUserIds { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the user ID that was removed or left, if any.
        /// </summary>
        public string? RemovedUserId { get; set; }
    }
}
