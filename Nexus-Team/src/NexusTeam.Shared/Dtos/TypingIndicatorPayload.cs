namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// WebSocket payload for typing indicators (send and receive).
    /// </summary>
    public class TypingIndicatorPayload
    {
        /// <summary>
        /// Gets or sets the user who is typing (set by server when broadcasting).
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat where typing is occurring.
        /// </summary>
        public string ChatId { get; set; } = string.Empty;
    }
}
