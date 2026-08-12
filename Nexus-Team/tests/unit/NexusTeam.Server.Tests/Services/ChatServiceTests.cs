namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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

    public class ChatServiceTests
    {
        [Fact]
        public async Task GetChatByIdAsync_WhenMissing_ReturnsNull()
        {
            var fixture = new Fixture();
            Assert.Null(await fixture.Service.GetChatByIdAsync("missing", "user-1"));
        }

        [Fact]
        public async Task GetChatByIdAsync_ForDirectMessage_UsesOtherUsersDisplayNameAndPublicStatus()
        {
            var fixture = new Fixture();
            fixture.Chats.ById = Chat(ChatType.DirectMessage, "user-1", "user-2");
            fixture.Users.Users["user-1"] = User("user-1", "one", null);
            fixture.Users.Users["user-2"] = User("user-2", "two", "Other Person");
            fixture.Status.Statuses["user-2"] = UserStatus.Away;

            var result = await fixture.Service.GetChatByIdAsync("chat-1", "user-1");

            Assert.NotNull(result);
            Assert.Equal("Other Person", result.Name);
            Assert.Equal(UserStatus.Away, result.Participants.Single(x => x.Id == "user-2").Status);
        }

        [Fact]
        public async Task GetUserChatsAsync_ForDirectMessage_FallsBackToUsername()
        {
            var fixture = new Fixture();
            fixture.Chats.UserChats.Add(Chat(ChatType.DirectMessage, "user-1", "user-2"));
            fixture.Users.Users["user-2"] = User("user-2", "fallback", "  ");

            var result = (await fixture.Service.GetUserChatsAsync("user-1")).ToList();

            Assert.Equal("fallback", Assert.Single(result).Name);
        }

        [Fact]
        public async Task CreateChatAsync_WhenNameExists_ThrowsDuplicate()
        {
            var fixture = new Fixture();
            fixture.Chats.NameExists = true;

            await Assert.ThrowsAsync<DuplicateChatException>(() =>
                fixture.Service.CreateChatAsync(CreateRequest("user-2"), "user-1"));
        }

        [Fact]
        public async Task CreateChatAsync_WhenParticipantMissing_ThrowsValidation()
        {
            var fixture = new Fixture();
            fixture.Users.Users["user-1"] = User("user-1", "one");

            await Assert.ThrowsAsync<ValidationException>(() =>
                fixture.Service.CreateChatAsync(CreateRequest("user-2"), "user-1"));
        }

        [Fact]
        public async Task CreateChatAsync_DeduplicatesParticipantsAddsCreatorAndMapsUsers()
        {
            var fixture = new Fixture();
            fixture.Users.Users["user-1"] = User("user-1", "one");
            fixture.Users.Users["user-2"] = User("user-2", "two");
            var request = CreateRequest("user-2", "user-2");

            var result = await fixture.Service.CreateChatAsync(request, "user-1");

            Assert.Equal("chat-1", result.Id);
            Assert.Equal(new[] { "user-2", "user-1" }, result.ParticipantIds);
            Assert.Equal(2, result.Participants.Count);
            Assert.Equal(fixture.Now, result.CreatedAt);
            Assert.Same(fixture.Chats.Created, fixture.Chats.ById);
        }

        [Theory]
        [InlineData(false, ChatType.Group, "user-1", typeof(NotFoundException))]
        [InlineData(true, ChatType.DirectMessage, "user-1", typeof(ValidationException))]
        [InlineData(true, ChatType.Group, "outsider", typeof(UnauthorizedException))]
        public async Task LeaveChatAsync_RejectsInvalidRequests(bool exists, ChatType type, string userId, Type exceptionType)
        {
            var fixture = new Fixture();
            fixture.Chats.ById = exists ? Chat(type, "user-1", "user-2") : null;

            await Assert.ThrowsAsync(exceptionType, () => fixture.Service.LeaveChatAsync("chat-1", userId));
        }

        [Fact]
        public async Task LeaveChatAsync_WhenOwnerLeaves_TransfersOwnershipAndRemovesFolderEntry()
        {
            var fixture = new Fixture();
            fixture.Chats.ById = Chat(ChatType.Group, "owner", "member");
            fixture.Chats.ById.CreatedBy = "owner";

            await fixture.Service.LeaveChatAsync("chat-1", "owner");

            Assert.Equal("member", fixture.Chats.ById.CreatedBy);
            Assert.Equal(("chat-1", "owner"), fixture.Folders.RemovedFromUser);
            Assert.Equal(fixture.Now, fixture.Chats.ById.UpdatedAt);
        }

        [Fact]
        public async Task LeaveChatAsync_WhenLastParticipant_DeletesEntireChat()
        {
            var fixture = new Fixture();
            fixture.Chats.ById = Chat(ChatType.Group, "owner");

            await fixture.Service.LeaveChatAsync("chat-1", "owner");

            Assert.Equal("chat-1", fixture.Chats.DeletedId);
            Assert.Equal("chat-1", fixture.Folders.RemovedFromAll);
        }

        [Theory]
        [InlineData(false, ChatType.Group, "owner", typeof(NotFoundException))]
        [InlineData(true, ChatType.DirectMessage, "owner", typeof(ValidationException))]
        [InlineData(true, ChatType.Group, "outsider", typeof(UnauthorizedException))]
        [InlineData(true, ChatType.Group, "member", typeof(UnauthorizedException))]
        public async Task UpdateChatAsync_RejectsInvalidRequests(bool exists, ChatType type, string userId, Type exceptionType)
        {
            var fixture = new Fixture();
            fixture.Chats.ById = exists ? Chat(type, "owner", "member") : null;
            if (fixture.Chats.ById != null)
            {
                fixture.Chats.ById.CreatedBy = "owner";
            }

            await Assert.ThrowsAsync(exceptionType, () =>
                fixture.Service.UpdateChatAsync("chat-1", userId, new UpdateChatRequest()));
        }

        [Fact]
        public async Task UpdateChatAsync_TrimsFieldsClearsWhitespaceAvatarAndUpdatesTimestamp()
        {
            var fixture = new Fixture();
            fixture.Chats.ById = Chat(ChatType.Group, "owner");
            fixture.Chats.ById.CreatedBy = "owner";
            fixture.Users.Users["owner"] = User("owner", "owner");

            var result = await fixture.Service.UpdateChatAsync("chat-1", "owner", new UpdateChatRequest
            {
                Name = "  New Name  ", Description = "  New Description  ", AvatarUrl = "  ",
            });

            Assert.Equal("New Name", result.Name);
            Assert.Equal("New Description", result.Description);
            Assert.Null(result.AvatarUrl);
            Assert.Equal(fixture.Now, fixture.Chats.ById.UpdatedAt);
        }

        [Fact]
        public async Task UploadChatAvatarAsync_AsOwner_SavesWithChatKeyAndUpdatesChat()
        {
            var fixture = new Fixture();
            fixture.Chats.ById = Chat(ChatType.Group, "owner");
            fixture.Chats.ById.CreatedBy = "owner";
            fixture.Users.Users["owner"] = User("owner", "owner");
            await using var stream = new MemoryStream(new byte[] { 1 });

            var result = await fixture.Service.UploadChatAvatarAsync("chat-1", "owner", "photo.png", stream);

            Assert.Equal("chat_chat-1", fixture.Avatar.LastKey);
            Assert.Equal("/avatar/chat_chat-1", result.AvatarUrl);
        }

        [Theory]
        [InlineData(false, "owner", typeof(NotFoundException))]
        [InlineData(true, "outsider", typeof(UnauthorizedException))]
        [InlineData(true, "member", typeof(UnauthorizedException))]
        public async Task DeleteChatAsync_RejectsMissingNonParticipantOrNonOwner(bool exists, string userId, Type exceptionType)
        {
            var fixture = new Fixture();
            fixture.Chats.ById = exists ? Chat(ChatType.Group, "owner", "member") : null;
            if (fixture.Chats.ById != null)
            {
                fixture.Chats.ById.CreatedBy = "owner";
            }

            await Assert.ThrowsAsync(exceptionType, () => fixture.Service.DeleteChatAsync("chat-1", userId));
        }

        [Fact]
        public async Task DeleteChatAsync_CascadesMessagesAttachmentsFoldersAndChat()
        {
            var fixture = new Fixture();
            fixture.Chats.ById = Chat(ChatType.Group, "owner");
            fixture.Messages.DeletedMessageIds.AddRange(new[] { "m1", "m2" });
            fixture.Attachments.DeletedAttachmentIds.Add("a1");

            await fixture.Service.DeleteChatAsync("chat-1", "owner");

            Assert.Equal(new[] { "m1", "m2" }, fixture.Attachments.LastMessageIds);
            Assert.Equal("chat-1", fixture.Folders.RemovedFromAll);
            Assert.Equal("chat-1", fixture.Chats.DeletedId);
        }

        [Fact]
        public async Task DeleteChatAsync_WhenCascadeFails_WrapsAsDomainException()
        {
            var fixture = new Fixture();
            fixture.Chats.ById = Chat(ChatType.Group, "owner");
            fixture.Messages.DeleteError = new InvalidOperationException("database down");

            var error = await Assert.ThrowsAsync<DomainException>(() => fixture.Service.DeleteChatAsync("chat-1", "owner"));

            Assert.Contains("database down", error.Message);
        }

        private static CreateChatRequest CreateRequest(params string[] participants) => new CreateChatRequest
        {
            Name = "Group", Description = "Description", Type = ChatType.Group, ParticipantIds = participants.ToList(),
        };

        private static Chat Chat(ChatType type, params string[] users) => new Chat
        {
            Id = "chat-1", Type = type, Name = "Original", ParticipantIds = users.ToList(), CreatedBy = users.FirstOrDefault() ?? string.Empty,
        };

        private static User User(string id, string username, string? displayName = null) => new User
        {
            Id = id, Username = username, Email = username + "@example.com", DisplayName = displayName ?? string.Empty,
        };

        private sealed class Fixture
        {
            public readonly DateTime Now = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

            public Fixture()
            {
                this.Service = new ChatService(
                    this.Chats, this.Users, this.Messages, this.Attachments, this.Folders,
                    new FixedId(), new FixedClock(this.Now), new LoggerConfiguration().CreateLogger(), this.Status, this.Avatar);
            }

            public FakeChatRepository Chats { get; } = new FakeChatRepository();
            public FakeUserRepository Users { get; } = new FakeUserRepository();
            public FakeMessageRepository Messages { get; } = new FakeMessageRepository();
            public FakeAttachmentRepository Attachments { get; } = new FakeAttachmentRepository();
            public FakeFolderRepository Folders { get; } = new FakeFolderRepository();
            public FakeStatus Status { get; } = new FakeStatus();
            public FakeAvatar Avatar { get; } = new FakeAvatar();
            public ChatService Service { get; }
        }

        private sealed class FixedId : IIdGenerator { public string GenerateId() => "chat-1"; }
        private sealed class FixedClock : IClock { public FixedClock(DateTime now) => this.UtcNow = now; public DateTime UtcNow { get; } }

        private sealed class FakeChatRepository : IChatRepository
        {
            public Chat? ById { get; set; }
            public Chat? Created { get; private set; }
            public List<Chat> UserChats { get; } = new List<Chat>();
            public bool NameExists { get; set; }
            public string? DeletedId { get; private set; }
            public Task<Chat?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(this.ById);
            public Task<IEnumerable<Chat>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Chat>>(this.UserChats);
            public Task CreateAsync(Chat chat, CancellationToken cancellationToken = default) { this.Created = chat; this.ById = chat; return Task.CompletedTask; }
            public Task UpdateAsync(Chat chat, CancellationToken cancellationToken = default) { this.ById = chat; return Task.CompletedTask; }
            public Task DeleteAsync(string id, CancellationToken cancellationToken = default) { this.DeletedId = id; return Task.CompletedTask; }
            public Task AddParticipantAsync(string chatId, string userId, CancellationToken cancellationToken = default) { this.ById?.ParticipantIds.Add(userId); return Task.CompletedTask; }
            public Task RemoveParticipantAsync(string chatId, string userId, CancellationToken cancellationToken = default) { this.ById?.ParticipantIds.Remove(userId); return Task.CompletedTask; }
            public Task<bool> ChatNameExistsForUserAsync(string name, string userId, CancellationToken cancellationToken = default) => Task.FromResult(this.NameExists);
        }

        private sealed class FakeUserRepository : IUserRepository
        {
            public Dictionary<string, User> Users { get; } = new Dictionary<string, User>();
            public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(this.Users.GetValueOrDefault(id));
            public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) => Task.FromResult(this.Users.Values.FirstOrDefault(x => x.Username == username));
            public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(this.Users.Values.FirstOrDefault(x => x.Email == email));
            public Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<User>>(this.Users.Values);
            public Task CreateAsync(User user, CancellationToken cancellationToken = default) { this.Users[user.Id] = user; return Task.CompletedTask; }
            public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task DeleteAsync(string id, CancellationToken cancellationToken = default) { this.Users.Remove(id); return Task.CompletedTask; }
        }

        private sealed class FakeMessageRepository : IMessageRepository
        {
            public List<string> DeletedMessageIds { get; } = new List<string>();
            public Exception? DeleteError { get; set; }
            public Task<Message?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult<Message?>(null);
            public Task<IEnumerable<Message>> GetByChatIdAsync(string chatId, int limit = 50, int offset = 0, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Message>>(Array.Empty<Message>());
            public Task CreateAsync(Message message, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task UpdateAsync(Message message, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<int> GetMessageCountAsync(string chatId, CancellationToken cancellationToken = default) => Task.FromResult(0);
            public Task<IEnumerable<Message>> SearchAsync(string chatId, string query, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Message>>(Array.Empty<Message>());
            public Task<List<string>> DeleteByChatIdAsync(string chatId, CancellationToken cancellationToken = default) => this.DeleteError == null ? Task.FromResult(this.DeletedMessageIds) : Task.FromException<List<string>>(this.DeleteError);
        }

        private sealed class FakeAttachmentRepository : IMessageAttachmentRepository
        {
            public List<string> DeletedAttachmentIds { get; } = new List<string>();
            public List<string>? LastMessageIds { get; private set; }
            public Task AddAsync(MessageAttachment attachment, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<MessageAttachment?> GetByIdAsync(string attachmentId, CancellationToken cancellationToken = default) => Task.FromResult<MessageAttachment?>(null);
            public Task<List<MessageAttachment>> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default) => Task.FromResult(new List<MessageAttachment>());
            public Task UpdateAsync(MessageAttachment attachment, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task DeleteAsync(string attachmentId, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<List<MessageAttachment>> GetByMessageIdsAsync(List<string> messageIds, CancellationToken cancellationToken = default) { this.LastMessageIds = messageIds; return Task.FromResult(new List<MessageAttachment>()); }
            public Task<List<string>> DeleteByMessageIdsAsync(List<string> messageIds, CancellationToken cancellationToken = default) { this.LastMessageIds = messageIds; return Task.FromResult(this.DeletedAttachmentIds); }
        }

        private sealed class FakeFolderRepository : IChatFolderRepository
        {
            public string? RemovedFromAll { get; private set; }
            public (string, string)? RemovedFromUser { get; private set; }
            public Task<ChatFolder?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult<ChatFolder?>(null);
            public Task<IEnumerable<ChatFolder>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ChatFolder>>(Array.Empty<ChatFolder>());
            public Task CreateAsync(ChatFolder folder, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task UpdateAsync(ChatFolder folder, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RemoveChatFromAllFoldersAsync(string chatId, CancellationToken cancellationToken = default) { this.RemovedFromAll = chatId; return Task.CompletedTask; }
            public Task RemoveChatFromUserFoldersAsync(string chatId, string userId, CancellationToken cancellationToken = default) { this.RemovedFromUser = (chatId, userId); return Task.CompletedTask; }
        }

        private sealed class FakeStatus : IUserStatusService
        {
            public Dictionary<string, UserStatus> Statuses { get; } = new Dictionary<string, UserStatus>();
            public Task<UserStatus> GetStatusAsync(string userId, CancellationToken cancellationToken = default) => this.GetPublicStatusAsync(userId, cancellationToken);
            public Task<UserStatus> GetPublicStatusAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(this.Statuses.GetValueOrDefault(userId, UserStatus.Offline));
            public Task SetStatusAsync(string userId, UserStatus status, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<bool> GetInvisiblePreferenceAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
            public Task SetInvisiblePreferenceAsync(string userId, bool isInvisible, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RemoveStatusAsync(string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private sealed class FakeAvatar : IAvatarService
        {
            public string? LastKey { get; private set; }
            public Task<string> SaveAvatarAsync(string userId, string fileName, Stream fileStream, CancellationToken cancellationToken = default) { this.LastKey = userId; return Task.FromResult("/avatar/" + userId); }
            public Task<Stream?> GetAvatarStreamAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
            public Task<Stream> GetDefaultAvatarStreamAsync(CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream());
        }
    }
}
