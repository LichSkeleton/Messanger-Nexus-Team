namespace NexusTeam.Server.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Services.Abstractions;
    using Serilog;

    /// <summary>
    /// Manages WebSocket connections for users.
    /// </summary>
    public class WebSocketConnectionManager : IWebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<string, WebSocket> connections;
        private readonly ConcurrentDictionary<string, string> connectionToUser;
        private readonly ConcurrentDictionary<string, HashSet<string>> userToConnections;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketConnectionManager"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        public WebSocketConnectionManager(ILogger logger)
        {
            this.connections = new ConcurrentDictionary<string, WebSocket>();
            this.connectionToUser = new ConcurrentDictionary<string, string>();
            this.userToConnections = new ConcurrentDictionary<string, HashSet<string>>();
            this.logger = logger;
        }

        /// <inheritdoc/>
        public void AddConnection(string userId, WebSocket socket, string connectionId)
        {
            if (!this.connections.TryAdd(connectionId, socket))
            {
                this.logger.Warning(
                    "Rejected duplicate WebSocket connection {ConnectionId} for user {UserId}",
                    connectionId,
                    userId);
                return;
            }

            if (!this.connectionToUser.TryAdd(connectionId, userId))
            {
                this.connections.TryRemove(connectionId, out _);
                this.logger.Warning(
                    "Rejected WebSocket connection {ConnectionId} due to existing user mapping",
                    connectionId);
                return;
            }

            this.userToConnections.AddOrUpdate(
                userId,
                new HashSet<string> { connectionId },
                (key, existingSet) =>
                {
                    lock (existingSet)
                    {
                        existingSet.Add(connectionId);
                        return existingSet;
                    }
                });

            this.logger.Information("WebSocket connection {ConnectionId} added for user {UserId}", connectionId, userId);
        }

        /// <inheritdoc/>
        public void RemoveConnection(string connectionId)
        {
            this.connections.TryRemove(connectionId, out _);

            if (this.connectionToUser.TryRemove(connectionId, out var userId))
            {
                if (this.userToConnections.TryGetValue(userId, out var connectionSet))
                {
                    lock (connectionSet)
                    {
                        connectionSet.Remove(connectionId);
                        if (connectionSet.Count == 0)
                        {
                            this.userToConnections.TryRemove(userId, out _);
                        }
                    }
                }

                this.logger.Information("WebSocket connection {ConnectionId} removed for user {UserId}", connectionId, userId);
            }
        }

        /// <inheritdoc/>
        public WebSocket? GetSocketByConnectionId(string connectionId)
        {
            this.connections.TryGetValue(connectionId, out var socket);
            return socket;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetConnectionIdsByUserId(string userId)
        {
            if (this.userToConnections.TryGetValue(userId, out var connectionSet))
            {
                lock (connectionSet)
                {
                    return connectionSet.ToList();
                }
            }

            return Enumerable.Empty<string>();
        }

        /// <inheritdoc/>
        public string? GetUserIdByConnectionId(string connectionId)
        {
            this.connectionToUser.TryGetValue(connectionId, out var userId);
            return userId;
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetConnectedUserIds()
        {
            return this.connectionToUser.Values.Distinct().ToList();
        }

        /// <inheritdoc/>
        public async Task SendMessageAsync(string connectionId, string message, CancellationToken cancellationToken = default)
        {
            var socket = this.GetSocketByConnectionId(connectionId);
            if (socket != null && socket.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task BroadcastToUserAsync(string userId, string message, CancellationToken cancellationToken = default)
        {
            var connectionIds = this.GetConnectionIdsByUserId(userId);
            var tasks = connectionIds.Select(connId => this.SendMessageAsync(connId, message, cancellationToken));
            await Task.WhenAll(tasks);
        }
    }
}
