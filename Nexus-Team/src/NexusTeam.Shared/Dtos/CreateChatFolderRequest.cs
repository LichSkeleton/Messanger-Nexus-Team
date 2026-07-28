namespace NexusTeam.Shared.Dtos
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request DTO for creating a chat folder.
    /// </summary>
    public class CreateChatFolderRequest
    {
        /// <summary>
        /// Gets or sets the folder name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of chat IDs to include in the folder.
        /// </summary>
        [JsonPropertyName("chatIds")]
        public List<string> ChatIds { get; set; } = new List<string>();
    }
}
