namespace NexusTeam.Client.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Service interface for AI image generation using Pollinations API.
    /// </summary>
    public interface IImageGeneratorService
    {
        /// <summary>
        /// Generates an image using the Pollinations API.
        /// </summary>
        /// <param name="prompt">The text prompt for image generation.</param>
        /// <param name="model">The model to use (flux, turbo, gptimage).</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated image as BitmapImage and the raw bytes.</returns>
        Task<(BitmapImage Image, byte[] ImageData, string ImageUrl)> GenerateImageAsync(
            string prompt,
            string model = "flux",
            int width = 1024,
            int height = 1024,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves a generated image record to the server.
        /// </summary>
        /// <param name="prompt">The prompt used.</param>
        /// <param name="model">The model used.</param>
        /// <param name="imageUrl">The Pollinations image URL.</param>
        /// <param name="imageData">The image bytes.</param>
        /// <param name="width">Image width.</param>
        /// <param name="height">Image height.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created image DTO.</returns>
        Task<GeneratedImageDto> SaveGeneratedImageAsync(
            string prompt,
            string model,
            string imageUrl,
            byte[] imageData,
            int width,
            int height,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all generated images for the current user.
        /// </summary>
        /// <param name="limit">Maximum number of images.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of generated images.</returns>
        Task<List<GeneratedImageDto>> GetGeneratedImagesAsync(int limit = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a generated image by ID.
        /// </summary>
        /// <param name="id">The image ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated image DTO.</returns>
        Task<GeneratedImageDto?> GetGeneratedImageAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a generated image.
        /// </summary>
        /// <param name="id">The image ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if deleted successfully.</returns>
        Task<bool> DeleteGeneratedImageAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets recent prompts for the current user.
        /// </summary>
        /// <param name="limit">Maximum number of prompts.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of recent prompts.</returns>
        Task<List<string>> GetRecentPromptsAsync(int limit = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a stored generated image.
        /// </summary>
        /// <param name="downloadUrl">The download URL.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The image as BitmapImage.</returns>
        Task<BitmapImage> DownloadStoredImageAsync(string downloadUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves an image to a local file.
        /// </summary>
        /// <param name="imageData">The image bytes.</param>
        /// <param name="suggestedFileName">Suggested file name.</param>
        /// <returns>The saved file path, or null if cancelled.</returns>
        Task<string?> SaveImageToFileAsync(byte[] imageData, string suggestedFileName);
    }
}
