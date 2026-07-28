namespace NexusTeam.Server.Configuration.Options
{
    /// <summary>
    /// Configuration options for rate limiting.
    /// </summary>
    public class RateLimitOptions
    {
        /// <summary>
        /// Gets the configuration section name.
        /// </summary>
        public const string SectionName = "RateLimit";

        /// <summary>
        /// Gets or sets the maximum number of login attempts allowed per window.
        /// </summary>
        public int LoginMaxAttempts { get; set; } = 5;

        /// <summary>
        /// Gets or sets the login rate limit window in seconds.
        /// </summary>
        public int LoginWindowSeconds { get; set; } = 300;

        /// <summary>
        /// Gets or sets the maximum number of messages allowed per window.
        /// </summary>
        public int MessageMaxAttempts { get; set; } = 60;

        /// <summary>
        /// Gets or sets the message rate limit window in seconds.
        /// </summary>
        public int MessageWindowSeconds { get; set; } = 60;
    }
}
