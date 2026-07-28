namespace NexusTeam.Server.Data.Repositories
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Models;

    /// <summary>
    /// Repository interface for generated images.
    /// </summary>
    public interface IGeneratedImageRepository
    {
        /// <summary>
        /// Gets a generated image by ID.
        /// </summary>
        /// <param name="id">The image ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated image or null.</returns>
        Task<GeneratedImage?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all generated images for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="limit">Maximum number of images to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of generated images.</returns>
        Task<IEnumerable<GeneratedImage>> GetByUserIdAsync(string userId, int limit = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new generated image record.
        /// </summary>
        /// <param name="image">The image to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        Task CreateAsync(GeneratedImage image, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a generated image record.
        /// </summary>
        /// <param name="image">The image to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        Task UpdateAsync(GeneratedImage image, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a generated image (soft delete).
        /// </summary>
        /// <param name="id">The image ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the async operation.</returns>
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets recent prompts for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="limit">Maximum number of prompts to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of recent prompts.</returns>
        Task<IEnumerable<string>> GetRecentPromptsAsync(string userId, int limit = 10, CancellationToken cancellationToken = default);
    }
}
