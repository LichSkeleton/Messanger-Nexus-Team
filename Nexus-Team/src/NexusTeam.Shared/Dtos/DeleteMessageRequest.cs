namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// Request to delete a message.
    /// </summary>
    public class DeleteMessageRequest
    {
        /// <summary>
        /// Gets or sets the message ID to delete.
        /// </summary>
        public string MessageId { get; set; } = string.Empty;
    }
}
