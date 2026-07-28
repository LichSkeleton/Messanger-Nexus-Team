namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// Request DTO for user registration.
    /// Sent from client to server to create a new account.
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// Gets or sets the desired username.
        /// Must be unique across the system.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email address.
        /// Must be unique and valid format.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password in plain text.
        /// Should be transmitted over secure connection only.
        /// Will be hashed before storage.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;
    }
}
