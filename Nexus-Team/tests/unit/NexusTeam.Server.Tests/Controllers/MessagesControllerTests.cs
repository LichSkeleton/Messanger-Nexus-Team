namespace NexusTeam.Server.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Controllers;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using Xunit;

    public class MessagesControllerTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SearchMessages_WithBlankQuery_ReturnsBadRequest(string query)
        {
            var service = new FakeService();
            var result = await new MessagesController(service).SearchMessages("chat-1", query, default);
            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Null(service.SearchCall);
        }

        [Fact]
        public async Task SearchMessages_ForwardsChatAndQueryAndReturnsOk()
        {
            var service = new FakeService();
            var result = await new MessagesController(service).SearchMessages("chat-1", "hello", default);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(("chat-1", "hello"), service.SearchCall);
        }

        private sealed class FakeService : IMessageService
        {
            public (string, string)? SearchCall { get; private set; }
            public Task<MessageDto> SendMessageAsync(SendMessageRequest request, string senderId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<MessageDto> EditMessageAsync(string messageId, string newContent, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<string> DeleteMessageAsync(string messageId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<IEnumerable<MessageDto>> GetChatMessagesAsync(string chatId, string userId, int limit, int offset, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task MarkAsDeliveredAsync(string messageId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task MarkAsReadAsync(string messageId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<IEnumerable<MessageDto>> SearchMessagesAsync(string chatId, string query, CancellationToken cancellationToken = default) { this.SearchCall = (chatId, query); return Task.FromResult<IEnumerable<MessageDto>>(Array.Empty<MessageDto>()); }
            public Task<MessageDto> AddReactionAsync(string messageId, string emoji, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<MessageDto> RemoveReactionAsync(string messageId, string emoji, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<MessageDto?> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }
    }
}
