namespace NexusTeam.Server.Data.Repositories
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Models;

    /// <summary>
    /// Repository interface for chat data operations.
    /// </summary>
    public interface IChatRepository
    {
        /// <summary>
        /// Gets a chat by its unique identifier.
        /// </summary>
        /// <param name="id">The chat identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The chat if found, null otherwise.</returns>
        Task<Chat?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all chats for a specific user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of chats the user participates in.</returns>
        Task<IEnumerable<Chat>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new chat.
        /// </summary>
        /// <param name="chat">The chat to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateAsync(Chat chat, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing chat.
        /// </summary>
        /// <param name="chat">The chat to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(Chat chat, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a chat by its identifier.
        /// </summary>
        /// <param name="id">The chat identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a participant to a chat.
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddParticipantAsync(string chatId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a participant from a chat.
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveParticipantAsync(string chatId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a chat name already exists for a specific user.
        /// </summary>
        /// <param name="name">The chat name to check.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the name exists for the user, false otherwise.</returns>
        Task<bool> ChatNameExistsForUserAsync(string name, string userId, CancellationToken cancellationToken = default);
    }
}
