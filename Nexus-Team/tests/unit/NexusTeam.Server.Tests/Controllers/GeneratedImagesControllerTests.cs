namespace NexusTeam.Server.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Controllers;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using Xunit;

    public class GeneratedImagesControllerTests
    {
        [Fact]
        public async Task GetImages_WithoutUser_ReturnsUnauthorized()
        {
            var fixture = new Fixture();
            Assert.IsType<UnauthorizedResult>(await fixture.Controller.GetImages());
        }

        [Fact]
        public async Task GetImages_ForwardsAuthenticatedUserAndLimit()
        {
            var fixture = new Fixture("user-1");
            var result = Assert.IsType<OkObjectResult>(await fixture.Controller.GetImages(7));
            Assert.NotNull(result.Value);
            Assert.Equal(("user-1", 7), fixture.Service.UserQuery);
        }

        [Fact]
        public async Task GetImage_ReturnsUnauthorizedNotFoundOrOk()
        {
            Assert.IsType<UnauthorizedResult>(await new Fixture().Controller.GetImage("missing"));
            var fixture = new Fixture("user-1");
            Assert.IsType<NotFoundResult>(await fixture.Controller.GetImage("missing"));
            fixture.Service.Image = new GeneratedImageDto { Id = "image-1", UserId = "user-1" };
            Assert.IsType<OkObjectResult>(await fixture.Controller.GetImage("image-1"));
            fixture.Service.Image = new GeneratedImageDto { Id = "image-1", UserId = "other" };
            Assert.IsType<NotFoundResult>(await fixture.Controller.GetImage("image-1"));
        }

        [Fact]
        public async Task CreateImage_WithoutUser_ReturnsUnauthorized()
        {
            var fixture = new Fixture();
            Assert.IsType<UnauthorizedResult>(await fixture.Controller.CreateImage(new CreateGeneratedImageRequest()));
        }

        [Fact]
        public async Task CreateImage_ForwardsPayloadAndReturnsCreatedAtAction()
        {
            var fixture = new Fixture("user-1");
            var request = new CreateGeneratedImageRequest { Prompt = "cat", Model = "flux", ImageUrl = "url", Width = 10, Height = 20 };

            var result = Assert.IsType<CreatedAtActionResult>(await fixture.Controller.CreateImage(request));

            Assert.Equal(nameof(GeneratedImagesController.GetImage), result.ActionName);
            Assert.Equal("image-1", result.RouteValues!["id"]);
            Assert.Equal(("user-1", "cat", "flux", "url", 10, 20), fixture.Service.CreateCall);
        }

        [Fact]
        public async Task SaveImageData_WithValidBase64_ForwardsDecodedBytes()
        {
            var fixture = new Fixture("user-1");
            fixture.Service.Image = new GeneratedImageDto { Id = "image-1", UserId = "user-1" };
            var result = await fixture.Controller.SaveImageData("image-1", new SaveImageDataRequest { ImageDataBase64 = "AQID" });

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(new byte[] { 1, 2, 3 }, fixture.Service.SavedBytes);
        }

        [Fact]
        public async Task SaveImageData_WithInvalidBase64_ReturnsBadRequest()
        {
            var fixture = new Fixture("user-1");
            fixture.Service.Image = new GeneratedImageDto { Id = "image-1", UserId = "user-1" };
            Assert.IsType<BadRequestObjectResult>(await fixture.Controller.SaveImageData("image-1", new SaveImageDataRequest { ImageDataBase64 = "!" }));
        }

        [Fact]
        public async Task DownloadImage_ReturnsUnauthorizedNotFoundOrFile()
        {
            Assert.IsType<NotFoundResult>(await new Fixture().Controller.DownloadImage("missing"));
            var fixture = new Fixture("user-1");
            Assert.IsType<NotFoundResult>(await fixture.Controller.DownloadImage("missing"));
            fixture.Service.Image = new GeneratedImageDto { Id = "image-1", UserId = "user-1" };
            fixture.Service.StreamResult = (new MemoryStream(new byte[] { 1 }), "image/png");

            var result = Assert.IsType<FileStreamResult>(await fixture.Controller.DownloadImage("image-1"));
            Assert.Equal("image/png", result.ContentType);
            Assert.Equal("image-1.png", result.FileDownloadName);
            await result.FileStream.DisposeAsync();
        }

        [Fact]
        public async Task DeleteImage_HandlesUnauthorizedNotFoundAndSuccess()
        {
            var anonymous = new Fixture();
            Assert.IsType<UnauthorizedResult>(await anonymous.Controller.DeleteImage("image-1"));
            var fixture = new Fixture("user-1");
            Assert.IsType<NotFoundResult>(await fixture.Controller.DeleteImage("image-1"));
            fixture.Service.DeleteResult = true;
            Assert.IsType<NoContentResult>(await fixture.Controller.DeleteImage("image-1"));
        }

        [Fact]
        public async Task GetRecentPrompts_HandlesUnauthorizedAndForwardsLimit()
        {
            Assert.IsType<UnauthorizedResult>(await new Fixture().Controller.GetRecentPrompts());
            var fixture = new Fixture("user-1");
            Assert.IsType<OkObjectResult>(await fixture.Controller.GetRecentPrompts(4));
            Assert.Equal(("user-1", 4), fixture.Service.PromptQuery);
        }

        private sealed class Fixture
        {
            public Fixture(string? userId = null)
            {
                this.Controller = new GeneratedImagesController(this.Service)
                {
                    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
                };
                if (userId != null) this.Controller.HttpContext.Items["UserId"] = userId;
            }
            public FakeService Service { get; } = new FakeService();
            public GeneratedImagesController Controller { get; }
        }

        private sealed class FakeService : IGeneratedImageService
        {
            public GeneratedImageDto? Image { get; set; }
            public bool DeleteResult { get; set; }
            public byte[]? SavedBytes { get; private set; }
            public (Stream? Stream, string ContentType)? StreamResult { get; set; }
            public (string, int)? UserQuery { get; private set; }
            public (string, int)? PromptQuery { get; private set; }
            public (string, string, string, string, int, int)? CreateCall { get; private set; }
            public Task<GeneratedImageDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(this.Image);
            public Task<IEnumerable<GeneratedImageDto>> GetByUserIdAsync(string userId, int limit = 50, CancellationToken cancellationToken = default) { this.UserQuery = (userId, limit); return Task.FromResult<IEnumerable<GeneratedImageDto>>(Array.Empty<GeneratedImageDto>()); }
            public Task<GeneratedImageDto> CreateAsync(string userId, string prompt, string model, string imageUrl, int width, int height, CancellationToken cancellationToken = default) { this.CreateCall = (userId, prompt, model, imageUrl, width, height); return Task.FromResult(new GeneratedImageDto { Id = "image-1" }); }
            public Task<string> SaveImageDataAsync(string id, byte[] imageData, CancellationToken cancellationToken = default) { this.SavedBytes = imageData; return Task.FromResult("/download"); }
            public Task<bool> DeleteAsync(string id, string userId, CancellationToken cancellationToken = default) => Task.FromResult(this.DeleteResult);
            public Task<IEnumerable<string>> GetRecentPromptsAsync(string userId, int limit = 10, CancellationToken cancellationToken = default) { this.PromptQuery = (userId, limit); return Task.FromResult<IEnumerable<string>>(new[] { "prompt" }); }
            public Task<(Stream? Stream, string ContentType)?> GetImageStreamAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(this.StreamResult);
        }
    }
}
