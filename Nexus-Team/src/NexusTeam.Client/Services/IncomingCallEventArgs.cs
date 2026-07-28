namespace NexusTeam.Client.Services
{
    using System;

    /// <summary>
    /// Event arguments for incoming call events.
    /// </summary>
    public class IncomingCallEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the unique call identifier.
        /// </summary>
        public string CallId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the caller.
        /// </summary>
        public string FromUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the username of the caller.
        /// </summary>
        public string FromUserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional chat identifier if call is from a chat.
        /// </summary>
        public string? ChatId { get; set; }
    }
}
