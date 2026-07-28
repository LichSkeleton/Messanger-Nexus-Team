namespace NexusTeam.Shared.Contracts
{
    using System;

    /// <summary>
    /// WebSocket message contract for call requests.
    /// Sent when a user initiates a voice call to another user.
    /// JSON schema: { "type": "call_request", "callId": "...", "fromUserId": "...", "toUserId": "...", "chatId": "...", "timestamp": "..." }.
    /// </summary>
    public class CallRequestContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.CallRequest;

        /// <summary>
        /// Gets or sets the unique call identifier.
        /// </summary>
        public string CallId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the caller.
        /// </summary>
        public string FromUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user identifier of the callee.
        /// </summary>
        public string ToUserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat identifier (optional, if call is initiated from a chat).
        /// </summary>
        public string? ChatId { get; set; }

        /// <summary>
        /// Gets or sets the call request timestamp in ISO 8601 format.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
