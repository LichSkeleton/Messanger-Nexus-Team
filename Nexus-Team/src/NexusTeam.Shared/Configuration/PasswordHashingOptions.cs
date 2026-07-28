namespace NexusTeam.Shared.Configuration
{
    /// <summary>
    /// Configuration options for password hashing using BCrypt.
    /// </summary>
    public class PasswordHashingOptions
    {
        /// <summary>
        /// Gets or sets the work factor (number of rounds) for BCrypt hashing.
        /// Higher values increase security but also increase computation time.
        /// Recommended minimum is 10, default is 12.
        /// </summary>
        public int WorkFactor { get; set; } = 12;
    }
}
