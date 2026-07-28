namespace NexusTeam.Shared.Contracts
{
    using System;

    /// <summary>
    /// WebSocket message contract for call endings.
    /// Sent when a call is ended by either party.
    /// JSON schema: { "type": "call_end", "callId": "...", "fromUserId": "...", "toUserId": "...", "timestamp": "..." }.
    /// </summary>
    public class CallEndContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.CallEnd;

        /// <summary>
        /// Gets or sets the unique call identifier.
        /// </summary>
        public string CallId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the user who ended the call.
        /// </summary>
        public string FromUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the other party.
        /// </summary>
        public string ToUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the call end timestamp in ISO 8601 format.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
