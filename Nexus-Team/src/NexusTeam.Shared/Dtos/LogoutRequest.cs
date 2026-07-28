namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// Request DTO for user logout.
    /// </summary>
    public class LogoutRequest
    {
        /// <summary>
        /// Gets or sets the refresh token to revoke.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;
    }
}
