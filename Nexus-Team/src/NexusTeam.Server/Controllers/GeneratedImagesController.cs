namespace NexusTeam.Server.Controllers
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Services.Abstractions;

    /// <summary>
    /// Controller for managing generated images.
    /// </summary>
    [ApiController]
    [Route("api/generated-images")]
    public class GeneratedImagesController : ControllerBase
    {
        private readonly IGeneratedImageService generatedImageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedImagesController"/> class.
        /// </summary>
        /// <param name="generatedImageService">The generated image service.</param>
        public GeneratedImagesController(IGeneratedImageService generatedImageService)
        {
            this.generatedImageService = generatedImageService;
        }

        /// <summary>
        /// Gets all generated images for the current user.
        /// </summary>
        /// <param name="limit">Maximum number of images to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of generated images.</returns>
        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetImages([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var images = await this.generatedImageService.GetByUserIdAsync(userId, limit, cancellationToken);
            return this.Ok(images);
        }

        /// <summary>
        /// Gets a specific generated image.
        /// </summary>
        /// <param name="id">The image ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated image.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetImage(string id, CancellationToken cancellationToken = default)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var image = await this.generatedImageService.GetByIdAsync(id, cancellationToken);
            if (image == null)
            {
                return this.NotFound();
            }

            if (image.UserId != userId)
            {
                return this.NotFound();
            }

            return this.Ok(image);
        }

        /// <summary>
        /// Creates a new generated image record.
        /// </summary>
        /// <param name="request">The create request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created image.</returns>
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CreateImage([FromBody] CreateGeneratedImageRequest request, CancellationToken cancellationToken = default)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var image = await this.generatedImageService.CreateAsync(
                userId,
                request.Prompt,
                request.Model,
                request.ImageUrl,
                request.Width,
                request.Height,
                cancellationToken);

            return this.CreatedAtAction(nameof(this.GetImage), new { id = image.Id }, image);
        }

        /// <summary>
        /// Saves image data to server storage.
        /// </summary>
        /// <param name="id">The image ID.</param>
        /// <param name="request">The image data request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The download URL.</returns>
        [HttpPost("{id}/data")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SaveImageData(string id, [FromBody] SaveImageDataRequest request, CancellationToken cancellationToken = default)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var image = await this.generatedImageService.GetByIdAsync(id, cancellationToken);
            if (image == null || image.UserId != userId)
            {
                return this.NotFound();
            }

            try
            {
                var imageData = System.Convert.FromBase64String(request.ImageDataBase64);
                var downloadUrl = await this.generatedImageService.SaveImageDataAsync(id, imageData, cancellationToken);
                return this.Ok(new { downloadUrl });
            }
            catch (System.Exception ex)
            {
                return this.BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Downloads a generated image.
        /// </summary>
        /// <param name="id">The image ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The image file.</returns>
        [HttpGet("{id}/download")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DownloadImage(string id, CancellationToken cancellationToken = default)
        {
            var image = await this.generatedImageService.GetByIdAsync(id, cancellationToken);
            if (image == null)
            {
                return this.NotFound();
            }

            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            if (image.UserId != userId)
            {
                return this.NotFound();
            }

            var result = await this.generatedImageService.GetImageStreamAsync(id, cancellationToken);
            if (result == null || result.Value.Stream == null)
            {
                return this.NotFound();
            }

            return this.File(result.Value.Stream, result.Value.ContentType, $"{id}.png");
        }

        /// <summary>
        /// Deletes a generated image.
        /// </summary>
        /// <param name="id">The image ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteImage(string id, CancellationToken cancellationToken = default)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var deleted = await this.generatedImageService.DeleteAsync(id, userId, cancellationToken);
            if (!deleted)
            {
                return this.NotFound();
            }

            return this.NoContent();
        }

        /// <summary>
        /// Gets recent prompts for the current user.
        /// </summary>
        /// <param name="limit">Maximum number of prompts to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of recent prompts.</returns>
        [HttpGet("prompts")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetRecentPrompts([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var prompts = await this.generatedImageService.GetRecentPromptsAsync(userId, limit, cancellationToken);
            return this.Ok(prompts);
        }
    }

    /// <summary>
    /// Request model for creating a generated image.
    /// </summary>
    public class CreateGeneratedImageRequest
    {
        /// <summary>
        /// Gets or sets the prompt.
        /// </summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        public string Model { get; set; } = "flux";

        /// <summary>
        /// Gets or sets the image URL.
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        public int Width { get; set; } = 1024;

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        public int Height { get; set; } = 1024;
    }

    /// <summary>
    /// Request model for saving image data.
    /// </summary>
    public class SaveImageDataRequest
    {
        /// <summary>
        /// Gets or sets the base64 encoded image data.
        /// </summary>
        public string ImageDataBase64 { get; set; } = string.Empty;
    }
}
