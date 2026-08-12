namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services;
    using NexusTeam.Shared.Enums;
    using NexusTeam.Shared.Models;
    using Serilog;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.PixelFormats;
    using Xunit;

    public class AttachmentServiceTests
    {
        [Fact]
        public async Task SaveAttachmentAsync_ForDocument_PersistsBytesMetadataAndUrls()
        {
            var fixture = new Fixture();
            await using var input = new MemoryStream(new byte[] { 1, 2, 3 });

            var result = await fixture.Service.SaveAttachmentAsync("message-1", "notes.pdf", input, "application/pdf");

            try
            {
                Assert.Equal(AttachmentType.Document, result.AttachmentType);
                Assert.Equal(3, result.FileSize);
                Assert.Null(result.ThumbnailUrl);
                Assert.Equal($"/api/attachments/download/{result.Id}", result.DownloadUrl);
                Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(fixture.PathFor(fixture.Repository.Added!.FilePath)));
            }
            finally
            {
                fixture.Cleanup(fixture.Repository.Added);
            }
        }

        [Fact]
        public async Task SaveAttachmentAsync_ForImage_GeneratesBoundedThumbnail()
        {
            var fixture = new Fixture();
            await using var input = await CreateImageAsync(400, 200);

            var result = await fixture.Service.SaveAttachmentAsync("message-1", "photo.png", input, "image/png");

            try
            {
                Assert.Equal(AttachmentType.Image, result.AttachmentType);
                Assert.NotNull(result.ThumbnailUrl);
                using var thumbnail = await Image.LoadAsync(fixture.PathFor(fixture.Repository.Added!.ThumbnailPath!));
                Assert.Equal(200, thumbnail.Width);
                Assert.Equal(100, thumbnail.Height);
            }
            finally
            {
                fixture.Cleanup(fixture.Repository.Added);
            }
        }

        [Fact]
        public async Task GetAttachmentAsync_MapsExistingAndReturnsNullForMissing()
        {
            var fixture = new Fixture();
            Assert.Null(await fixture.Service.GetAttachmentAsync("missing"));
            fixture.Repository.ById = Model("a1", "file.bin");

            var result = await fixture.Service.GetAttachmentAsync("a1");

            Assert.NotNull(result);
            Assert.Equal("/api/attachments/download/a1", result.DownloadUrl);
        }

        [Fact]
        public async Task GetAttachmentStreamAsync_ReturnsNullForMissingRecordOrFile()
        {
            var fixture = new Fixture();
            Assert.Null(await fixture.Service.GetAttachmentStreamAsync("missing"));
            fixture.Repository.ById = Model("a1", "does-not-exist.bin");
            Assert.Null(await fixture.Service.GetAttachmentStreamAsync("a1"));
        }

        [Fact]
        public async Task GetAttachmentStreamAsync_ReturnsStoredBytes()
        {
            var fixture = new Fixture();
            var model = Model("a1", $"attachment-{Guid.NewGuid():N}.bin");
            fixture.Repository.ById = model;
            await File.WriteAllBytesAsync(fixture.PathFor(model.FilePath), new byte[] { 4, 5 });

            try
            {
                await using var stream = await fixture.Service.GetAttachmentStreamAsync("a1");
                Assert.NotNull(stream);
                using var copy = new MemoryStream();
                await stream.CopyToAsync(copy);
                Assert.Equal(new byte[] { 4, 5 }, copy.ToArray());
            }
            finally
            {
                fixture.Cleanup(model);
            }
        }

        [Fact]
        public async Task GetMessageAttachmentsAsync_MapsEveryRepositoryItem()
        {
            var fixture = new Fixture();
            fixture.Repository.ByMessage.Add(Model("a1", "one.bin"));
            fixture.Repository.ByMessage.Add(Model("a2", "two.bin"));

            var result = await fixture.Service.GetMessageAttachmentsAsync("message-1");

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetThumbnailStreamAsync_ReturnsNullWithoutMetadataOrFile()
        {
            var fixture = new Fixture();
            fixture.Repository.ById = Model("a1", "file.bin");
            Assert.Null(await fixture.Service.GetThumbnailStreamAsync("a1"));
            fixture.Repository.ById.ThumbnailPath = "missing-thumb.jpg";
            Assert.Null(await fixture.Service.GetThumbnailStreamAsync("a1"));
        }

        [Fact]
        public async Task UpdateAttachmentAsync_WhenMissing_Throws()
        {
            var fixture = new Fixture();
            await using var input = new MemoryStream(new byte[] { 1 });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.Service.UpdateAttachmentAsync("missing", input, "application/octet-stream"));
        }

        [Fact]
        public async Task UpdateAttachmentAsync_ReplacesFileAndUpdatesMetadata()
        {
            var fixture = new Fixture();
            var model = Model("a1", $"attachment-{Guid.NewGuid():N}.txt");
            fixture.Repository.ById = model;
            await File.WriteAllBytesAsync(fixture.PathFor(model.FilePath), new byte[] { 1 });
            await using var input = new MemoryStream(new byte[] { 7, 8, 9, 10 });

            try
            {
                var result = await fixture.Service.UpdateAttachmentAsync("a1", input, "text/plain");

                Assert.Equal(4, result.FileSize);
                Assert.Equal("text/plain", result.ContentType);
                Assert.Same(model, fixture.Repository.Updated);
                Assert.Equal(new byte[] { 7, 8, 9, 10 }, await File.ReadAllBytesAsync(fixture.PathFor(model.FilePath)));
            }
            finally
            {
                fixture.Cleanup(model);
            }
        }

        [Fact]
        public async Task DeleteAttachmentAsync_DeletesFilesAndRepositoryRecord()
        {
            var fixture = new Fixture();
            var model = Model("a1", $"attachment-{Guid.NewGuid():N}.bin");
            model.ThumbnailPath = $"attachment-{Guid.NewGuid():N}-thumb.jpg";
            fixture.Repository.ById = model;
            await File.WriteAllBytesAsync(fixture.PathFor(model.FilePath), new byte[] { 1 });
            await File.WriteAllBytesAsync(fixture.PathFor(model.ThumbnailPath), new byte[] { 2 });

            var result = await fixture.Service.DeleteAttachmentAsync("a1");

            Assert.True(result);
            Assert.False(File.Exists(fixture.PathFor(model.FilePath)));
            Assert.False(File.Exists(fixture.PathFor(model.ThumbnailPath)));
            Assert.Equal("a1", fixture.Repository.DeletedId);
        }

        [Fact]
        public async Task DeleteAttachmentAsync_WhenMissing_ReturnsFalse()
        {
            var fixture = new Fixture();
            Assert.False(await fixture.Service.DeleteAttachmentAsync("missing"));
        }

        private static MessageAttachment Model(string id, string filePath) => new MessageAttachment
        {
            Id = id, MessageId = "message-1", FileName = "file.bin", FilePath = filePath,
            ContentType = "application/octet-stream", AttachmentType = AttachmentType.Other, UploadedAt = DateTime.UtcNow,
        };

        private static async Task<MemoryStream> CreateImageAsync(int width, int height)
        {
            using var image = new Image<Rgba32>(width, height, new Rgba32(1, 2, 3));
            var stream = new MemoryStream();
            await image.SaveAsPngAsync(stream);
            stream.Position = 0;
            return stream;
        }

        private sealed class Fixture
        {
            public Fixture() => this.Service = new AttachmentService(this.Repository, new LoggerConfiguration().CreateLogger());

            public FakeRepository Repository { get; } = new FakeRepository();
            public AttachmentService Service { get; }
            public string PathFor(string relative) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Storage", "Attachments", relative);
            public void Cleanup(MessageAttachment? attachment)
            {
                if (attachment == null) return;
                foreach (var path in new[] { attachment.FilePath, attachment.ThumbnailPath })
                {
                    if (!string.IsNullOrEmpty(path) && File.Exists(this.PathFor(path))) File.Delete(this.PathFor(path));
                }
            }
        }

        private sealed class FakeRepository : IMessageAttachmentRepository
        {
            public MessageAttachment? ById { get; set; }
            public MessageAttachment? Added { get; private set; }
            public MessageAttachment? Updated { get; private set; }
            public string? DeletedId { get; private set; }
            public List<MessageAttachment> ByMessage { get; } = new List<MessageAttachment>();
            public Task AddAsync(MessageAttachment attachment, CancellationToken cancellationToken = default) { this.Added = attachment; this.ById = attachment; return Task.CompletedTask; }
            public Task<MessageAttachment?> GetByIdAsync(string attachmentId, CancellationToken cancellationToken = default) => Task.FromResult(this.ById);
            public Task<List<MessageAttachment>> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default) => Task.FromResult(this.ByMessage);
            public Task UpdateAsync(MessageAttachment attachment, CancellationToken cancellationToken = default) { this.Updated = attachment; return Task.CompletedTask; }
            public Task DeleteAsync(string attachmentId, CancellationToken cancellationToken = default) { this.DeletedId = attachmentId; return Task.CompletedTask; }
            public Task<List<MessageAttachment>> GetByMessageIdsAsync(List<string> messageIds, CancellationToken cancellationToken = default) => Task.FromResult(new List<MessageAttachment>());
            public Task<List<string>> DeleteByMessageIdsAsync(List<string> messageIds, CancellationToken cancellationToken = default) => Task.FromResult(new List<string>());
        }
    }
}
