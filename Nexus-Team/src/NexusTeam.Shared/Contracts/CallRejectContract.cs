namespace NexusTeam.Shared.Contracts
{
    using System;

    /// <summary>
    /// WebSocket message contract for call rejections.
    /// Sent when a user rejects an incoming call.
    /// JSON schema: { "type": "call_reject", "callId": "...", "fromUserId": "...", "toUserId": "...", "reason": "...", "timestamp": "..." }.
    /// </summary>
    public class CallRejectContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.CallReject;

        /// <summary>
        /// Gets or sets the unique call identifier.
        /// </summary>
        public string CallId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the user who rejected the call.
        /// </summary>
        public string FromUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the caller.
        /// </summary>
        public string ToUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional reason for rejecting the call.
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Gets or sets the call rejection timestamp in ISO 8601 format.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
