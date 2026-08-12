namespace NexusTeam.Server.Services.Abstractions
{
    using System.Collections.Generic;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages WebSocket connections for users.
    /// </summary>
    public interface IWebSocketConnectionManager
    {
        /// <summary>
        /// Adds a connection for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="socket">The WebSocket connection.</param>
        /// <param name="connectionId">The unique connection ID.</param>
        void AddConnection(string userId, WebSocket socket, string connectionId);

        /// <summary>
        /// Removes a connection.
        /// </summary>
        /// <param name="connectionId">The connection ID.</param>
        void RemoveConnection(string connectionId);

        /// <summary>
        /// Gets the WebSocket for a connection ID.
        /// </summary>
        /// <param name="connectionId">The connection ID.</param>
        /// <returns>The WebSocket if found, null otherwise.</returns>
        WebSocket? GetSocketByConnectionId(string connectionId);

        /// <summary>
        /// Gets all connection IDs for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>A collection of connection IDs.</returns>
        IEnumerable<string> GetConnectionIdsByUserId(string userId);

        /// <summary>
        /// Gets the user ID for a connection.
        /// </summary>
        /// <param name="connectionId">The connection ID.</param>
        /// <returns>The user ID if found, null otherwise.</returns>
        string? GetUserIdByConnectionId(string connectionId);

        /// <summary>
        /// Gets all user IDs that currently have at least one active connection.
        /// </summary>
        /// <returns>Distinct connected user IDs.</returns>
        IEnumerable<string> GetConnectedUserIds();

        /// <summary>
        /// Sends a message to a specific connection.
        /// </summary>
        /// <param name="connectionId">The connection ID.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendMessageAsync(string connectionId, string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Broadcasts a message to all connections of a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task BroadcastToUserAsync(string userId, string message, CancellationToken cancellationToken = default);
    }
}
