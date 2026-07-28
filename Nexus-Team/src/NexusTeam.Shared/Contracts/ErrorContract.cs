namespace NexusTeam.Shared.Contracts
{
    /// <summary>
    /// WebSocket message contract for error notifications.
    /// Sent when an error occurs during WebSocket communication.
    /// JSON schema: { "type": "error", "code": "...", "message": "...", "details": "..." }.
    /// </summary>
    public class ErrorContract : IWebSocketMessage
    {
        /// <inheritdoc/>
        public string Type => MessageTypes.Error;

        /// <summary>
        /// Gets or sets the error code for programmatic handling.
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the human-readable error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional error details (optional).
        /// </summary>
        public string? Details { get; set; }
    }
}
