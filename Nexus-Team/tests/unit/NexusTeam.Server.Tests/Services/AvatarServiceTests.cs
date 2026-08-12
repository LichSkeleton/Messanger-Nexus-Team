namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using NexusTeam.Server.Services;
    using Serilog;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.PixelFormats;
    using Xunit;

    public class AvatarServiceTests
    {
        [Fact]
        public async Task SaveAvatarAsync_StoresJpegAndReturnsUserUrl()
        {
            var service = CreateService();
            var userId = "avatar-" + Guid.NewGuid().ToString("N");
            var path = GetAvatarPath(userId);
            await using var input = await CreateImageStreamAsync(100, 80);

            try
            {
                var url = await service.SaveAvatarAsync(userId, "photo.png", input);

                Assert.Equal($"/api/users/avatar/{userId}", url);
                Assert.True(File.Exists(path));
                using var saved = await Image.LoadAsync(path);
                Assert.Equal(100, saved.Width);
                Assert.Equal(80, saved.Height);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public async Task SaveAvatarAsync_WhenLarge_ResizesWithin512Boundary()
        {
            var service = CreateService();
            var userId = "avatar-" + Guid.NewGuid().ToString("N");
            var path = GetAvatarPath(userId);
            await using var input = await CreateImageStreamAsync(1000, 500);

            try
            {
                await service.SaveAvatarAsync(userId, "large.png", input);
                using var saved = await Image.LoadAsync(path);

                Assert.Equal(512, saved.Width);
                Assert.Equal(256, saved.Height);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public async Task GetAvatarStreamAsync_WhenAvatarExists_ReturnsStoredJpeg()
        {
            var service = CreateService();
            var userId = "avatar-" + Guid.NewGuid().ToString("N");
            var path = GetAvatarPath(userId);
            await using var input = await CreateImageStreamAsync(20, 20);

            try
            {
                await service.SaveAvatarAsync(userId, "avatar.png", input);
                await using var stream = await service.GetAvatarStreamAsync(userId);

                Assert.NotNull(stream);
                Assert.True(stream.Length > 0);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public async Task GetDefaultAvatarStreamAsync_ReturnsReadableJpeg()
        {
            var service = CreateService();

            await using var stream = await service.GetDefaultAvatarStreamAsync();
            using var image = await Image.LoadAsync(stream);

            Assert.True(image.Width > 0);
            Assert.True(image.Height > 0);
        }

        [Fact]
        public async Task SaveAvatarAsync_WithInvalidImage_PropagatesFailure()
        {
            var service = CreateService();
            await using var invalid = new MemoryStream(new byte[] { 1, 2, 3 });

            await Assert.ThrowsAnyAsync<Exception>(() =>
                service.SaveAvatarAsync("invalid-user", "bad.bin", invalid));
        }

        private static AvatarService CreateService()
            => new AvatarService(new LoggerConfiguration().CreateLogger());

        private static string GetAvatarPath(string userId)
            => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "Avatars", $"{userId}.jpg");

        private static async Task<MemoryStream> CreateImageStreamAsync(int width, int height)
        {
            using var image = new Image<Rgba32>(width, height, new Rgba32(10, 20, 30));
            var stream = new MemoryStream();
            await image.SaveAsPngAsync(stream);
            stream.Position = 0;
            return stream;
        }
    }
}
