namespace NexusTeam.Server.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a validation error response.
    /// </summary>
    public class ValidationErrorResponse
    {
        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = "Validation failed";

        /// <summary>
        /// Gets or sets the validation errors by field.
        /// </summary>
        public Dictionary<string, List<string>> Errors { get; set; } = new Dictionary<string, List<string>>();
    }
}
