namespace NexusTeam.Client.Services
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for queueing messages when offline and flushing on reconnect.
    /// </summary>
    public interface IOfflineMessageQueue
    {
        /// <summary>
        /// Enqueues a message for later sending.
        /// </summary>
        /// <param name="message">The message to enqueue.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EnqueueAsync(QueuedMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all pending messages in the queue.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of queued messages.</returns>
        Task<List<QueuedMessage>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a message from the queue.
        /// </summary>
        /// <param name="messageId">The message ID to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveAsync(string messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all messages from the queue.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ClearAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of pending messages.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of queued messages.</returns>
        Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    }
}
