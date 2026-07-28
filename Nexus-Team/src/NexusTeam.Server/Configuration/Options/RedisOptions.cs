namespace NexusTeam.Server.Configuration.Options
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Configuration options for Redis connection.
    /// </summary>
    public class RedisOptions
    {
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public const string SectionName = "Redis";

        /// <summary>
        /// Gets or sets the Redis connection string.
        /// </summary>
        [Required(ErrorMessage = "Redis connection string is required")]
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the default database index.
        /// </summary>
        [Range(-1, 15, ErrorMessage = "Database index must be between -1 and 15")]
        public int DefaultDatabase { get; set; } = -1;

        /// <summary>
        /// Gets or sets the connect timeout in milliseconds.
        /// </summary>
        [Range(1000, 30000, ErrorMessage = "Connect timeout must be between 1000 and 30000 milliseconds")]
        public int ConnectTimeout { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the sync timeout in milliseconds.
        /// </summary>
        [Range(1000, 30000, ErrorMessage = "Sync timeout must be between 1000 and 30000 milliseconds")]
        public int SyncTimeout { get; set; } = 5000;

        /// <summary>
        /// Gets or sets a value indicating whether to abort on connect fail.
        /// </summary>
        public bool AbortOnConnectFail { get; set; } = false;
    }
}
