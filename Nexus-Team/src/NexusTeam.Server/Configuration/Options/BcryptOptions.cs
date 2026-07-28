namespace NexusTeam.Server.Configuration.Options
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Configuration options for BCrypt password hashing.
    /// </summary>
    public class BcryptOptions
    {
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public const string SectionName = "Bcrypt";

        /// <summary>
        /// Gets or sets the work factor (cost) for bcrypt hashing.
        /// Higher values increase security but take longer to compute.
        /// Recommended range: 10-14.
        /// </summary>
        [Range(4, 31, ErrorMessage = "Bcrypt work factor must be between 4 and 31")]
        public int WorkFactor { get; set; } = 11;
    }
}
