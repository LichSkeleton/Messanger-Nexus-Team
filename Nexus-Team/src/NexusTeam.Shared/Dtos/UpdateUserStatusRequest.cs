namespace NexusTeam.Shared.Dtos
{
    using System.Text.Json.Serialization;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// Request to update the authenticated user's presence status.
    /// </summary>
    public class UpdateUserStatusRequest
    {
        /// <summary>
        /// Gets or sets the desired status. Supported values from the client: Online, Invisible.
        /// </summary>
        [JsonPropertyName("status")]
        public UserStatus Status { get; set; }
    }
}
