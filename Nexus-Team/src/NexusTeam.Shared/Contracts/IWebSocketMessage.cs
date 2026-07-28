namespace NexusTeam.Shared.Contracts
{
    /// <summary>
    /// Base interface for all WebSocket messages.
    /// All real-time messages sent over WebSocket must implement this interface.
    /// The Type property is used for message discrimination during deserialization.
    /// </summary>
    public interface IWebSocketMessage
    {
        /// <summary>
        /// Gets the message type identifier for routing and deserialization.
        /// Must be unique across all message types.
        /// </summary>
        string Type { get; }
    }
}
