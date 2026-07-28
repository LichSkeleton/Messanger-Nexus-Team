namespace NexusTeam.Shared.Configuration
{
    /// <summary>
    /// Configuration settings for JWT token generation and validation.
    /// Should be populated from application configuration (appsettings.json).
    /// </summary>
    public class JwtSettings
    {
        /// <summary>
        /// Gets or sets the secret key used for signing tokens.
        /// Must be at least 256 bits (32 bytes) for HS256 algorithm.
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token issuer (typically the application name or URL).
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token audience (typically the client application or API).
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the access token expiration time in minutes.
        /// Default is 60 minutes (1 hour).
        /// </summary>
        public int AccessTokenExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// Gets or sets the refresh token expiration time in days.
        /// Default is 7 days.
        /// </summary>
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}
