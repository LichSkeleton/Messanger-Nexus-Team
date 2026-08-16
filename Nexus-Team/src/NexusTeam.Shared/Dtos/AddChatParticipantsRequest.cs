namespace NexusTeam.Shared.Dtos
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request to add one or more users to a group chat.
    /// </summary>
    public class AddChatParticipantsRequest
    {
        /// <summary>
        /// Gets or sets the user IDs to add.
        /// </summary>
        [JsonPropertyName("userIds")]
        public List<string> UserIds { get; set; } = new List<string>();
    }
}
