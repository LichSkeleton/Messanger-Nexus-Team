namespace NexusTeam.Shared.Enums
{
    /// <summary>
    /// Represents the delivery and read status of a message.
    /// </summary>
    public enum MessageStatus
    {
        /// <summary>
        /// Message has been sent but not yet delivered.
        /// </summary>
        Sent = 0,

        /// <summary>
        /// Message has been delivered to the recipient(s).
        /// </summary>
        Delivered = 1,

        /// <summary>
        /// Message has been read by the recipient(s).
        /// </summary>
        Read = 2,

        /// <summary>
        /// Message failed to send.
        /// </summary>
        Failed = 3,
    }
}
