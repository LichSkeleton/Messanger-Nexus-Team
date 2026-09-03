namespace NexusTeam.Shared.Contracts
{
    /// <summary>
    /// Constants for WebSocket message type identifiers.
    /// Used for message discrimination and routing.
    /// </summary>
    public static class MessageTypes
    {
        /// <summary>
        /// Message type for chat messages.
        /// </summary>
        public const string ChatMessage = "chat_message";

        /// <summary>
        /// Message type for user joined notifications.
        /// </summary>
        public const string UserJoined = "user_joined";

        /// <summary>
        /// Message type for user left notifications.
        /// </summary>
        public const string UserLeft = "user_left";

        /// <summary>
        /// Message type for typing indicators.
        /// </summary>
        public const string TypingIndicator = "typing_indicator";

        /// <summary>
        /// Message type for message delivered notifications.
        /// </summary>
        public const string MessageDelivered = "message_delivered";

        /// <summary>
        /// Message type for message read notifications.
        /// </summary>
        public const string MessageRead = "message_read";

        /// <summary>
        /// Message type for user status changes.
        /// </summary>
        public const string UserStatusChanged = "user_status_changed";

        /// <summary>
        /// Message type for errors.
        /// </summary>
        public const string Error = "error";

        /// <summary>
        /// Message type for call requests.
        /// </summary>
        public const string CallRequest = "call_request";

        /// <summary>
        /// Message type for call answers.
        /// </summary>
        public const string CallAnswer = "call_answer";

        /// <summary>
        /// Message type for call rejections.
        /// </summary>
        public const string CallReject = "call_reject";

        /// <summary>
        /// Message type for call endings.
        /// </summary>
        public const string CallEnd = "call_end";

        /// <summary>
        /// Message type for WebRTC SDP offers.
        /// </summary>
        public const string CallSdpOffer = "call_sdp_offer";

        /// <summary>
        /// Message type for WebRTC SDP answers.
        /// </summary>
        public const string CallSdpAnswer = "call_sdp_answer";

        /// <summary>
        /// Message type for WebRTC ICE candidates.
        /// </summary>
        public const string CallIceCandidate = "call_ice_candidate";

        /// <summary>
        /// Message type for call audio data.
        /// </summary>
        public const string CallAudioData = "call_audio_data";
    }
}
