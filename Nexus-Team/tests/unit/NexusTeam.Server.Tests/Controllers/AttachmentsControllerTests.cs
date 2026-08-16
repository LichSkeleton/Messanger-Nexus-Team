namespace NexusTeam.Server.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Controllers;
    using NexusTeam.Server.Services;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using Serilog;
    using Xunit;

    public class AttachmentsControllerTests
    {
        [Fact]
        public async Task UploadAttachmentAsync_RequiresAuthentication()
        {
            var controller = CreateController(new FakeAttachments(), userId: null);
            Assert.IsType<UnauthorizedResult>((await controller.UploadAttachmentAsync(File("photo.png", 1, new byte[] { 1 }), "m1", default)).Result);
        }

        [Fact]
        public async Task UploadAttachmentAsync_RejectsEmptyDisallowedAndOversizedFiles()
        {
            var controller = CreateController(new FakeAttachments());
            Assert.IsType<BadRequestObjectResult>((await controller.UploadAttachmentAsync(File("empty.png", 0), "m1", default)).Result);
            Assert.IsType<BadRequestObjectResult>((await controller.UploadAttachmentAsync(File("malware.exe", 1), "m1", default)).Result);
            var oversized = (await controller.UploadAttachmentAsync(File("large.pdf", 101L * 1024 * 1024), "m1", default)).Result;
            Assert.Equal(413, Assert.IsType<ObjectResult>(oversized).StatusCode);
        }

        [Fact]
        public async Task UploadAttachmentAsync_OnSuccess_ReturnsOk()
        {
            var service = new FakeAttachments { Result = new MessageAttachmentDto { Id = "a1" } };
            var result = await CreateController(service).UploadAttachmentAsync(File("photo.png", 3, new byte[] { 1, 2, 3 }), "m1", default);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("m1", service.SavedMessageId);
        }

        [Fact]
        public async Task UploadAttachmentAsync_WhenServiceFails_Returns500()
        {
            var service = new FakeAttachments { Error = new IOException() };
            var result = await CreateController(service).UploadAttachmentAsync(File("photo.png", 1, new byte[] { 1 }), "m1", default);
            Assert.Equal(500, Assert.IsType<ObjectResult>(result.Result).StatusCode);
        }

        [Fact]
        public async Task DownloadAttachmentAsync_HandlesMissingRecordMissingFileAndSuccess()
        {
            var service = new FakeAttachments();
            var controller = CreateController(service);
            Assert.IsType<NotFoundObjectResult>(await controller.DownloadAttachmentAsync("a1", default));
            service.Result = new MessageAttachmentDto { Id = "a1", FileName = "file.txt", ContentType = "text/plain", MessageId = "m1" };
            Assert.IsType<NotFoundObjectResult>(await controller.DownloadAttachmentAsync("a1", default));
            service.Stream = new MemoryStream(new byte[] { 1 });
            var file = Assert.IsType<FileStreamResult>(await controller.DownloadAttachmentAsync("a1", default));
            Assert.Equal("file.txt", file.FileDownloadName);
            await file.FileStream.DisposeAsync();
        }

        [Fact]
        public async Task DownloadThumbnailAsync_HandlesMissingAndReturnsJpeg()
        {
            var service = new FakeAttachments();
            var controller = CreateController(service);
            Assert.IsType<NotFoundObjectResult>(await controller.DownloadThumbnailAsync("a1", default));
            service.Result = new MessageAttachmentDto { Id = "a1", MessageId = "m1" };
            service.Thumbnail = new MemoryStream(new byte[] { 1 });
            var file = Assert.IsType<FileStreamResult>(await controller.DownloadThumbnailAsync("a1", default));
            Assert.Equal("image/jpeg", file.ContentType);
            await file.FileStream.DisposeAsync();
        }

        [Fact]
        public async Task GetMessageAttachmentsAsync_MapsSuccessAndFailure()
        {
            var service = new FakeAttachments();
            Assert.IsType<OkObjectResult>((await CreateController(service).GetMessageAttachmentsAsync("m1", default)).Result);
            service.Error = new InvalidOperationException();
            Assert.Equal(500, Assert.IsType<ObjectResult>((await CreateController(service).GetMessageAttachmentsAsync("m1", default)).Result).StatusCode);
        }

        [Fact]
        public async Task UpdateAttachmentAsync_ValidatesAndMapsNotFoundAndSuccess()
        {
            var service = new FakeAttachments();
            var controller = CreateController(service);
            Assert.IsType<BadRequestObjectResult>((await controller.UpdateAttachmentAsync("a1", File("bad.exe", 1), default)).Result);
            Assert.IsType<NotFoundResult>((await controller.UpdateAttachmentAsync("a1", File("ok.txt", 1, new byte[] { 1 }), default)).Result);
            service.Result = new MessageAttachmentDto { Id = "a1", MessageId = "m1" };
            Assert.IsType<OkObjectResult>((await controller.UpdateAttachmentAsync("a1", File("ok.txt", 1, new byte[] { 1 }), default)).Result);
        }

        [Fact]
        public async Task DeleteAttachmentAsync_MapsFalseTrueAndException()
        {
            var service = new FakeAttachments();
            var controller = CreateController(service);
            Assert.IsType<NotFoundResult>(await controller.DeleteAttachmentAsync("a1", default));
            service.Result = new MessageAttachmentDto { Id = "a1", MessageId = "m1" };
            Assert.IsType<NotFoundObjectResult>(await controller.DeleteAttachmentAsync("a1", default));
            service.DeleteResult = true;
            Assert.IsType<NoContentResult>(await controller.DeleteAttachmentAsync("a1", default));
            service.Error = new InvalidOperationException();
            Assert.Equal(500, Assert.IsType<ObjectResult>(await controller.DeleteAttachmentAsync("a1", default)).StatusCode);
        }

        private static IFormFile File(string name, long length, byte[]? bytes = null)
            => new FormFile(new MemoryStream(bytes ?? Array.Empty<byte>()), 0, length, "file", name) { Headers = new HeaderDictionary(), ContentType = "application/octet-stream" };

        private static AttachmentsController CreateController(FakeAttachments service, string? userId = "user-1")
        {
            var controller = new AttachmentsController(
                service,
                new StubMessages(),
                new StubChats(),
                new NullConnections(),
                new LoggerConfiguration().CreateLogger())
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext(),
                },
            };

            if (userId != null)
            {
                controller.HttpContext.Items["UserId"] = userId;
            }

            return controller;
        }

        private sealed class FakeAttachments : IAttachmentService
        {
            public MessageAttachmentDto? Result { get; set; }
            public Exception? Error { get; set; }
            public Stream? Stream { get; set; }
            public Stream? Thumbnail { get; set; }
            public bool DeleteResult { get; set; }
            public string? SavedMessageId { get; private set; }
            private T Return<T>(T value) { if (this.Error != null) throw this.Error; return value; }
            public Task<MessageAttachmentDto> SaveAttachmentAsync(string messageId, string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default) { this.SavedMessageId = messageId; return Task.FromResult(this.Return(this.Result ?? new MessageAttachmentDto())); }
            public Task<MessageAttachmentDto?> GetAttachmentAsync(string attachmentId, CancellationToken cancellationToken = default) => Task.FromResult(this.Return(this.Result));
            public Task<Stream?> GetAttachmentStreamAsync(string attachmentId, CancellationToken cancellationToken = default) => Task.FromResult(this.Return(this.Stream));
            public Task<Stream?> GetThumbnailStreamAsync(string attachmentId, CancellationToken cancellationToken = default) => Task.FromResult(this.Return(this.Thumbnail));
            public Task<List<MessageAttachmentDto>> GetMessageAttachmentsAsync(string messageId, CancellationToken cancellationToken = default) => Task.FromResult(this.Return(new List<MessageAttachmentDto>()));
            public Task<MessageAttachmentDto> UpdateAttachmentAsync(string attachmentId, Stream fileStream, string contentType, CancellationToken cancellationToken = default) => Task.FromResult(this.Return(this.Result ?? new MessageAttachmentDto()));
            public Task<bool> DeleteAttachmentAsync(string attachmentId, CancellationToken cancellationToken = default) => Task.FromResult(this.Return(this.DeleteResult));
        }

        private sealed class StubMessages : IMessageService
        {
            public Task<MessageDto?> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default)
                => Task.FromResult<MessageDto?>(new MessageDto { Id = messageId, ChatId = "chat-1" });

            public Task<MessageDto> SendMessageAsync(SendMessageRequest request, string senderId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<MessageDto> ForwardMessageAsync(string targetChatId, string messageId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<MessageDto> EditMessageAsync(string messageId, string newContent, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<string> DeleteMessageAsync(string messageId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<IEnumerable<MessageDto>> GetChatMessagesAsync(string chatId, string userId, int limit, int offset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task MarkAsDeliveredAsync(string messageId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task MarkAsReadAsync(string messageId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<IEnumerable<MessageDto>> SearchMessagesAsync(string chatId, string query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<MessageDto> AddReactionAsync(string messageId, string emoji, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<MessageDto> RemoveReactionAsync(string messageId, string emoji, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class StubChats : IChatService
        {
            public Task<ChatDto?> GetChatByIdAsync(string chatId, string userId, CancellationToken cancellationToken = default)
                => Task.FromResult<ChatDto?>(new ChatDto { Id = chatId, ParticipantIds = new List<string> { userId } });

            public Task<IEnumerable<ChatDto>> GetUserChatsAsync(string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<ChatDto> CreateChatAsync(CreateChatRequest request, string creatorUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task DeleteChatAsync(string chatId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<ChatMembershipChangeResult> LeaveChatAsync(string chatId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<ChatMembershipChangeResult> AddParticipantsAsync(string chatId, string ownerUserId, IReadOnlyList<string> userIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<ChatMembershipChangeResult> RemoveParticipantAsync(string chatId, string ownerUserId, string targetUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<ChatDto> UpdateChatAsync(string chatId, string userId, UpdateChatRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<ChatDto> UploadChatAvatarAsync(string chatId, string userId, string fileName, Stream fileStream, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<ChatDto> SetChatPinnedAsync(string chatId, string userId, bool pinned, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class NullConnections : IWebSocketConnectionManager
        {
            public void AddConnection(string userId, WebSocket socket, string connectionId) { }
            public void RemoveConnection(string connectionId) { }
            public WebSocket? GetSocketByConnectionId(string connectionId) => null;
            public IEnumerable<string> GetConnectionIdsByUserId(string userId) => Array.Empty<string>();
            public string? GetUserIdByConnectionId(string connectionId) => null;
            public IEnumerable<string> GetConnectedUserIds() => Array.Empty<string>();
            public Task SendMessageAsync(string connectionId, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task BroadcastToUserAsync(string userId, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}
