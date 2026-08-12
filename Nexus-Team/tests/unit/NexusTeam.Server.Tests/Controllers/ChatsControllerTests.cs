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
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using NexusTeam.Shared.Exceptions;
    using Serilog;
    using Xunit;

    public class ChatsControllerTests
    {
        [Fact]
        public async Task GetChats_RequiresUserAndReturnsServiceChats()
        {
            Assert.IsType<UnauthorizedResult>((await new Fixture().Controller.GetChats(default)).Result);
            Assert.IsType<OkObjectResult>((await new Fixture("user-1").Controller.GetChats(default)).Result);
        }

        [Fact]
        public async Task GetChat_MapsUnauthorizedMissingAndExisting()
        {
            Assert.IsType<UnauthorizedResult>((await new Fixture().Controller.GetChat("chat-1", default)).Result);
            var fixture = new Fixture("user-1");
            Assert.IsType<NotFoundResult>((await fixture.Controller.GetChat("chat-1", default)).Result);
            fixture.Chats.Result = Chat();
            Assert.IsType<OkObjectResult>((await fixture.Controller.GetChat("chat-1", default)).Result);
        }

        [Theory]
        [InlineData(0, -1, 50, 0)]
        [InlineData(101, 4, 50, 4)]
        [InlineData(20, 2, 20, 2)]
        public async Task GetChatMessages_NormalizesPagination(int limit, int offset, int expectedLimit, int expectedOffset)
        {
            Assert.IsType<UnauthorizedResult>((await new Fixture().Controller.GetChatMessages("chat-1", limit, offset)).Result);
            var fixture = new Fixture("user-1");
            Assert.IsType<OkObjectResult>((await fixture.Controller.GetChatMessages("chat-1", limit, offset)).Result);
            Assert.Equal((expectedLimit, expectedOffset), fixture.Messages.Page);
            Assert.Equal("user-1", fixture.Messages.UserId);
        }

        [Fact]
        public async Task SendMessage_RequiresUserSetsRouteChatAndReturnsCreated()
        {
            Assert.IsType<UnauthorizedResult>((await new Fixture().Controller.SendMessage("chat-1", new SendMessageRequest(), default)).Result);
            var fixture = new Fixture("user-1");
            var request = new SendMessageRequest();
            Assert.IsType<CreatedResult>((await fixture.Controller.SendMessage("chat-1", request, default)).Result);
            Assert.Equal("chat-1", request.ChatId);
            Assert.Equal("user-1", fixture.Messages.UserId);
        }

        [Fact]
        public async Task CreateChat_ValidatesRequestAuthenticationAndModel()
        {
            Assert.IsType<BadRequestObjectResult>((await new Fixture().Controller.CreateChat(null, default)).Result);
            Assert.IsType<UnauthorizedResult>((await new Fixture().Controller.CreateChat(new CreateChatRequest(), default)).Result);
            var fixture = new Fixture("user-1"); fixture.Controller.ModelState.AddModelError("name", "required");
            Assert.IsType<BadRequestObjectResult>((await fixture.Controller.CreateChat(new CreateChatRequest(), default)).Result);
        }

        [Theory]
        [InlineData("none", typeof(CreatedAtActionResult))]
        [InlineData("duplicate", typeof(ConflictObjectResult))]
        [InlineData("validation", typeof(BadRequestObjectResult))]
        public async Task CreateChat_MapsServiceOutcomes(string error, Type resultType)
        {
            var fixture = new Fixture("user-1"); fixture.Chats.Result = Chat(); fixture.Chats.Error = Error(error);
            Assert.IsType(resultType, (await fixture.Controller.CreateChat(new CreateChatRequest { Name = "Chat" }, default)).Result);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AddReaction_MapsSuccessAndValidation(bool fails)
        {
            var fixture = new Fixture("user-1"); fixture.Messages.Error = fails ? new ValidationException("bad") : null;
            var result = (await fixture.Controller.AddReaction("chat-1", "m1", new AddReactionRequest { Emoji = "👍" }, default)).Result;
            Assert.Equal(fails, result is BadRequestObjectResult);
            Assert.Equal(!fails, result is OkObjectResult);
        }

        [Fact]
        public async Task RemoveReaction_DecodesEmojiBeforeCallingService()
        {
            var fixture = new Fixture("user-1");
            Assert.IsType<OkObjectResult>((await fixture.Controller.RemoveReaction("chat-1", "m1", "%F0%9F%91%8D", default)).Result);
            Assert.Equal("👍", fixture.Messages.Emoji);
        }

        [Theory]
        [InlineData("none", typeof(NoContentResult))]
        [InlineData("notfound", typeof(NotFoundResult))]
        [InlineData("unauthorized", typeof(UnauthorizedResult))]
        [InlineData("domain", typeof(ObjectResult))]
        public async Task DeleteChat_MapsServiceOutcomes(string error, Type resultType)
        {
            var fixture = new Fixture("user-1"); fixture.Chats.Error = Error(error);
            Assert.IsType(resultType, await fixture.Controller.DeleteChat("chat-1", default));
        }

        [Theory]
        [InlineData("none", typeof(NoContentResult))]
        [InlineData("notfound", typeof(NotFoundResult))]
        [InlineData("unauthorized", typeof(UnauthorizedObjectResult))]
        [InlineData("validation", typeof(BadRequestObjectResult))]
        [InlineData("domain", typeof(ObjectResult))]
        public async Task LeaveChat_MapsServiceOutcomes(string error, Type resultType)
        {
            var fixture = new Fixture("user-1"); fixture.Chats.Error = Error(error);
            Assert.IsType(resultType, await fixture.Controller.LeaveChat("chat-1", default));
        }

        [Theory]
        [InlineData("none", typeof(OkObjectResult))]
        [InlineData("notfound", typeof(NotFoundResult))]
        [InlineData("unauthorized", typeof(UnauthorizedObjectResult))]
        [InlineData("validation", typeof(BadRequestObjectResult))]
        [InlineData("domain", typeof(ObjectResult))]
        public async Task UpdateChat_MapsServiceOutcomes(string error, Type resultType)
        {
            var fixture = new Fixture("user-1"); fixture.Chats.Result = Chat(); fixture.Chats.Error = Error(error);
            Assert.IsType(resultType, (await fixture.Controller.UpdateChat("chat-1", new UpdateChatRequest(), default)).Result);
        }

        [Fact]
        public async Task UploadChatAvatar_ValidatesFileAndReturnsUpdatedChat()
        {
            var fixture = new Fixture("user-1"); fixture.Chats.Result = Chat();
            Assert.IsType<BadRequestObjectResult>((await fixture.Controller.UploadChatAvatar("chat-1", File(0), default)).Result);
            Assert.Equal(413, Assert.IsType<ObjectResult>((await fixture.Controller.UploadChatAvatar("chat-1", File(6L * 1024 * 1024), default)).Result).StatusCode);
            Assert.IsType<OkObjectResult>((await fixture.Controller.UploadChatAvatar("chat-1", File(1, new byte[] { 1 }), default)).Result);
        }

        private static ChatDto Chat() => new ChatDto { Id = "chat-1", ParticipantIds = new List<string> { "user-1" } };
        private static Exception? Error(string kind) => kind switch { "duplicate" => new DuplicateChatException("Chat"), "validation" => new ValidationException("bad"), "notfound" => new NotFoundException("missing"), "unauthorized" => new UnauthorizedException("forbidden"), "domain" => new DomainException("failed"), _ => null };
        private static IFormFile File(long length, byte[]? data = null) => new FormFile(new MemoryStream(data ?? Array.Empty<byte>()), 0, length, "file", "avatar.png");

        private sealed class Fixture
        {
            public Fixture(string? userId = null)
            {
                this.Controller = new ChatsController(this.Chats, this.Messages, new Connections(), new Status(), new LoggerConfiguration().CreateLogger()) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
                if (userId != null) this.Controller.HttpContext.Items["UserId"] = userId;
            }
            public Chats Chats { get; } = new Chats(); public Messages Messages { get; } = new Messages(); public ChatsController Controller { get; }
        }

        private sealed class Chats : IChatService
        {
            public ChatDto? Result { get; set; } public Exception? Error { get; set; }
            private Task<T> Return<T>(T value) => this.Error == null ? Task.FromResult(value) : Task.FromException<T>(this.Error);
            private Task Return() => this.Error == null ? Task.CompletedTask : Task.FromException(this.Error);
            public Task<IEnumerable<ChatDto>> GetUserChatsAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ChatDto>>(Array.Empty<ChatDto>()); public Task<ChatDto?> GetChatByIdAsync(string chatId, string userId, CancellationToken cancellationToken = default) => Task.FromResult(this.Result); public Task<ChatDto> CreateChatAsync(CreateChatRequest request, string creatorUserId, CancellationToken cancellationToken = default) => this.Return(this.Result ?? new ChatDto()); public Task DeleteChatAsync(string chatId, string userId, CancellationToken cancellationToken = default) => this.Return(); public Task LeaveChatAsync(string chatId, string userId, CancellationToken cancellationToken = default) => this.Return(); public Task<ChatDto> UpdateChatAsync(string chatId, string userId, UpdateChatRequest request, CancellationToken cancellationToken = default) => this.Return(this.Result ?? new ChatDto()); public Task<ChatDto> UploadChatAvatarAsync(string chatId, string userId, string fileName, Stream fileStream, CancellationToken cancellationToken = default) => this.Return(this.Result ?? new ChatDto());
        }

        private sealed class Messages : IMessageService
        {
            public (int, int)? Page { get; private set; } public string? UserId { get; private set; } public string? Emoji { get; private set; } public ValidationException? Error { get; set; }
            private Task<MessageDto> Result() => this.Error == null ? Task.FromResult(new MessageDto { Id = "m1" }) : Task.FromException<MessageDto>(this.Error);
            public Task<MessageDto> SendMessageAsync(SendMessageRequest request, string senderId, CancellationToken cancellationToken = default) { this.UserId = senderId; return this.Result(); } public Task<IEnumerable<MessageDto>> GetChatMessagesAsync(string chatId, string userId, int limit, int offset, CancellationToken cancellationToken = default) { this.Page = (limit, offset); this.UserId = userId; return Task.FromResult<IEnumerable<MessageDto>>(Array.Empty<MessageDto>()); } public Task<MessageDto> AddReactionAsync(string messageId, string emoji, string userId, CancellationToken cancellationToken = default) { this.Emoji = emoji; return this.Result(); } public Task<MessageDto> RemoveReactionAsync(string messageId, string emoji, string userId, CancellationToken cancellationToken = default) { this.Emoji = emoji; return this.Result(); }
            public Task<MessageDto> EditMessageAsync(string messageId, string newContent, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<string> DeleteMessageAsync(string messageId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task MarkAsDeliveredAsync(string messageId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task MarkAsReadAsync(string messageId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<IEnumerable<MessageDto>> SearchMessagesAsync(string chatId, string query, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<MessageDto?> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class Status : IUserStatusService
        {
            public Task<UserStatus> GetStatusAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(UserStatus.Offline); public Task<UserStatus> GetPublicStatusAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(UserStatus.Offline); public Task SetStatusAsync(string userId, UserStatus status, CancellationToken cancellationToken = default) => Task.CompletedTask; public Task<bool> GetInvisiblePreferenceAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(false); public Task SetInvisiblePreferenceAsync(string userId, bool isInvisible, CancellationToken cancellationToken = default) => Task.CompletedTask; public Task RemoveStatusAsync(string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private sealed class Connections : IWebSocketConnectionManager
        {
            public void AddConnection(string userId, WebSocket socket, string connectionId) { } public void RemoveConnection(string connectionId) { } public WebSocket? GetSocketByConnectionId(string connectionId) => null; public IEnumerable<string> GetConnectionIdsByUserId(string userId) => Array.Empty<string>(); public string? GetUserIdByConnectionId(string connectionId) => null; public IEnumerable<string> GetConnectedUserIds() => Array.Empty<string>(); public Task SendMessageAsync(string connectionId, string message, CancellationToken cancellationToken = default) => Task.CompletedTask; public Task BroadcastToUserAsync(string userId, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}
