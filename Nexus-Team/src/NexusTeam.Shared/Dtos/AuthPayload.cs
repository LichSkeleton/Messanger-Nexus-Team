namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// Payload for WebSocket authentication message.
    /// </summary>
    public class AuthPayload
    {
        /// <summary>
        /// Gets or sets the JWT access token.
        /// </summary>
        public string? Token { get; set; }
    }
}
