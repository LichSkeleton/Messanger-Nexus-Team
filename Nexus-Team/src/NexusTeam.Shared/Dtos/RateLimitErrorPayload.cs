namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// WebSocket error payload returned when a message send rate limit is exceeded.
    /// </summary>
    public class RateLimitErrorPayload
    {
        /// <summary>
        /// Gets or sets the short error label.
        /// </summary>
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the human-readable error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
