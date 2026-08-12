namespace NexusTeam.Server.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Controllers;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Exceptions;
    using Serilog;
    using Xunit;

    public class ChatFoldersControllerTests
    {
        [Fact]
        public async Task GetFolders_RequiresUserAndForwardsAuthenticatedId()
        {
            Assert.IsType<UnauthorizedResult>((await new Fixture().Controller.GetFolders(default)).Result);
            var fixture = new Fixture("user-1");
            Assert.IsType<OkObjectResult>((await fixture.Controller.GetFolders(default)).Result);
            Assert.Equal("user-1", fixture.Service.LastUserId);
        }

        [Fact]
        public async Task GetFolder_MapsMissingToNotFoundAndExistingToOk()
        {
            var fixture = new Fixture("user-1");
            Assert.IsType<NotFoundResult>((await fixture.Controller.GetFolder("folder-1", default)).Result);
            fixture.Service.Folder = new ChatFolderDto { Id = "folder-1" };
            Assert.IsType<OkObjectResult>((await fixture.Controller.GetFolder("folder-1", default)).Result);
        }

        [Fact]
        public async Task CreateFolder_RejectsNullUnauthorizedAndInvalidModel()
        {
            Assert.IsType<BadRequestObjectResult>((await new Fixture().Controller.CreateFolder(null, default)).Result);
            Assert.IsType<UnauthorizedResult>((await new Fixture().Controller.CreateFolder(Request(), default)).Result);
            var invalid = new Fixture("user-1");
            invalid.Controller.ModelState.AddModelError("name", "required");
            Assert.IsType<BadRequestObjectResult>((await invalid.Controller.CreateFolder(Request(), default)).Result);
        }

        [Fact]
        public async Task CreateFolder_ReturnsCreatedAndMapsValidationFailureToBadRequest()
        {
            var fixture = new Fixture("user-1");
            fixture.Service.Folder = new ChatFolderDto { Id = "folder-1" };
            Assert.IsType<CreatedAtActionResult>((await fixture.Controller.CreateFolder(Request(), default)).Result);
            fixture.Service.Error = new ValidationException("duplicate");
            Assert.IsType<BadRequestObjectResult>((await fixture.Controller.CreateFolder(Request(), default)).Result);
        }

        [Theory]
        [InlineData("notfound", typeof(NotFoundResult))]
        [InlineData("unauthorized", typeof(UnauthorizedResult))]
        [InlineData("validation", typeof(BadRequestObjectResult))]
        public async Task UpdateFolder_MapsDomainErrors(string error, Type resultType)
        {
            var fixture = new Fixture("user-1");
            fixture.Service.Error = error switch
            {
                "notfound" => new NotFoundException("missing"),
                "unauthorized" => new UnauthorizedException("forbidden"),
                _ => new ValidationException("invalid"),
            };

            var result = (await fixture.Controller.UpdateFolder("folder-1", Request(), default)).Result;
            Assert.IsType(resultType, result);
        }

        [Fact]
        public async Task UpdateFolder_OnSuccess_ReturnsOk()
        {
            var fixture = new Fixture("user-1");
            fixture.Service.Folder = new ChatFolderDto { Id = "folder-1" };
            Assert.IsType<OkObjectResult>((await fixture.Controller.UpdateFolder("folder-1", Request(), default)).Result);
        }

        [Theory]
        [InlineData("none", typeof(NoContentResult))]
        [InlineData("notfound", typeof(NotFoundResult))]
        [InlineData("unauthorized", typeof(UnauthorizedResult))]
        public async Task DeleteFolder_MapsServiceOutcomes(string error, Type resultType)
        {
            var fixture = new Fixture("user-1");
            fixture.Service.Error = error switch
            {
                "notfound" => new NotFoundException("missing"),
                "unauthorized" => new UnauthorizedException("forbidden"),
                _ => null,
            };
            Assert.IsType(resultType, await fixture.Controller.DeleteFolder("folder-1", default));
        }

        private static CreateChatFolderRequest Request() => new CreateChatFolderRequest { Name = "Folder" };

        private sealed class Fixture
        {
            public Fixture(string? userId = null)
            {
                this.Controller = new ChatFoldersController(this.Service, new LoggerConfiguration().CreateLogger())
                {
                    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
                };
                if (userId != null) this.Controller.HttpContext.Items["UserId"] = userId;
            }
            public FakeService Service { get; } = new FakeService();
            public ChatFoldersController Controller { get; }
        }

        private sealed class FakeService : IChatFolderService
        {
            public ChatFolderDto? Folder { get; set; }
            public Exception? Error { get; set; }
            public string? LastUserId { get; private set; }
            public Task<IEnumerable<ChatFolderDto>> GetUserFoldersAsync(string userId, CancellationToken cancellationToken = default) { this.LastUserId = userId; return Task.FromResult<IEnumerable<ChatFolderDto>>(Array.Empty<ChatFolderDto>()); }
            public Task<ChatFolderDto?> GetFolderByIdAsync(string folderId, string userId, CancellationToken cancellationToken = default) => Task.FromResult(this.Folder);
            public Task<ChatFolderDto> CreateFolderAsync(CreateChatFolderRequest request, string userId, CancellationToken cancellationToken = default) => this.Error == null ? Task.FromResult(this.Folder ?? new ChatFolderDto()) : Task.FromException<ChatFolderDto>(this.Error);
            public Task<ChatFolderDto> UpdateFolderAsync(string folderId, CreateChatFolderRequest request, string userId, CancellationToken cancellationToken = default) => this.Error == null ? Task.FromResult(this.Folder ?? new ChatFolderDto()) : Task.FromException<ChatFolderDto>(this.Error);
            public Task DeleteFolderAsync(string folderId, string userId, CancellationToken cancellationToken = default) => this.Error == null ? Task.CompletedTask : Task.FromException(this.Error);
        }
    }
}
