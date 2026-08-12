namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using NexusTeam.Shared.Exceptions;
    using NexusTeam.Shared.Models;
    using Serilog;
    using Xunit;

    public class MessageServiceTests
    {
        [Fact]
        public async Task SendMessageAsync_WhenChatMissing_Throws()
        {
            var fixture = new Fixture();

            await Assert.ThrowsAsync<ValidationException>(() =>
                fixture.Service.SendMessageAsync(Request(), "sender"));
        }

        [Fact]
        public async Task SendMessageAsync_WhenSenderIsNotParticipant_Throws()
        {
            var fixture = new Fixture();
            fixture.Chats.ById = Chat("other");

            await Assert.ThrowsAsync<ValidationException>(() =>
                fixture.Service.SendMessageAsync(Request(), "sender"));
        }

        [Fact]
        public async Task SendMessageAsync_CreatesMessageUpdatesChatLoadsExistingAttachmentsAndInvalidatesCache()
        {
            var fixture = new Fixture();
            fixture.Chats.ById = Chat("sender");
            fixture.Attachments.ById["a1"] = Attachment("a1", thumbnail: "thumb.jpg");

            var result = await fixture.Service.SendMessageAsync(
                Request(attachmentIds: new List<string> { "a1", "missing" }), "sender");

            Assert.Equal("message-1", result.Id);
            Assert.Equal("hello", result.Content);
            Assert.Equal(fixture.Now, result.CreatedAt);
            Assert.Single(result.Attachments);
            Assert.Equal("/api/attachments/download/a1", result.Attachments[0].DownloadUrl);
            Assert.Equal("/api/attachments/thumbnail/a1", result.Attachments[0].ThumbnailUrl);
            Assert.Equal(fixture.Now, fixture.Chats.ById.LastMessageAt);
            Assert.Same(fixture.Chats.ById, fixture.Chats.Updated);
            Assert.Equal(CacheKeys(), fixture.Cache.RemovedKeys);
        }

        [Theory]
        [InlineData(null, "user")]
        [InlineData("other", "user")]
        public async Task EditMessageAsync_WhenMissingOrNotSender_Throws(string? senderId, string userId)
        {
            var fixture = new Fixture();
            if (senderId != null)
            {
                fixture.Messages.ById = Message(senderId: senderId);
            }

            await Assert.ThrowsAsync<ValidationException>(() =>
                fixture.Service.EditMessageAsync("message-1", "new", userId));
        }

        [Fact]
        public async Task EditMessageAsync_WhenDeleted_Throws()
        {
            var fixture = new Fixture();
            fixture.Messages.ById = Message(isDeleted: true);

            await Assert.ThrowsAsync<ValidationException>(() =>
                fixture.Service.EditMessageAsync("message-1", "new", "sender"));
        }

        [Fact]
        public async Task EditMessageAsync_UpdatesContentTimestampAndCache()
        {
            var fixture = new Fixture();
            fixture.Messages.ById = Message();

            var result = await fixture.Service.EditMessageAsync("message-1", "new", "sender");

            Assert.Equal("new", result.Content);
            Assert.Equal(fixture.Now, result.EditedAt);
            Assert.Same(fixture.Messages.ById, fixture.Messages.Updated);
            Assert.Equal(CacheKeys(), fixture.Cache.RemovedKeys);
        }

        [Fact]
        public async Task DeleteMessageAsync_WhenValid_DeletesAndReturnsChatId()
        {
            var fixture = new Fixture();
            fixture.Messages.ById = Message();

            var result = await fixture.Service.DeleteMessageAsync("message-1", "sender");

            Assert.Equal("chat-1", result);
            Assert.Equal("message-1", fixture.Messages.DeletedId);
            Assert.Equal(CacheKeys(), fixture.Cache.RemovedKeys);
        }

        [Theory]
        [InlineData(false, "sender")]
        [InlineData(true, "other")]
        public async Task DeleteMessageAsync_WhenMissingOrNotSender_Throws(bool exists, string userId)
        {
            var fixture = new Fixture();
            fixture.Messages.ById = exists ? Message() : null;

            await Assert.ThrowsAsync<ValidationException>(() =>
                fixture.Service.DeleteMessageAsync("message-1", userId));
        }

        [Fact]
        public async Task DeleteMessageAsync_WhenAlreadyDeleted_Throws()
        {
            var fixture = new Fixture();
            fixture.Messages.ById = Message(isDeleted: true);

            await Assert.ThrowsAsync<ValidationException>(() =>
                fixture.Service.DeleteMessageAsync("message-1", "sender"));
        }

        [Fact]
        public async Task GetChatMessagesAsync_OnCacheHit_ReturnsRequestedSliceWithoutRepositoryCall()
        {
            var fixture = new Fixture();
            fixture.Cache.Value = new List<MessageDto>
            {
                new MessageDto { Id = "m1" }, new MessageDto { Id = "m2" }, new MessageDto { Id = "m3" },
            };
            fixture.Chats.ById = Chat("user-1");

            var result = (await fixture.Service.GetChatMessagesAsync("chat-1", "user-1", 1, 1)).ToList();

            Assert.Equal("m2", Assert.Single(result).Id);
            Assert.Null(fixture.Messages.LastChatQuery);
        }

        [Fact]
        public async Task GetChatMessagesAsync_OnCacheMiss_LoadsAttachmentsMapsAndCachesForFiveMinutes()
        {
            var fixture = new Fixture();
            fixture.Chats.ById = Chat("user-1");
            fixture.Messages.ChatMessages.Add(Message());
            fixture.Attachments.ByMessage["message-1"] = new List<MessageAttachment> { Attachment("a1") };

            var result = (await fixture.Service.GetChatMessagesAsync("chat-1", "user-1", 20, 5)).ToList();

            Assert.Single(result);
            Assert.Single(result[0].Attachments);
            Assert.Equal(("chat-1", 20, 5), fixture.Messages.LastChatQuery);
            Assert.Equal("chat:messages:chat-1:20:5", fixture.Cache.SetKey);
            Assert.Equal(TimeSpan.FromMinutes(5), fixture.Cache.Expiration);
        }

        [Fact]
        public async Task SearchMessagesAsync_WithBlankQuery_ReturnsEmptyWithoutRepositoryCall()
        {
            var fixture = new Fixture();

            var result = await fixture.Service.SearchMessagesAsync("chat-1", "  ");

            Assert.Empty(result);
            Assert.Null(fixture.Messages.LastSearch);
        }

        [Fact]
        public async Task SearchMessagesAsync_LoadsAttachmentsAndMapsResults()
        {
            var fixture = new Fixture();
            fixture.Messages.SearchResults.Add(Message());
            fixture.Attachments.ByMessage["message-1"] = new List<MessageAttachment> { Attachment("a1") };

            var result = (await fixture.Service.SearchMessagesAsync("chat-1", "hello")).ToList();

            Assert.Single(result[0].Attachments);
            Assert.Equal(("chat-1", "hello"), fixture.Messages.LastSearch);
        }

        [Theory]
        [InlineData(MessageStatus.Sent, true)]
        [InlineData(MessageStatus.Delivered, false)]
        [InlineData(MessageStatus.Read, false)]
        public async Task MarkAsDeliveredAsync_OnlyTransitionsSentMessages(MessageStatus initial, bool updates)
        {
            var fixture = new Fixture();
            fixture.Messages.ById = Message(status: initial);

            await fixture.Service.MarkAsDeliveredAsync("message-1");

            Assert.Equal(updates ? MessageStatus.Delivered : initial, fixture.Messages.ById.Status);
            Assert.Equal(updates, fixture.Messages.Updated != null);
        }

        [Theory]
        [InlineData(MessageStatus.Sent, true)]
        [InlineData(MessageStatus.Delivered, true)]
        [InlineData(MessageStatus.Read, false)]
        public async Task MarkAsReadAsync_TransitionsAnyUnreadMessage(MessageStatus initial, bool updates)
        {
            var fixture = new Fixture();
            fixture.Messages.ById = Message(status: initial);

            await fixture.Service.MarkAsReadAsync("message-1");

            Assert.Equal(MessageStatus.Read, fixture.Messages.ById.Status);
            Assert.Equal(updates, fixture.Messages.Updated != null);
        }

        [Fact]
        public async Task AddReactionAsync_InitializesAndDoesNotDuplicateReaction()
        {
            var fixture = new Fixture();
            fixture.Messages.ById = Message();
            fixture.Chats.ById = Chat("user-1");

            await fixture.Service.AddReactionAsync("message-1", "👍", "user-1");
            fixture.Messages.Updated = null;
            fixture.Cache.RemovedKeys.Clear();
            var result = await fixture.Service.AddReactionAsync("message-1", "👍", "user-1");

            Assert.Equal(new[] { "user-1" }, result.Reactions["👍"]);
            Assert.Null(fixture.Messages.Updated);
            Assert.Empty(fixture.Cache.RemovedKeys);
        }

        [Fact]
        public async Task RemoveReactionAsync_RemovesEmptyEmojiEntryAndInvalidatesCache()
        {
            var fixture = new Fixture();
            fixture.Messages.ById = Message();
            fixture.Messages.ById.Reactions["👍"] = new List<string> { "user-1" };
            fixture.Chats.ById = Chat("user-1");

            var result = await fixture.Service.RemoveReactionAsync("message-1", "👍", "user-1");

            Assert.DoesNotContain("👍", result.Reactions.Keys);
            Assert.NotNull(fixture.Messages.Updated);
            Assert.Equal(CacheKeys(), fixture.Cache.RemovedKeys);
        }

        [Fact]
        public async Task GetMessageByIdAsync_WhenFound_LoadsAttachmentsAndMapsUrls()
        {
            var fixture = new Fixture();
            fixture.Messages.ById = Message();
            fixture.Attachments.ByMessage["message-1"] = new List<MessageAttachment> { Attachment("a1") };

            var result = await fixture.Service.GetMessageByIdAsync("message-1");

            Assert.NotNull(result);
            Assert.Equal("/api/attachments/download/a1", Assert.Single(result.Attachments).DownloadUrl);
        }

        private static SendMessageRequest Request(List<string>? attachmentIds = null) => new SendMessageRequest
        {
            ChatId = "chat-1", Content = "hello", ReplyToId = "parent", AttachmentIds = attachmentIds ?? new List<string>(),
        };

        private static Chat Chat(params string[] users) => new Chat
        {
            Id = "chat-1", ParticipantIds = users.ToList(), CreatedBy = users.FirstOrDefault() ?? string.Empty,
        };

        private static Message Message(
            string senderId = "sender", bool isDeleted = false, MessageStatus status = MessageStatus.Sent) => new Message
        {
            Id = "message-1", ChatId = "chat-1", SenderId = senderId, Content = "old", IsDeleted = isDeleted, Status = status,
        };

        private static MessageAttachment Attachment(string id, string? thumbnail = null) => new MessageAttachment
        {
            Id = id, MessageId = "message-1", FileName = "file.png", ContentType = "image/png", ThumbnailPath = thumbnail,
        };

        private static string[] CacheKeys() => new[]
        {
            "chat:messages:chat-1:50:0", "chat:messages:chat-1:1:0", "chat:messages:chat-1:20:0",
        };

        private sealed class Fixture
        {
            public readonly DateTime Now = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

            public Fixture()
            {
                this.Service = new MessageService(
                    this.Messages, this.Chats, this.Attachments, this.Cache,
                    new FixedId(), new FixedClock(this.Now), new LoggerConfiguration().CreateLogger());
            }

            public FakeMessageRepository Messages { get; } = new FakeMessageRepository();

            public FakeChatRepository Chats { get; } = new FakeChatRepository();

            public FakeAttachmentRepository Attachments { get; } = new FakeAttachmentRepository();

            public FakeCache Cache { get; } = new FakeCache();

            public MessageService Service { get; }
        }

        private sealed class FixedId : IIdGenerator { public string GenerateId() => "message-1"; }

        private sealed class FixedClock : IClock
        {
            public FixedClock(DateTime now) => this.UtcNow = now;

            public DateTime UtcNow { get; }
        }

        private sealed class FakeMessageRepository : IMessageRepository
        {
            public Message? ById { get; set; }
            public Message? Updated { get; set; }
            public string? DeletedId { get; private set; }
            public List<Message> ChatMessages { get; } = new List<Message>();
            public List<Message> SearchResults { get; } = new List<Message>();
            public (string, int, int)? LastChatQuery { get; private set; }
            public (string, string)? LastSearch { get; private set; }
            public Task<Message?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(this.ById);
            public Task<IEnumerable<Message>> GetByChatIdAsync(string chatId, int limit = 50, int offset = 0, CancellationToken cancellationToken = default) { this.LastChatQuery = (chatId, limit, offset); return Task.FromResult<IEnumerable<Message>>(this.ChatMessages); }
            public Task CreateAsync(Message message, CancellationToken cancellationToken = default) { this.ById = message; return Task.CompletedTask; }
            public Task UpdateAsync(Message message, CancellationToken cancellationToken = default) { this.Updated = message; return Task.CompletedTask; }
            public Task DeleteAsync(string id, CancellationToken cancellationToken = default) { this.DeletedId = id; return Task.CompletedTask; }
            public Task<int> GetMessageCountAsync(string chatId, CancellationToken cancellationToken = default) => Task.FromResult(this.ChatMessages.Count);
            public Task<IEnumerable<Message>> SearchAsync(string chatId, string query, CancellationToken cancellationToken = default) { this.LastSearch = (chatId, query); return Task.FromResult<IEnumerable<Message>>(this.SearchResults); }
            public Task<List<string>> DeleteByChatIdAsync(string chatId, CancellationToken cancellationToken = default) => Task.FromResult(new List<string>());
        }

        private sealed class FakeChatRepository : IChatRepository
        {
            public Chat? ById { get; set; }
            public Chat? Updated { get; private set; }
            public Task<Chat?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(this.ById);
            public Task<IEnumerable<Chat>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Chat>>(Array.Empty<Chat>());
            public Task CreateAsync(Chat chat, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task UpdateAsync(Chat chat, CancellationToken cancellationToken = default) { this.Updated = chat; return Task.CompletedTask; }
            public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task AddParticipantAsync(string chatId, string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RemoveParticipantAsync(string chatId, string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<bool> ChatNameExistsForUserAsync(string name, string userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        }

        private sealed class FakeAttachmentRepository : IMessageAttachmentRepository
        {
            public Dictionary<string, MessageAttachment> ById { get; } = new Dictionary<string, MessageAttachment>();
            public Dictionary<string, List<MessageAttachment>> ByMessage { get; } = new Dictionary<string, List<MessageAttachment>>();
            public Task AddAsync(MessageAttachment attachment, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<MessageAttachment?> GetByIdAsync(string attachmentId, CancellationToken cancellationToken = default) => Task.FromResult(this.ById.GetValueOrDefault(attachmentId));
            public Task<List<MessageAttachment>> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default) => Task.FromResult(this.ByMessage.GetValueOrDefault(messageId) ?? new List<MessageAttachment>());
            public Task UpdateAsync(MessageAttachment attachment, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task DeleteAsync(string attachmentId, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<List<MessageAttachment>> GetByMessageIdsAsync(List<string> messageIds, CancellationToken cancellationToken = default) => Task.FromResult(new List<MessageAttachment>());
            public Task<List<string>> DeleteByMessageIdsAsync(List<string> messageIds, CancellationToken cancellationToken = default) => Task.FromResult(new List<string>());
        }

        private sealed class FakeCache : ICacheService
        {
            public object? Value { get; set; }
            public List<string> RemovedKeys { get; } = new List<string>();
            public string? SetKey { get; private set; }
            public TimeSpan? Expiration { get; private set; }
            public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult((T?)this.Value);
            public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) { this.SetKey = key; this.Expiration = expiration; return Task.CompletedTask; }
            public Task RemoveAsync(string key, CancellationToken cancellationToken = default) { this.RemovedKeys.Add(key); return Task.CompletedTask; }
            public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(false);
        }
    }
}
