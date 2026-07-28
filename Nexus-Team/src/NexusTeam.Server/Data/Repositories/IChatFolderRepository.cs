namespace NexusTeam.Server.Data.Repositories
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Models;

    /// <summary>
    /// Repository interface for chat folder data operations.
    /// </summary>
    public interface IChatFolderRepository
    {
        /// <summary>
        /// Gets a folder by its unique identifier.
        /// </summary>
        /// <param name="id">The folder identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The folder if found, null otherwise.</returns>
        Task<ChatFolder?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all folders for a specific user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of folders owned by the user.</returns>
        Task<IEnumerable<ChatFolder>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        /// <param name="folder">The folder to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateAsync(ChatFolder folder, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing folder.
        /// </summary>
        /// <param name="folder">The folder to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(ChatFolder folder, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a folder by its identifier.
        /// </summary>
        /// <param name="id">The folder identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a chat from all folders.
        /// </summary>
        /// <param name="chatId">The chat identifier to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveChatFromAllFoldersAsync(string chatId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a chat from folders belonging to a specific user.
        /// </summary>
        /// <param name="chatId">The chat identifier to remove.</param>
        /// <param name="userId">The user whose folders should be updated.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveChatFromUserFoldersAsync(string chatId, string userId, CancellationToken cancellationToken = default);
    }
}
