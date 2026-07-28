namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// Response DTO for successful authentication.
    /// Sent from server to client after login.
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// Gets or sets the JWT access token for authenticated requests.
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the refresh token for obtaining new access tokens.
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the token expiration time in seconds.
        /// </summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Gets or sets the authenticated user information.
        /// </summary>
        public UserDto User { get; set; } = new UserDto();
    }
}
