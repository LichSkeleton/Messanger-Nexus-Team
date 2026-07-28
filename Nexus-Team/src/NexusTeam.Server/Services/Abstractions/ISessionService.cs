namespace NexusTeam.Server.Services.Abstractions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for managing user sessions and message queues in Redis.
    /// </summary>
    public interface ISessionService
    {
        /// <summary>
        /// Creates a new session for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="connectionId">The connection ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The resume token.</returns>
        Task<string> CreateSessionAsync(string userId, string connectionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the session heartbeat.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateHeartbeatAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a session.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveSessionAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Queues a message for offline delivery.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="message">The message to queue.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task QueueMessageAsync(string userId, string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets queued messages for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of queued messages.</returns>
        Task<IEnumerable<string>> GetQueuedMessagesAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears the message queue for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ClearMessageQueueAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user has an active session.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the user has an active session, false otherwise.</returns>
        Task<bool> HasActiveSessionAsync(string userId, CancellationToken cancellationToken = default);
    }
}
