namespace NexusTeam.Shared.Contracts
{
    using System;

    /// <summary>
    /// WebSocket message contract for WebRTC SDP answers.
    /// Sent in response to an SDP offer, contains the SDP answer.
    /// JSON schema: { "type": "call_sdp_answer", "callId": "...", "fromUserId": "...", "toUserId": "...", "sdp": "...", "timestamp": "..." }.
    /// </summary>
    public class CallSdpAnswerContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.CallSdpAnswer;

        /// <summary>
        /// Gets or sets the unique call identifier.
        /// </summary>
        public string CallId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the user sending the SDP answer.
        /// </summary>
        public string FromUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the recipient.
        /// </summary>
        public string ToUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SDP answer as a string.
        /// </summary>
        public string Sdp { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SDP answer timestamp in ISO 8601 format.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
