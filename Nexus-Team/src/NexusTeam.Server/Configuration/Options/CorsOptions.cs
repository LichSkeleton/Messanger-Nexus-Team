namespace NexusTeam.Server.Configuration.Options
{
    using System.Collections.Generic;

    /// <summary>
    /// Configuration options for CORS.
    /// </summary>
    public class CorsOptions
    {
        /// <summary>
        /// Gets the configuration section name.
        /// </summary>
        public const string SectionName = "Cors";

        /// <summary>
        /// Gets or sets the allowed origins.
        /// </summary>
        public List<string> AllowedOrigins { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether credentials are allowed.
        /// </summary>
        public bool AllowCredentials { get; set; } = true;

        /// <summary>
        /// Gets or sets the policy name.
        /// </summary>
        public string PolicyName { get; set; } = "NexusTeamCorsPolicy";
    }
}
