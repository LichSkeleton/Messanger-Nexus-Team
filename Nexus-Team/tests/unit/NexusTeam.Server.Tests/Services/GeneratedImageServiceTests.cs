namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Models;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services;
    using NexusTeam.Shared.Abstractions;
    using Xunit;

    public class GeneratedImageServiceTests
    {
        [Fact]
        public async Task CreateAsync_NormalizesPromptAndPersistsMetadata()
        {
            var fixture = new Fixture();
            var before = DateTime.UtcNow;

            var result = await fixture.Service.CreateAsync(
                "user-1", "  draw a cat  ", "flux", "https://image", 512, 768);

            var after = DateTime.UtcNow;
            var created = Assert.IsType<GeneratedImage>(fixture.Repository.Created);
            Assert.Equal("image-1", created.Id);
            Assert.Equal("draw a cat", created.Prompt);
            Assert.Equal(512, created.Width);
            Assert.Equal(768, created.Height);
            Assert.False(created.IsDeleted);
            Assert.InRange(created.GeneratedAt, before, after);
            Assert.Equal(created.Id, result.Id);
            Assert.Null(result.DownloadUrl);
        }

        [Fact]
        public async Task CreateAsync_WithNullPrompt_StoresEmptyPrompt()
        {
            var fixture = new Fixture();

            var result = await fixture.Service.CreateAsync(
                "user-1", null!, "flux", "url", 1, 1);

            Assert.Equal(string.Empty, result.Prompt);
        }

        [Fact]
        public async Task GetByIdAsync_MapsImageAndDownloadUrl()
        {
            var fixture = new Fixture();
            fixture.Repository.ById = CreateImage(filePath: "/tmp/image.png");

            var result = await fixture.Service.GetByIdAsync("image-1");

            Assert.NotNull(result);
            Assert.Equal("/api/generated-images/image-1/download", result.DownloadUrl);
        }

        [Fact]
        public async Task GetByUserIdAsync_ForwardsLimitAndMapsImages()
        {
            var fixture = new Fixture();
            fixture.Repository.UserImages.Add(CreateImage());

            var result = (await fixture.Service.GetByUserIdAsync("user-1", 7)).ToList();

            Assert.Single(result);
            Assert.Equal(7, fixture.Repository.LastLimit);
        }

        [Fact]
        public async Task SaveImageDataAsync_WhenMissing_Throws()
        {
            var fixture = new Fixture();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.Service.SaveImageDataAsync("missing", new byte[] { 1 }));
        }

        [Fact]
        public async Task SaveImageDataAsync_WritesFileUpdatesRepositoryAndReturnsUrl()
        {
            var fixture = new Fixture();
            var image = CreateImage();
            fixture.Repository.ById = image;
            var bytes = new byte[] { 1, 2, 3, 4 };

            var url = await fixture.Service.SaveImageDataAsync("image-1", bytes);

            try
            {
                Assert.Equal("/api/generated-images/image-1/download", url);
                Assert.NotNull(image.FilePath);
                Assert.Equal(bytes, await File.ReadAllBytesAsync(image.FilePath));
                Assert.Same(image, fixture.Repository.Updated);

                var streamResult = await fixture.Service.GetImageStreamAsync("image-1");
                Assert.NotNull(streamResult);
                Assert.Equal("image/png", streamResult.Value.ContentType);
                await streamResult.Value.Stream!.DisposeAsync();
            }
            finally
            {
                if (image.FilePath != null && File.Exists(image.FilePath))
                {
                    File.Delete(image.FilePath);
                }
            }
        }

        [Fact]
        public async Task DeleteAsync_WhenMissingOrNotOwner_ReturnsFalse()
        {
            var fixture = new Fixture();
            Assert.False(await fixture.Service.DeleteAsync("missing", "user-1"));
            fixture.Repository.ById = CreateImage(owner: "user-2");
            Assert.False(await fixture.Service.DeleteAsync("image-1", "user-1"));
            Assert.Null(fixture.Repository.DeletedId);
        }

        [Fact]
        public async Task DeleteAsync_WhenOwner_DeletesFileAndRepositoryRecord()
        {
            var fixture = new Fixture();
            var image = CreateImage();
            fixture.Repository.ById = image;
            await fixture.Service.SaveImageDataAsync("image-1", new byte[] { 1 });
            var path = image.FilePath!;

            var result = await fixture.Service.DeleteAsync("image-1", "user-1");

            Assert.True(result);
            Assert.False(File.Exists(path));
            Assert.Equal("image-1", fixture.Repository.DeletedId);
        }

        [Fact]
        public async Task GetRecentPromptsAsync_ForwardsUserAndLimit()
        {
            var fixture = new Fixture();
            fixture.Repository.Prompts.Add("prompt-1");

            var result = await fixture.Service.GetRecentPromptsAsync("user-1", 3);

            Assert.Equal(new[] { "prompt-1" }, result);
            Assert.Equal("user-1", fixture.Repository.LastUserId);
            Assert.Equal(3, fixture.Repository.LastLimit);
        }

        private static GeneratedImage CreateImage(string owner = "user-1", string? filePath = null)
            => new GeneratedImage
            {
                Id = "image-1",
                UserId = owner,
                Prompt = "prompt",
                Model = "flux",
                ImageUrl = "url",
                FilePath = filePath,
                Width = 512,
                Height = 512,
                GeneratedAt = DateTime.UtcNow,
            };

        private sealed class Fixture
        {
            public Fixture()
            {
                this.Service = new GeneratedImageService(this.Repository, new FixedId());
            }

            public FakeRepository Repository { get; } = new FakeRepository();

            public GeneratedImageService Service { get; }
        }

        private sealed class FixedId : IIdGenerator
        {
            public string GenerateId() => "image-1";
        }

        private sealed class FakeRepository : IGeneratedImageRepository
        {
            public GeneratedImage? ById { get; set; }

            public List<GeneratedImage> UserImages { get; } = new List<GeneratedImage>();

            public List<string> Prompts { get; } = new List<string>();

            public GeneratedImage? Created { get; private set; }

            public GeneratedImage? Updated { get; private set; }

            public string? DeletedId { get; private set; }

            public string? LastUserId { get; private set; }

            public int LastLimit { get; private set; }

            public Task<GeneratedImage?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
                => Task.FromResult(this.ById);

            public Task<IEnumerable<GeneratedImage>> GetByUserIdAsync(string userId, int limit = 50, CancellationToken cancellationToken = default)
            {
                this.LastUserId = userId;
                this.LastLimit = limit;
                return Task.FromResult<IEnumerable<GeneratedImage>>(this.UserImages);
            }

            public Task CreateAsync(GeneratedImage image, CancellationToken cancellationToken = default)
            {
                this.Created = image;
                return Task.CompletedTask;
            }

            public Task UpdateAsync(GeneratedImage image, CancellationToken cancellationToken = default)
            {
                this.Updated = image;
                return Task.CompletedTask;
            }

            public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            {
                this.DeletedId = id;
                return Task.CompletedTask;
            }

            public Task<IEnumerable<string>> GetRecentPromptsAsync(string userId, int limit = 10, CancellationToken cancellationToken = default)
            {
                this.LastUserId = userId;
                this.LastLimit = limit;
                return Task.FromResult<IEnumerable<string>>(this.Prompts);
            }
        }
    }
}
