namespace NexusTeam.Server.Configuration.Options
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Configuration options for JWT token generation and validation.
    /// </summary>
    public class JwtOptions
    {
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public const string SectionName = "Jwt";

        /// <summary>
        /// Gets or sets the secret key for signing tokens.
        /// </summary>
        [Required(ErrorMessage = "JWT secret key is required")]
        [MinLength(32, ErrorMessage = "JWT secret key must be at least 32 characters")]
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token issuer.
        /// </summary>
        [Required(ErrorMessage = "JWT issuer is required")]
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token audience.
        /// </summary>
        [Required(ErrorMessage = "JWT audience is required")]
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token expiration time in minutes.
        /// </summary>
        [Range(1, 1440, ErrorMessage = "Token expiration must be between 1 and 1440 minutes")]
        public int ExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// Gets or sets the refresh token expiration time in days.
        /// </summary>
        [Range(1, 90, ErrorMessage = "Refresh token expiration must be between 1 and 90 days")]
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}
