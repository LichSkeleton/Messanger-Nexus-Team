namespace NexusTeam.Server.Services
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Services.Abstractions;
    using Serilog;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.PixelFormats;
    using SixLabors.ImageSharp.Processing;

    /// <summary>
    /// Implementation of avatar service for managing user avatars.
    /// </summary>
    public class AvatarService : IAvatarService
    {
        private readonly ILogger logger;
        private readonly string storagePath;
        private readonly string defaultAvatarPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvatarService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public AvatarService(ILogger logger)
        {
            this.logger = logger;

            // Set storage path for avatars
            this.storagePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Storage",
                "Avatars");

            Directory.CreateDirectory(this.storagePath);

            // Default avatar path (will be copied from client or created)
            this.defaultAvatarPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Storage",
                "Avatars",
                "default.jpg");
        }

        /// <inheritdoc/>
        public async Task<string> SaveAvatarAsync(
            string userId,
            string fileName,
            Stream fileStream,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Always save as JPEG for consistency
                var avatarFileName = $"{userId}.jpg";
                var avatarPath = Path.Combine(this.storagePath, avatarFileName);

                // Process and save image
                using (var image = await Image.LoadAsync(fileStream, cancellationToken))
                {
                    // Resize to max 512x512 for avatars (maintain aspect ratio)
                    const int maxSize = 512;
                    if (image.Width > maxSize || image.Height > maxSize)
                    {
                        var ratioX = (double)maxSize / image.Width;
                        var ratioY = (double)maxSize / image.Height;
                        var ratio = Math.Min(ratioX, ratioY);

                        var newWidth = (int)(image.Width * ratio);
                        var newHeight = (int)(image.Height * ratio);

                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(newWidth, newHeight),
                            Mode = ResizeMode.Max,
                        }));
                    }

                    // Save as JPEG with quality 85
                    await image.SaveAsJpegAsync(avatarPath, cancellationToken);
                }

                // Return relative URL path
                var relativePath = $"/api/users/avatar/{userId}";
                this.logger.Information("Avatar saved for user {UserId} at {Path}", userId, avatarPath);
                return relativePath;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to save avatar for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Stream?> GetAvatarStreamAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var avatarFileName = $"{userId}.jpg";
                var avatarPath = Path.Combine(this.storagePath, avatarFileName);

                if (File.Exists(avatarPath))
                {
                    var stream = new FileStream(avatarPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    return stream;
                }

                // Return default avatar if user avatar not found
                return await this.GetDefaultAvatarStreamAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to get avatar stream for user {UserId}", userId);
                return await this.GetDefaultAvatarStreamAsync(cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task<Stream> GetDefaultAvatarStreamAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // If default avatar exists, return it
                if (File.Exists(this.defaultAvatarPath))
                {
                    return new FileStream(this.defaultAvatarPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }

                // Create a simple default avatar (solid color circle)
                this.logger.Information("Creating default avatar at {Path}", this.defaultAvatarPath);
                using (var image = new Image<Rgba32>(200, 200))
                {
                    // Fill with a nice color
                    var color1 = new Rgba32(0x33, 0x35, 0x4E); // #33354E

                    // Fill all pixels with the color
                    for (int y = 0; y < image.Height; y++)
                    {
                        for (int x = 0; x < image.Width; x++)
                        {
                            image[x, y] = color1;
                        }
                    }

                    // For now, just save a solid color image
                    await image.SaveAsJpegAsync(this.defaultAvatarPath, cancellationToken);
                }

                return new FileStream(this.defaultAvatarPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to get default avatar stream");

                // Return a memory stream with a simple placeholder
                var memoryStream = new MemoryStream();

                // Create a minimal 1x1 pixel image
                using (var image = new Image<Rgba32>(1, 1))
                {
                    image[0, 0] = new Rgba32(0x33, 0x35, 0x4E);
                    await image.SaveAsJpegAsync(memoryStream, cancellationToken);
                }

                memoryStream.Position = 0;
                return memoryStream;
            }
        }
    }
}
