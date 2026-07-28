namespace NexusTeam.Server.Configuration.Options
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Configuration options for MongoDB connection.
    /// </summary>
    public class MongoOptions
    {
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public const string SectionName = "MongoDB";

        /// <summary>
        /// Gets or sets the MongoDB connection string.
        /// </summary>
        [Required(ErrorMessage = "MongoDB connection string is required")]
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        [Required(ErrorMessage = "MongoDB database name is required")]
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the connection timeout in seconds.
        /// </summary>
        [Range(1, 60, ErrorMessage = "Connection timeout must be between 1 and 60 seconds")]
        public int ConnectionTimeout { get; set; } = 10;

        /// <summary>
        /// Gets or sets the server selection timeout in seconds.
        /// </summary>
        [Range(1, 60, ErrorMessage = "Server selection timeout must be between 1 and 60 seconds")]
        public int ServerSelectionTimeout { get; set; } = 5;
    }
}
