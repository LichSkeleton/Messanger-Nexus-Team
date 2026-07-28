namespace NexusTeam.Shared.Contracts
{
    using System;

    /// <summary>
    /// WebSocket message contract for WebRTC SDP offers.
    /// Sent when initiating a WebRTC connection, contains the SDP offer.
    /// JSON schema: { "type": "call_sdp_offer", "callId": "...", "fromUserId": "...", "toUserId": "...", "sdp": "...", "timestamp": "..." }.
    /// </summary>
    public class CallSdpOfferContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.CallSdpOffer;

        /// <summary>
        /// Gets or sets the unique call identifier.
        /// </summary>
        public string CallId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the user sending the SDP offer.
        /// </summary>
        public string FromUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the recipient.
        /// </summary>
        public string ToUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SDP offer as a string.
        /// </summary>
        public string Sdp { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SDP offer timestamp in ISO 8601 format.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
