namespace NexusTeam.Shared.Dtos
{
    using System.Text.Json;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// Envelope for WebSocket message transmission.
    /// </summary>
    public class WebSocketMessageEnvelope
    {
        /// <summary>
        /// Gets or sets the type of the message.
        /// </summary>
        public WebSocketMessageType Type { get; set; }

        /// <summary>
        /// Gets or sets the message payload as JSON.
        /// </summary>
        public JsonElement? Payload { get; set; }

        /// <summary>
        /// Gets or sets the message ID for tracking.
        /// </summary>
        public string? MessageId { get; set; }

        /// <summary>
        /// Gets or sets the error message if Type is Error.
        /// </summary>
        public string? Error { get; set; }
    }
}
