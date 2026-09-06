namespace NexusTeam.Shared.Contracts
{
    using System;

    /// <summary>
    /// WebSocket message contract for call timeout notifications.
    /// Sent when a call request has not been answered in time.
    /// </summary>
    public class CallTimeoutContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.CallTimeout;

        /// <summary>
        /// Gets or sets the unique call identifier.
        /// </summary>
        public string CallId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the user who originated the call.
        /// </summary>
        public string FromUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the other party.
        /// </summary>
        public string ToUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the timeout was generated.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
