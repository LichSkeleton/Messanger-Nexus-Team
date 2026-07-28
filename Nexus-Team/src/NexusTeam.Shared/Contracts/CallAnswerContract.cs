namespace NexusTeam.Shared.Contracts
{
    using System;

    /// <summary>
    /// WebSocket message contract for call answers.
    /// Sent when a user accepts an incoming call.
    /// JSON schema: { "type": "call_answer", "callId": "...", "fromUserId": "...", "toUserId": "...", "timestamp": "..." }.
    /// </summary>
    public class CallAnswerContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.CallAnswer;

        /// <summary>
        /// Gets or sets the unique call identifier.
        /// </summary>
        public string CallId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the user who answered the call.
        /// </summary>
        public string FromUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the caller.
        /// </summary>
        public string ToUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the call answer timestamp in ISO 8601 format.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
