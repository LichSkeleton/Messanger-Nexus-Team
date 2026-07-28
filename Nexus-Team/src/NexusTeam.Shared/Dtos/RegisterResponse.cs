namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// Response DTO for successful user registration.
    /// Sent from server to client after account creation.
    /// </summary>
    public class RegisterResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether registration was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets an error message if registration failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the newly created user information.
        /// Only populated if registration was successful.
        /// </summary>
        public UserDto? User { get; set; }
    }
}
