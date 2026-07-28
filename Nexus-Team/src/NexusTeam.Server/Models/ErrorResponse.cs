namespace NexusTeam.Server.Models
{
    /// <summary>
    /// Represents a generic error response.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional error details.
        /// </summary>
        public string? Details { get; set; }
    }
}
