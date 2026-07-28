namespace NexusTeam.Shared.Contracts
{
    /// <summary>
    /// WebSocket message contract for typing indicators.
    /// Sent when a user starts or stops typing in a chat.
    /// JSON schema: { "type": "typing_indicator", "userId": "...", "chatId": "...", "username": "...", "isTyping": true/false }.
    /// </summary>
    public class TypingIndicatorContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.TypingIndicator;

        /// <summary>
        /// Gets or sets the user identifier of the person typing.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat identifier where typing is occurring.
        /// </summary>
        public string ChatId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the username of the person typing.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the user is currently typing.
        /// True when starting to type, false when stopped.
        /// </summary>
        public bool IsTyping { get; set; }
    }
}
