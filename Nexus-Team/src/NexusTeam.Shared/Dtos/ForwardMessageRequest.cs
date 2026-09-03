namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// Request DTO for forwarding an existing message into another chat.
    /// The server copies content and attachments so the original can later be deleted.
    /// </summary>
    public class ForwardMessageRequest
    {
        /// <summary>
        /// Gets or sets the ID of the message to forward.
        /// </summary>
        public string MessageId { get; set; } = string.Empty;
    }
}
