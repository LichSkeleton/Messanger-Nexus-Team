namespace NexusTeam.Server.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Controllers;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using NexusTeam.Shared.Models;
    using Serilog;
    using Xunit;

    public class UsersControllerTests
    {
        [Fact]
        public async Task GetUsers_RequiresAuthenticationAndExcludesCurrentUser()
        {
            Assert.IsType<UnauthorizedResult>((await new Fixture().Controller.GetUsers(default)).Result);
            var fixture = new Fixture("me");
            fixture.Users.Items.Add(User("me"));
            fixture.Users.Items.Add(User("other"));
            fixture.Status.PublicStatus = UserStatus.DoNotDisturb;

            var result = Assert.IsType<OkObjectResult>((await fixture.Controller.GetUsers(default)).Result);
            var users = Assert.IsAssignableFrom<IEnumerable<UserDto>>(result.Value).ToList();
            Assert.Equal("other", Assert.Single(users).Id);
            Assert.Equal(UserStatus.DoNotDisturb, users[0].Status);
        }

        [Fact]
        public async Task UpdateProfile_ValidatesUserDisplayNameAndExistence()
        {
            Assert.IsType<UnauthorizedResult>((await new Fixture().Controller.UpdateProfile(new UpdateUserProfileRequest { DisplayName = "Name" }, default)).Result);
            var fixture = new Fixture("user-1");
            Assert.IsType<BadRequestObjectResult>((await fixture.Controller.UpdateProfile(new UpdateUserProfileRequest { DisplayName = " " }, default)).Result);
            Assert.IsType<NotFoundResult>((await fixture.Controller.UpdateProfile(new UpdateUserProfileRequest { DisplayName = "Name" }, default)).Result);
        }

        [Fact]
        public async Task UpdateProfile_UpdatesUserAndReturnsCurrentStatus()
        {
            var fixture = new Fixture("user-1");
            fixture.Users.Items.Add(User("user-1"));
            fixture.Status.OwnStatus = UserStatus.Away;

            var result = Assert.IsType<OkObjectResult>((await fixture.Controller.UpdateProfile(new UpdateUserProfileRequest { DisplayName = "New" }, default)).Result);
            var dto = Assert.IsType<UserDto>(result.Value);
            Assert.Equal("New", dto.DisplayName);
            Assert.Equal(UserStatus.Away, dto.Status);
            Assert.NotNull(fixture.Users.Updated);
        }

        [Fact]
        public async Task GetMyStatus_ReturnsAuthenticatedUsersPrivateStatus()
        {
            var fixture = new Fixture("user-1");
            fixture.Status.OwnStatus = UserStatus.Invisible;
            var dto = Assert.IsType<StatusUpdateDto>(Assert.IsType<OkObjectResult>((await fixture.Controller.GetMyStatus(default)).Result).Value);
            Assert.Equal(UserStatus.Invisible, dto.Status);
        }

        [Theory]
        [InlineData(UserStatus.Offline)]
        [InlineData(UserStatus.Away)]
        [InlineData(UserStatus.DoNotDisturb)]
        public async Task UpdateMyStatus_RejectsUnsupportedStatuses(UserStatus status)
        {
            var fixture = new Fixture("user-1");
            Assert.IsType<BadRequestObjectResult>((await fixture.Controller.UpdateMyStatus(new UpdateUserStatusRequest { Status = status }, default)).Result);
        }

        [Theory]
        [InlineData(UserStatus.Online, false)]
        [InlineData(UserStatus.Invisible, true)]
        public async Task UpdateMyStatus_PersistsStatusAndInvisiblePreference(UserStatus status, bool invisible)
        {
            var fixture = new Fixture("user-1");
            var result = await fixture.Controller.UpdateMyStatus(new UpdateUserStatusRequest { Status = status }, default);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(status, fixture.Status.SetStatus);
            Assert.Equal(invisible, fixture.Status.InvisiblePreference);
        }

        [Fact]
        public async Task UploadAvatar_ValidatesAuthenticationTypeAndSize()
        {
            Assert.IsType<UnauthorizedResult>((await new Fixture().Controller.UploadAvatarAsync(File("a.png", 1), default)).Result);
            var fixture = new Fixture("user-1");
            Assert.IsType<BadRequestObjectResult>((await fixture.Controller.UploadAvatarAsync(File("a.exe", 1), default)).Result);
            Assert.Equal(413, Assert.IsType<ObjectResult>((await fixture.Controller.UploadAvatarAsync(File("a.png", 11L * 1024 * 1024), default)).Result).StatusCode);
        }

        [Fact]
        public async Task UploadAvatar_UpdatesUserAndReturnsDto()
        {
            var fixture = new Fixture("user-1");
            fixture.Users.Items.Add(User("user-1"));

            var result = Assert.IsType<OkObjectResult>((await fixture.Controller.UploadAvatarAsync(File("a.png", 1, new byte[] { 1 }), default)).Result);

            Assert.Equal("/avatar/user-1", Assert.IsType<UserDto>(result.Value).AvatarUrl);
            Assert.Equal("/avatar/user-1", fixture.Users.Updated!.AvatarUrl);
        }

        [Fact]
        public async Task GetAvatar_UsesRequestedStreamOrFallsBackToDefault()
        {
            var fixture = new Fixture();
            var fallback = Assert.IsType<FileStreamResult>(await fixture.Controller.GetAvatarAsync("missing", default));
            Assert.Equal(1, fixture.Avatar.DefaultCalls);
            await fallback.FileStream.DisposeAsync();
            fixture.Avatar.UserStream = new MemoryStream(new byte[] { 2 });
            var direct = Assert.IsType<FileStreamResult>(await fixture.Controller.GetAvatarAsync("user-1", default));
            Assert.Equal("avatar_user-1.jpg", direct.FileDownloadName);
            await direct.FileStream.DisposeAsync();
        }

        private static User User(string id) => new User { Id = id, Username = id, Email = id + "@example.com", DisplayName = id };
        private static IFormFile File(string name, long length, byte[]? data = null) => new FormFile(new MemoryStream(data ?? Array.Empty<byte>()), 0, length, "file", name);

        private sealed class Fixture
        {
            public Fixture(string? userId = null)
            {
                this.Controller = new UsersController(this.Users, new LoggerConfiguration().CreateLogger(), this.Status, this.Avatar, this.Chats, new Connections())
                {
                    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
                };
                if (userId != null) this.Controller.HttpContext.Items["UserId"] = userId;
            }
            public Users Users { get; } = new Users(); public Status Status { get; } = new Status(); public Avatar Avatar { get; } = new Avatar(); public Chats Chats { get; } = new Chats(); public UsersController Controller { get; }
        }

        private sealed class Users : IUserRepository
        {
            public List<User> Items { get; } = new List<User>(); public User? Updated { get; private set; }
            public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(this.Items.FirstOrDefault(x => x.Id == id)); public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) => Task.FromResult(this.Items.FirstOrDefault(x => x.Username == username)); public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(this.Items.FirstOrDefault(x => x.Email == email)); public Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<User>>(this.Items); public Task CreateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask; public Task UpdateAsync(User user, CancellationToken cancellationToken = default) { this.Updated = user; return Task.CompletedTask; } public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private sealed class Status : IUserStatusService
        {
            public UserStatus OwnStatus { get; set; } = UserStatus.Online; public UserStatus PublicStatus { get; set; } = UserStatus.Offline; public UserStatus? SetStatus { get; private set; } public bool? InvisiblePreference { get; private set; }
            public Task<UserStatus> GetStatusAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(this.OwnStatus); public Task<UserStatus> GetPublicStatusAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(this.PublicStatus); public Task SetStatusAsync(string userId, UserStatus status, CancellationToken cancellationToken = default) { this.SetStatus = status; return Task.CompletedTask; } public Task<bool> GetInvisiblePreferenceAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(false); public Task SetInvisiblePreferenceAsync(string userId, bool isInvisible, CancellationToken cancellationToken = default) { this.InvisiblePreference = isInvisible; return Task.CompletedTask; } public Task RemoveStatusAsync(string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private sealed class Avatar : IAvatarService
        {
            public Stream? UserStream { get; set; } public int DefaultCalls { get; private set; }
            public Task<string> SaveAvatarAsync(string userId, string fileName, Stream fileStream, CancellationToken cancellationToken = default) => Task.FromResult("/avatar/" + userId); public Task<Stream?> GetAvatarStreamAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(this.UserStream); public Task<Stream> GetDefaultAvatarStreamAsync(CancellationToken cancellationToken = default) { this.DefaultCalls++; return Task.FromResult<Stream>(new MemoryStream(new byte[] { 1 })); }
        }

        private sealed class Chats : IChatService
        {
            public Task<IEnumerable<ChatDto>> GetUserChatsAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ChatDto>>(Array.Empty<ChatDto>()); public Task<ChatDto?> GetChatByIdAsync(string chatId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<ChatDto> CreateChatAsync(CreateChatRequest request, string creatorUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task DeleteChatAsync(string chatId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task LeaveChatAsync(string chatId, string userId, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<ChatDto> UpdateChatAsync(string chatId, string userId, UpdateChatRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException(); public Task<ChatDto> UploadChatAvatarAsync(string chatId, string userId, string fileName, Stream fileStream, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class Connections : IWebSocketConnectionManager
        {
            public void AddConnection(string userId, WebSocket socket, string connectionId) { } public void RemoveConnection(string connectionId) { } public WebSocket? GetSocketByConnectionId(string connectionId) => null; public IEnumerable<string> GetConnectionIdsByUserId(string userId) => Array.Empty<string>(); public string? GetUserIdByConnectionId(string connectionId) => null; public Task SendMessageAsync(string connectionId, string message, CancellationToken cancellationToken = default) => Task.CompletedTask; public Task BroadcastToUserAsync(string userId, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }
}
