namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// WebSocket payload broadcast when a chat is deleted.
    /// </summary>
    public class ChatDeletedPayload
    {
        /// <summary>
        /// Gets or sets the deleted chat ID.
        /// </summary>
        public string ChatId { get; set; } = string.Empty;
    }
}
