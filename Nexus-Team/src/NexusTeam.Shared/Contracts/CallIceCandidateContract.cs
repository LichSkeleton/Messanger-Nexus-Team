namespace NexusTeam.Shared.Contracts
{
    using System;

    /// <summary>
    /// WebSocket message contract for WebRTC ICE candidates.
    /// Sent during WebRTC connection establishment to exchange network connectivity information.
    /// JSON schema: { "type": "call_ice_candidate", "callId": "...", "fromUserId": "...", "toUserId": "...", "candidate": "...", "sdpMid": "...", "sdpMLineIndex": 0, "timestamp": "..." }.
    /// </summary>
    public class CallIceCandidateContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.CallIceCandidate;

        /// <summary>
        /// Gets or sets the unique call identifier.
        /// </summary>
        public string CallId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the user sending the ICE candidate.
        /// </summary>
        public string FromUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the recipient.
        /// </summary>
        public string ToUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ICE candidate as a string.
        /// </summary>
        public string Candidate { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SDP media stream identification tag.
        /// </summary>
        public string SdpMid { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SDP media line index.
        /// </summary>
        public int SdpMLineIndex { get; set; }

        /// <summary>
        /// Gets or sets the ICE candidate timestamp in ISO 8601 format.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
