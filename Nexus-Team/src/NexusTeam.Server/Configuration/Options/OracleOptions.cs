namespace NexusTeam.Server.Configuration.Options
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Configuration options for Oracle database connection.
    /// </summary>
    public class OracleOptions
    {
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public const string SectionName = "Oracle";

        /// <summary>
        /// Gets or sets the Oracle connection string.
        /// </summary>
        [Required(ErrorMessage = "Oracle connection string is required")]
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the command timeout in seconds.
        /// </summary>
        [Range(1, 300, ErrorMessage = "Command timeout must be between 1 and 300 seconds")]
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for transient failures.
        /// </summary>
        [Range(0, 5, ErrorMessage = "Max retry attempts must be between 0 and 5")]
        public int MaxRetryAttempts { get; set; } = 3;
    }
}
