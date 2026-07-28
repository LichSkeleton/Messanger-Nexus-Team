namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// Response payload for WebSocket authentication message.
    /// </summary>
    public class AuthenticateResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether authentication was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the resume token for session resumption.
        /// </summary>
        public string? ResumeToken { get; set; }
    }
}
