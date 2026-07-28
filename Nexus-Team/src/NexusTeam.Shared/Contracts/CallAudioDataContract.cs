namespace NexusTeam.Shared.Contracts
{
    using System;

    /// <summary>
    /// WebSocket message contract for call audio data.
    /// Sent when transmitting raw audio data during an active call.
    /// JSON schema: { "type": "call_audio_data", "callId": "...", "fromUserId": "...", "toUserId": "...", "audioData": "...", "timestamp": "..." }.
    /// </summary>
    public class CallAudioDataContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.CallAudioData;

        /// <summary>
        /// Gets or sets the unique call identifier.
        /// </summary>
        public string CallId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the sender.
        /// </summary>
        public string FromUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the receiver.
        /// </summary>
        public string ToUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the audio data as base64-encoded string.
        /// </summary>
        public string AudioData { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the audio data timestamp in ISO 8601 format.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
