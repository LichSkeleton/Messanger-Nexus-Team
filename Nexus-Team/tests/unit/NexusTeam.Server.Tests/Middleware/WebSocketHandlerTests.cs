namespace NexusTeam.Server.Tests.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using NexusTeam.Server.Middleware;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Models;
    using Serilog;
    using Xunit;

    public class WebSocketHandlerTests
    {
        [Fact]
        public async Task InvokeAsync_ForNormalHttpRequest_CallsNextMiddleware()
        {
            var called = false;
            var handler = new WebSocketHandler(_ => { called = true; return Task.CompletedTask; }, new Connections(), new Jwt(), new LoggerConfiguration().CreateLogger());

            await handler.InvokeAsync(new DefaultHttpContext());

            Assert.True(called);
        }

        [Fact]
        public async Task BroadcastAvatarUpdateAsync_DeduplicatesPartnersAndExcludesChangedUser()
        {
            var chats = new Chats();
            chats.Items.Add(new ChatDto { ParticipantIds = new List<string> { "owner", "p1", "p2" } });
            chats.Items.Add(new ChatDto { ParticipantIds = new List<string> { "owner", "p1" } });
            var connections = new Connections();

            await WebSocketHandler.BroadcastAvatarUpdateAsync("owner", "/avatar", chats, connections, new LoggerConfiguration().CreateLogger());

            Assert.Equal(2, connections.Users.Count);
            Assert.Contains("p1", connections.Users);
            Assert.Contains("p2", connections.Users);
            Assert.All(connections.Messages, message => Assert.Contains("\"type\":\"avatarUpdate\"", message));
        }

        [Fact]
        public async Task BroadcastAvatarUpdateAsync_WhenChatLookupFails_DoesNotPropagate()
        {
            var chats = new Chats { Error = new InvalidOperationException("failed") };
            await WebSocketHandler.BroadcastAvatarUpdateAsync("owner", null, chats, new Connections(), new LoggerConfiguration().CreateLogger());
        }

        private sealed class Jwt : IJwtTokenService
        {
            public Task<string> GenerateAccessTokenAsync(User user) => Task.FromResult("token");
            public Task<string?> ValidateTokenAsync(string token) => Task.FromResult<string?>(null);
        }

        private sealed class Chats : IChatService
        {
            public List<ChatDto> Items { get; } = new List<ChatDto>(); public Exception? Error { get; set; }
            public Task<IEnumerable<ChatDto>> GetUserChatsAsync(string userId, CancellationToken cancellationToken = default) => this.Error == null ? Task.FromResult<IEnumerable<ChatDto>>(this.Items) : Task.FromException<IEnumerable<ChatDto>>(this.Error);
            public Task<ChatDto?> GetChatByIdAsync(string chatId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<ChatDto> CreateChatAsync(CreateChatRequest request, string creatorUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task DeleteChatAsync(string chatId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task LeaveChatAsync(string chatId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<ChatDto> UpdateChatAsync(string chatId, string userId, UpdateChatRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<ChatDto> UploadChatAvatarAsync(string chatId, string userId, string fileName, System.IO.Stream fileStream, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class Connections : IWebSocketConnectionManager
        {
            public List<string> Users { get; } = new List<string>(); public List<string> Messages { get; } = new List<string>();
            public Task BroadcastToUserAsync(string userId, string message, CancellationToken cancellationToken = default) { this.Users.Add(userId); this.Messages.Add(message); return Task.CompletedTask; }
            public void AddConnection(string userId, WebSocket socket, string connectionId) { } public void RemoveConnection(string connectionId) { } public WebSocket? GetSocketByConnectionId(string connectionId) => null; public IEnumerable<string> GetConnectionIdsByUserId(string userId) => Array.Empty<string>(); public string? GetUserIdByConnectionId(string connectionId) => null; public Task SendMessageAsync(string connectionId, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}
