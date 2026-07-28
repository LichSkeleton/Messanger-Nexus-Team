namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// Request to edit an existing message.
    /// </summary>
    public class EditMessageRequest
    {
        /// <summary>
        /// Gets or sets the message ID to edit.
        /// </summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new content for the message.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }
}
