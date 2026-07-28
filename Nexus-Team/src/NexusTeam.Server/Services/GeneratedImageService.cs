namespace NexusTeam.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Models;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Service for managing generated images.
    /// </summary>
    public class GeneratedImageService : IGeneratedImageService
    {
        private readonly IGeneratedImageRepository repository;
        private readonly IIdGenerator idGenerator;
        private readonly string storagePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedImageService"/> class.
        /// </summary>
        /// <param name="repository">The generated image repository.</param>
        /// <param name="idGenerator">The ID generator.</param>
        public GeneratedImageService(IGeneratedImageRepository repository, IIdGenerator idGenerator)
        {
            this.repository = repository;
            this.idGenerator = idGenerator;

            // Set up storage path
            this.storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage", "generated_images");
            Directory.CreateDirectory(this.storagePath);
        }

        /// <inheritdoc/>
        public async Task<GeneratedImageDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var image = await this.repository.GetByIdAsync(id, cancellationToken);
            return image != null ? this.MapToDto(image) : null;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<GeneratedImageDto>> GetByUserIdAsync(string userId, int limit = 50, CancellationToken cancellationToken = default)
        {
            var images = await this.repository.GetByUserIdAsync(userId, limit, cancellationToken);
            return images.Select(this.MapToDto);
        }

        /// <inheritdoc/>
        public async Task<GeneratedImageDto> CreateAsync(
            string userId,
            string prompt,
            string model,
            string imageUrl,
            int width,
            int height,
            CancellationToken cancellationToken = default)
        {
            // Normalize prompt to avoid leading/trailing whitespace in storage
            var normalizedPrompt = (prompt ?? string.Empty).Trim();

            var image = new GeneratedImage
            {
                Id = this.idGenerator.GenerateId(),
                UserId = userId,
                Prompt = normalizedPrompt,
                Model = model,
                ImageUrl = imageUrl,
                Width = width,
                Height = height,
                GeneratedAt = DateTime.UtcNow,
                IsDeleted = false,
            };

            await this.repository.CreateAsync(image, cancellationToken);
            return this.MapToDto(image);
        }

        /// <inheritdoc/>
        public async Task<string> SaveImageDataAsync(string id, byte[] imageData, CancellationToken cancellationToken = default)
        {
            var image = await this.repository.GetByIdAsync(id, cancellationToken);
            if (image == null)
            {
                throw new InvalidOperationException("Image not found");
            }

            var fileName = $"{id}.png";
            var filePath = Path.Combine(this.storagePath, fileName);

            await File.WriteAllBytesAsync(filePath, imageData, cancellationToken);

            image.FilePath = filePath;
            await this.repository.UpdateAsync(image, cancellationToken);

            return $"/api/generated-images/{id}/download";
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(string id, string userId, CancellationToken cancellationToken = default)
        {
            var image = await this.repository.GetByIdAsync(id, cancellationToken);
            if (image == null || image.UserId != userId)
            {
                return false;
            }

            // Delete file if exists
            if (!string.IsNullOrEmpty(image.FilePath) && File.Exists(image.FilePath))
            {
                try
                {
                    File.Delete(image.FilePath);
                }
                catch
                {
                    // Ignore file deletion errors
                }
            }

            await this.repository.DeleteAsync(id, cancellationToken);
            return true;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetRecentPromptsAsync(string userId, int limit = 10, CancellationToken cancellationToken = default)
        {
            return await this.repository.GetRecentPromptsAsync(userId, limit, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<(Stream? Stream, string ContentType)?> GetImageStreamAsync(string id, CancellationToken cancellationToken = default)
        {
            var image = await this.repository.GetByIdAsync(id, cancellationToken);
            if (image == null || string.IsNullOrEmpty(image.FilePath) || !File.Exists(image.FilePath))
            {
                return null;
            }

            var stream = File.OpenRead(image.FilePath);
            return (stream, "image/png");
        }

        private GeneratedImageDto MapToDto(GeneratedImage image)
        {
            return new GeneratedImageDto
            {
                Id = image.Id ?? string.Empty,
                UserId = image.UserId,
                Prompt = image.Prompt,
                Model = image.Model,
                ImageUrl = image.ImageUrl,
                DownloadUrl = !string.IsNullOrEmpty(image.FilePath) ? $"/api/generated-images/{image.Id}/download" : null,
                Width = image.Width,
                Height = image.Height,
                GeneratedAt = image.GeneratedAt,
            };
        }
    }
}
