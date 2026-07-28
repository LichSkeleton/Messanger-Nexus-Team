namespace NexusTeam.Server.Data.Repositories
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Models;

    /// <summary>
    /// Repository interface for message data operations.
    /// </summary>
    public interface IMessageRepository
    {
        /// <summary>
        /// Gets a message by its unique identifier.
        /// </summary>
        /// <param name="id">The message identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The message if found, null otherwise.</returns>
        Task<Message?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all messages for a specific chat.
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <param name="limit">Maximum number of messages to retrieve.</param>
        /// <param name="offset">Number of messages to skip.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of messages in the chat.</returns>
        Task<IEnumerable<Message>> GetByChatIdAsync(string chatId, int limit = 50, int offset = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new message.
        /// </summary>
        /// <param name="message">The message to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateAsync(Message message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing message.
        /// </summary>
        /// <param name="message">The message to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(Message message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes a message by its identifier.
        /// </summary>
        /// <param name="id">The message identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of messages in a chat.
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of messages in the chat.</returns>
        Task<int> GetMessageCountAsync(string chatId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for messages matching a query within a chat.
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <param name="query">The search query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of matching messages.</returns>
        Task<IEnumerable<Message>> SearchAsync(string chatId, string query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all messages for a specific chat (hard delete).
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The IDs of deleted messages.</returns>
        Task<List<string>> DeleteByChatIdAsync(string chatId, CancellationToken cancellationToken = default);
    }
}
