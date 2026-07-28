namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// Payload broadcast after a message is soft-deleted.
    /// </summary>
    public class DeleteMessageNotification
    {
        /// <summary>
        /// Gets or sets the deleted message ID.
        /// </summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat the message belonged to.
        /// </summary>
        public string ChatId { get; set; } = string.Empty;
    }
}
