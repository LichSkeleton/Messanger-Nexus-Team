namespace NexusTeam.Shared.Dtos
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request model for updating user profile.
    /// </summary>
    public class UpdateUserProfileRequest
    {
        /// <summary>
        /// Gets or sets the new display name.
        /// </summary>
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;
    }
}
