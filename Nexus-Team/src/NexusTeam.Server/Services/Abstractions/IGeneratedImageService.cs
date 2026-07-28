namespace NexusTeam.Server.Services.Abstractions
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Service interface for managing generated images.
    /// </summary>
    public interface IGeneratedImageService
    {
        /// <summary>
        /// Gets a generated image by ID.
        /// </summary>
        /// <param name="id">The image ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated image DTO or null.</returns>
        Task<GeneratedImageDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all generated images for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="limit">Maximum number of images to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of generated image DTOs.</returns>
        Task<IEnumerable<GeneratedImageDto>> GetByUserIdAsync(string userId, int limit = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new generated image record.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="prompt">The prompt used.</param>
        /// <param name="model">The model used.</param>
        /// <param name="imageUrl">The generated image URL.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created image DTO.</returns>
        Task<GeneratedImageDto> CreateAsync(
            string userId,
            string prompt,
            string model,
            string imageUrl,
            int width,
            int height,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the image data to server storage.
        /// </summary>
        /// <param name="id">The image ID.</param>
        /// <param name="imageData">The image bytes.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The download URL.</returns>
        Task<string> SaveImageDataAsync(string id, byte[] imageData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a generated image.
        /// </summary>
        /// <param name="id">The image ID.</param>
        /// <param name="userId">The user ID (for authorization).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if deleted, false otherwise.</returns>
        Task<bool> DeleteAsync(string id, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets recent prompts for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="limit">Maximum number of prompts to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of recent prompts.</returns>
        Task<IEnumerable<string>> GetRecentPromptsAsync(string userId, int limit = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the image file stream for download.
        /// </summary>
        /// <param name="id">The image ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The file stream and content type, or null if not found.</returns>
        Task<(System.IO.Stream? Stream, string ContentType)?> GetImageStreamAsync(string id, CancellationToken cancellationToken = default);
    }
}
