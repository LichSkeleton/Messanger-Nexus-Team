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

        /// <summary>
        /// Initializes a new instance of the <see cref="AvatarService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public AvatarService(ILogger logger)
        {
            this.logger = logger;

            this.storagePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Storage",
                "Avatars");

            Directory.CreateDirectory(this.storagePath);
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
                var avatarFileName = $"{userId}.jpg";
                var avatarPath = Path.Combine(this.storagePath, avatarFileName);

                using (var image = await Image.LoadAsync(fileStream, cancellationToken))
                {
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

                    await image.SaveAsJpegAsync(avatarPath, cancellationToken);
                }

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
                    return new FileStream(avatarPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }

                return await CreateDefaultAvatarAsync(userId, cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to get avatar stream for user {UserId}", userId);
                return await CreateDefaultAvatarAsync(userId, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public Task<Stream> GetDefaultAvatarStreamAsync(CancellationToken cancellationToken = default)
        {
            return CreateSilhouetteAvatarAsync("default", cancellationToken);
        }

        private static bool IsGroupAvatarKey(string key)
        {
            return !string.IsNullOrEmpty(key)
                && key.StartsWith("chat_", StringComparison.Ordinal);
        }

        private static Task<Stream> CreateDefaultAvatarAsync(string key, CancellationToken cancellationToken)
        {
            return IsGroupAvatarKey(key)
                ? CreateGroupSilhouetteAvatarAsync(key, cancellationToken)
                : CreateSilhouetteAvatarAsync(key, cancellationToken);
        }

        private static Rgba32 ColorForKey(string key)
        {
            var palette = new[]
            {
                new Rgba32(0x5B, 0x8D, 0xEF),
                new Rgba32(0x6B, 0xC9, 0x81),
                new Rgba32(0xE1, 0x70, 0x76),
                new Rgba32(0xA6, 0x95, 0xE7),
                new Rgba32(0xEE, 0x7A, 0xAE),
                new Rgba32(0x6E, 0xC9, 0xCB),
                new Rgba32(0xFA, 0xA7, 0x74),
                new Rgba32(0x64, 0xB5, 0xF6),
            };

            if (string.Equals(key, "default", StringComparison.OrdinalIgnoreCase))
            {
                return palette[0];
            }

            unchecked
            {
                var hash = 17;
                foreach (var character in key)
                {
                    hash = (hash * 31) + character;
                }

                if (hash < 0)
                {
                    hash = -hash;
                }

                return palette[hash % palette.Length];
            }
        }

        private static void FillCircle(Image<Rgba32> image, int centerX, int centerY, int radius, Rgba32 color)
        {
            FillEllipse(image, centerX, centerY, radius, radius, color);
        }

        private static void FillEllipse(Image<Rgba32> image, int centerX, int centerY, int radiusX, int radiusY, Rgba32 color)
        {
            if (radiusX <= 0 || radiusY <= 0)
            {
                return;
            }

            var minX = Math.Max(0, centerX - radiusX);
            var maxX = Math.Min(image.Width - 1, centerX + radiusX);
            var minY = Math.Max(0, centerY - radiusY);
            var maxY = Math.Min(image.Height - 1, centerY + radiusY);
            var rxSquared = (double)(radiusX * radiusX);
            var rySquared = (double)(radiusY * radiusY);

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x - centerX;
                    var dy = y - centerY;
                    if (((dx * dx) / rxSquared) + ((dy * dy) / rySquared) <= 1)
                    {
                        image[x, y] = color;
                    }
                }
            }
        }

        private static void DrawPerson(Image<Rgba32> image, int headX, int headY, int headRadius, int bodyY, int bodyRadius, Rgba32 color)
        {
            FillCircle(image, headX, headY, headRadius, color);
            FillCircle(image, headX, bodyY, bodyRadius, color);
        }

        private static void DrawBust(
            Image<Rgba32> image,
            int headX,
            int headY,
            int headRadius,
            int bodyX,
            int bodyY,
            int bodyRadiusX,
            int bodyRadiusY,
            Rgba32 color)
        {
            FillCircle(image, headX, headY, headRadius, color);
            FillEllipse(image, bodyX, bodyY, bodyRadiusX, bodyRadiusY, color);
        }

        private static async Task<Stream> CreateSilhouetteAvatarAsync(string key, CancellationToken cancellationToken)
        {
            const int size = 200;
            var background = ColorForKey(key);
            var silhouette = new Rgba32(255, 255, 255, 230);

            using var image = new Image<Rgba32>(size, size, background);
            DrawPerson(image, size / 2, 72, 38, 210, 78, silhouette);

            var memoryStream = new MemoryStream();
            await image.SaveAsJpegAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }

        private static async Task<Stream> CreateGroupSilhouetteAvatarAsync(string key, CancellationToken cancellationToken)
        {
            const int size = 200;
            var background = ColorForKey(key);
            var backPerson = new Rgba32(255, 255, 255, 205);
            var frontPerson = new Rgba32(255, 255, 255, 245);

            using var image = new Image<Rgba32>(size, size, background);

            // Two overlapping people (classic group icon). A background-colored
            // gap is drawn around the front person so the figures stay distinct.
            DrawBust(image, 72, 78, 30, 70, 168, 42, 38, backPerson);
            DrawBust(image, 130, 74, 36, 134, 172, 50, 44, background);
            DrawBust(image, 130, 74, 32, 134, 172, 46, 40, frontPerson);

            var memoryStream = new MemoryStream();
            await image.SaveAsJpegAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
