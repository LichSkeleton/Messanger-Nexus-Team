namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Exceptions;
    using NexusTeam.Shared.Models;
    using Serilog;
    using Xunit;

    public class ChatFolderServiceTests
    {
        private static readonly DateTime Now = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task GetUserFoldersAsync_MapsRepositoryFolders()
        {
            var fixture = new Fixture();
            fixture.Folders.UserFolders.Add(CreateFolder());

            var result = (await fixture.Service.GetUserFoldersAsync("user-1")).ToList();

            var dto = Assert.Single(result);
            Assert.Equal("folder-1", dto.Id);
            Assert.Equal(new[] { "chat-1" }, dto.ChatIds);
        }

        [Fact]
        public async Task GetFolderByIdAsync_WhenMissing_ReturnsNull()
        {
            var fixture = new Fixture();

            Assert.Null(await fixture.Service.GetFolderByIdAsync("missing", "user-1"));
        }

        [Fact]
        public async Task GetFolderByIdAsync_WhenOwnedByAnotherUser_ReturnsNull()
        {
            var fixture = new Fixture();
            fixture.Folders.Folder = CreateFolder(owner: "user-2");

            Assert.Null(await fixture.Service.GetFolderByIdAsync("folder-1", "user-1"));
        }

        [Fact]
        public async Task CreateFolderAsync_TrimsNameDeduplicatesChatsAndSetsMetadata()
        {
            var fixture = new Fixture();
            fixture.Chats.ById["chat-1"] = CreateChat("chat-1", "user-1");

            var result = await fixture.Service.CreateFolderAsync(new CreateChatFolderRequest
            {
                Name = "  Work  ",
                ChatIds = new List<string> { "chat-1", "chat-1" },
            }, "user-1");

            var created = Assert.IsType<ChatFolder>(fixture.Folders.Created);
            Assert.Equal("generated-folder", created.Id);
            Assert.Equal("Work", created.Name);
            Assert.Equal(new[] { "chat-1" }, created.ChatIds);
            Assert.Equal(Now, created.CreatedAt);
            Assert.Equal(Now, created.UpdatedAt);
            Assert.Equal(created.Id, result.Id);
        }

        [Fact]
        public async Task CreateFolderAsync_WithNullChats_CreatesEmptyFolder()
        {
            var fixture = new Fixture();
            var request = new CreateChatFolderRequest { Name = "Empty", ChatIds = null! };

            var result = await fixture.Service.CreateFolderAsync(request, "user-1");

            Assert.Empty(result.ChatIds);
            Assert.NotNull(request.ChatIds);
        }

        [Fact]
        public async Task CreateFolderAsync_WithBlankName_ThrowsValidation()
        {
            var fixture = new Fixture();

            await Assert.ThrowsAsync<ValidationException>(() =>
                fixture.Service.CreateFolderAsync(
                    new CreateChatFolderRequest { Name = " " },
                    "user-1"));
        }

        [Fact]
        public async Task CreateFolderAsync_WithMissingChat_ThrowsValidation()
        {
            var fixture = new Fixture();

            var exception = await Assert.ThrowsAsync<ValidationException>(() =>
                fixture.Service.CreateFolderAsync(new CreateChatFolderRequest
                {
                    Name = "Work",
                    ChatIds = new List<string> { "missing" },
                }, "user-1"));

            Assert.Contains("does not exist", exception.Message);
        }

        [Fact]
        public async Task CreateFolderAsync_WithUnauthorizedChat_ThrowsValidation()
        {
            var fixture = new Fixture();
            fixture.Chats.ById["chat-1"] = CreateChat("chat-1", "user-2");

            var exception = await Assert.ThrowsAsync<ValidationException>(() =>
                fixture.Service.CreateFolderAsync(new CreateChatFolderRequest
                {
                    Name = "Work",
                    ChatIds = new List<string> { "chat-1" },
                }, "user-1"));

            Assert.Contains("do not have access", exception.Message);
        }

        [Fact]
        public async Task UpdateFolderAsync_UpdatesMutableFieldsAndTimestamp()
        {
            var fixture = new Fixture();
            fixture.Folders.Folder = CreateFolder();
            fixture.Chats.ById["chat-2"] = CreateChat("chat-2", "user-1");

            var result = await fixture.Service.UpdateFolderAsync(
                "folder-1",
                new CreateChatFolderRequest
                {
                    Name = "  Updated  ",
                    ChatIds = new List<string> { "chat-2", "chat-2" },
                },
                "user-1");

            Assert.Equal("Updated", result.Name);
            Assert.Equal(new[] { "chat-2" }, result.ChatIds);
            Assert.Equal(Now, result.UpdatedAt);
            Assert.Same(fixture.Folders.Folder, fixture.Folders.Updated);
        }

        [Fact]
        public async Task UpdateFolderAsync_WhenMissing_ThrowsNotFound()
        {
            var fixture = new Fixture();

            await Assert.ThrowsAsync<NotFoundException>(() => fixture.Service.UpdateFolderAsync(
                "missing",
                new CreateChatFolderRequest { Name = "Updated" },
                "user-1"));
        }

        [Fact]
        public async Task UpdateFolderAsync_WhenNotOwner_ThrowsUnauthorized()
        {
            var fixture = new Fixture();
            fixture.Folders.Folder = CreateFolder(owner: "user-2");

            await Assert.ThrowsAsync<UnauthorizedException>(() => fixture.Service.UpdateFolderAsync(
                "folder-1",
                new CreateChatFolderRequest { Name = "Updated" },
                "user-1"));
        }

        [Fact]
        public async Task DeleteFolderAsync_WhenOwner_DeletesFolder()
        {
            var fixture = new Fixture();
            fixture.Folders.Folder = CreateFolder();

            await fixture.Service.DeleteFolderAsync("folder-1", "user-1");

            Assert.Equal("folder-1", fixture.Folders.DeletedId);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DeleteFolderAsync_WhenMissingOrUnauthorized_Throws(bool existsForOtherUser)
        {
            var fixture = new Fixture();
            if (existsForOtherUser)
            {
                fixture.Folders.Folder = CreateFolder(owner: "user-2");
            }

            var exception = await Record.ExceptionAsync(() =>
                fixture.Service.DeleteFolderAsync("folder-1", "user-1"));

            Assert.IsAssignableFrom<DomainException>(exception);
            Assert.Null(fixture.Folders.DeletedId);
        }

        private static ChatFolder CreateFolder(string owner = "user-1") => new ChatFolder
        {
            Id = "folder-1",
            Name = "Work",
            UserId = owner,
            ChatIds = new List<string> { "chat-1" },
            CreatedAt = Now.AddDays(-1),
            UpdatedAt = Now.AddDays(-1),
        };

        private static Chat CreateChat(string id, params string[] participants) => new Chat
        {
            Id = id,
            ParticipantIds = participants.ToList(),
        };

        private sealed class Fixture
        {
            public Fixture()
            {
                this.Service = new ChatFolderService(
                    this.Folders,
                    this.Chats,
                    new FixedId(),
                    new FixedClock(),
                    new LoggerConfiguration().CreateLogger());
            }

            public FakeFolderRepository Folders { get; } = new FakeFolderRepository();

            public FakeChatRepository Chats { get; } = new FakeChatRepository();

            public ChatFolderService Service { get; }
        }

        private sealed class FixedId : IIdGenerator
        {
            public string GenerateId() => "generated-folder";
        }

        private sealed class FixedClock : IClock
        {
            public DateTime UtcNow => Now;
        }

        private sealed class FakeFolderRepository : IChatFolderRepository
        {
            public ChatFolder? Folder { get; set; }

            public List<ChatFolder> UserFolders { get; } = new List<ChatFolder>();

            public ChatFolder? Created { get; private set; }

            public ChatFolder? Updated { get; private set; }

            public string? DeletedId { get; private set; }

            public Task<ChatFolder?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
                => Task.FromResult(this.Folder);

            public Task<IEnumerable<ChatFolder>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
                => Task.FromResult<IEnumerable<ChatFolder>>(this.UserFolders);

            public Task CreateAsync(ChatFolder folder, CancellationToken cancellationToken = default)
            {
                this.Created = folder;
                return Task.CompletedTask;
            }

            public Task UpdateAsync(ChatFolder folder, CancellationToken cancellationToken = default)
            {
                this.Updated = folder;
                return Task.CompletedTask;
            }

            public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            {
                this.DeletedId = id;
                return Task.CompletedTask;
            }

            public Task RemoveChatFromAllFoldersAsync(string chatId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task RemoveChatFromUserFoldersAsync(string chatId, string userId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }

        private sealed class FakeChatRepository : IChatRepository
        {
            public Dictionary<string, Chat> ById { get; } = new Dictionary<string, Chat>();

            public Task<Chat?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
                => Task.FromResult(this.ById.TryGetValue(id, out var chat) ? chat : null);

            public Task<IEnumerable<Chat>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task CreateAsync(Chat chat, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task UpdateAsync(Chat chat, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task AddParticipantAsync(string chatId, string userId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task RemoveParticipantAsync(string chatId, string userId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<bool> ChatNameExistsForUserAsync(string name, string userId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }
    }
}
