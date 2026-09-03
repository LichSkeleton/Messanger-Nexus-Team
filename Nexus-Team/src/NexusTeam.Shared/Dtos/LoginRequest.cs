namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// Request DTO for user authentication.
    /// Sent from client to server during login.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Gets or sets the username or email for login.
        /// </summary>
        public string UsernameOrEmail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password in plain text.
        /// Should be transmitted over secure connection only.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>Gets or sets the stable browser device identifier.</summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>Gets or sets a human-readable browser/device label.</summary>
        public string DeviceName { get; set; } = string.Empty;
    }
}
