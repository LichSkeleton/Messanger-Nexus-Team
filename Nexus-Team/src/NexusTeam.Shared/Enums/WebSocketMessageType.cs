namespace NexusTeam.Shared.Enums
{
    /// <summary>
    /// Represents the type of WebSocket message.
    /// </summary>
    public enum WebSocketMessageType
    {
        /// <summary>
        /// New chat message from user.
        /// </summary>
        NewMessage = 0,

        /// <summary>
        /// Edit existing message.
        /// </summary>
        EditMessage = 1,

        /// <summary>
        /// Delete a message.
        /// </summary>
        DeleteMessage = 2,

        /// <summary>
        /// Message delivery confirmation.
        /// </summary>
        MessageDelivered = 3,

        /// <summary>
        /// Message read confirmation.
        /// </summary>
        MessageRead = 4,

        /// <summary>
        /// User typing indicator.
        /// </summary>
        Typing = 5,

        /// <summary>
        /// User status update.
        /// </summary>
        StatusUpdate = 6,

        /// <summary>
        /// Heartbeat/ping message.
        /// </summary>
        Heartbeat = 7,

        /// <summary>
        /// Error message.
        /// </summary>
        Error = 8,

        /// <summary>
        /// Authentication message.
        /// </summary>
        Authenticate = 9,

        /// <summary>
        /// Resume session with token.
        /// </summary>
        Resume = 10,

        /// <summary>
        /// Message reaction update (add or remove).
        /// </summary>
        MessageReaction = 11,

        /// <summary>
        /// User avatar update.
        /// </summary>
        AvatarUpdate = 12,

        /// <summary>
        /// Chat deleted notification.
        /// </summary>
        ChatDeleted = 13,

        /// <summary>
        /// Call request (initiate a voice call).
        /// </summary>
        CallRequest = 14,

        /// <summary>
        /// Call answer (accept incoming call).
        /// </summary>
        CallAnswer = 15,

        /// <summary>
        /// Call reject (reject incoming call).
        /// </summary>
        CallReject = 16,

        /// <summary>
        /// Call end (end an active call).
        /// </summary>
        CallEnd = 17,

        /// <summary>
        /// WebRTC SDP offer.
        /// </summary>
        CallSdpOffer = 18,

        /// <summary>
        /// WebRTC SDP answer.
        /// </summary>
        CallSdpAnswer = 19,

        /// <summary>
        /// WebRTC ICE candidate.
        /// </summary>
        CallIceCandidate = 20,

        /// <summary>
        /// Call audio data (raw audio bytes).
        /// </summary>
        CallAudioData = 21,

        /// <summary>
        /// New chat created notification.
        /// </summary>
        ChatCreated = 22,

        /// <summary>
        /// Chat metadata updated notification (e.g. group name or avatar).
        /// </summary>
        ChatUpdated = 23,
    }
}
