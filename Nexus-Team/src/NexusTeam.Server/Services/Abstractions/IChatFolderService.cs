namespace NexusTeam.Server.Services.Abstractions
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Service for chat folder operations.
    /// </summary>
    public interface IChatFolderService
    {
        /// <summary>
        /// Gets all folders for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of folder DTOs.</returns>
        Task<IEnumerable<ChatFolderDto>> GetUserFoldersAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific folder by ID.
        /// </summary>
        /// <param name="folderId">The folder ID.</param>
        /// <param name="userId">The user ID for authorization.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The folder DTO if found.</returns>
        Task<ChatFolderDto?> GetFolderByIdAsync(string folderId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        /// <param name="request">The create folder request data.</param>
        /// <param name="userId">The user ID of the folder creator.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created folder DTO.</returns>
        Task<ChatFolderDto> CreateFolderAsync(CreateChatFolderRequest request, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing folder.
        /// </summary>
        /// <param name="folderId">The folder ID.</param>
        /// <param name="request">The update request data.</param>
        /// <param name="userId">The user ID for authorization.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated folder DTO.</returns>
        Task<ChatFolderDto> UpdateFolderAsync(string folderId, CreateChatFolderRequest request, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a folder.
        /// </summary>
        /// <param name="folderId">The folder ID.</param>
        /// <param name="userId">The user ID for authorization.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteFolderAsync(string folderId, string userId, CancellationToken cancellationToken = default);
    }
}
