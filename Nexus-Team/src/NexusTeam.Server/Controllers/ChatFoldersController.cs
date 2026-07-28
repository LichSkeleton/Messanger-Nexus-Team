namespace NexusTeam.Server.Controllers
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using Serilog;

    /// <summary>
    /// Controller for chat folder-related endpoints.
    /// </summary>
    [ApiController]
    [Route("api/folders")]
    public class ChatFoldersController : ControllerBase
    {
        private readonly IChatFolderService folderService;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatFoldersController"/> class.
        /// </summary>
        /// <param name="folderService">Folder service.</param>
        /// <param name="logger">Logger.</param>
        public ChatFoldersController(IChatFolderService folderService, ILogger logger)
        {
            this.folderService = folderService;
            this.logger = logger;
        }

        /// <summary>
        /// Gets all folders for the authenticated user.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of folder DTOs.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ChatFolderDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<ChatFolderDto>>> GetFolders(CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                this.logger.Warning("Unauthorized folder access attempt");
                return this.Unauthorized();
            }

            var folders = await this.folderService.GetUserFoldersAsync(userId, cancellationToken);
            return this.Ok(folders);
        }

        /// <summary>
        /// Gets a specific folder by ID.
        /// </summary>
        /// <param name="id">The folder ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The folder DTO if found.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ChatFolderDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ChatFolderDto>> GetFolder(string id, CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var folder = await this.folderService.GetFolderByIdAsync(id, userId, cancellationToken);
            if (folder == null)
            {
                return this.NotFound();
            }

            return this.Ok(folder);
        }

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        /// <param name="request">The create folder request data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created folder DTO.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ChatFolderDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ChatFolderDto>> CreateFolder([FromBody] CreateChatFolderRequest? request, CancellationToken cancellationToken)
        {
            this.logger.Information("POST /api/folders - Creating new folder {FolderName}", request?.Name ?? "unknown");

            if (request == null)
            {
                this.logger.Warning("Null request body for folder creation");
                return this.BadRequest(new { error = "Request body is required." });
            }

            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                this.logger.Warning("Unauthorized folder creation attempt");
                return this.Unauthorized();
            }

            if (!this.ModelState.IsValid)
            {
                this.logger.Warning("Validation failed for folder creation: {ValidationErrors}", this.ModelState);
                return this.BadRequest(this.ModelState);
            }

            try
            {
                var folder = await this.folderService.CreateFolderAsync(request, userId, cancellationToken);
                this.logger.Information("Folder {FolderId} created successfully by user {UserId}", folder.Id, userId);
                return this.CreatedAtAction(nameof(this.GetFolder), new { id = folder.Id }, folder);
            }
            catch (Shared.Exceptions.ValidationException ex)
            {
                this.logger.Warning("Validation error during folder creation: {Message}", ex.Message);
                return this.BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing folder.
        /// </summary>
        /// <param name="id">The folder ID.</param>
        /// <param name="request">The update request data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated folder DTO.</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ChatFolderDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ChatFolderDto>> UpdateFolder(string id, [FromBody] CreateChatFolderRequest? request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return this.BadRequest(new { error = "Request body is required." });
            }

            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            try
            {
                var folder = await this.folderService.UpdateFolderAsync(id, request, userId, cancellationToken);
                return this.Ok(folder);
            }
            catch (Shared.Exceptions.NotFoundException)
            {
                return this.NotFound();
            }
            catch (Shared.Exceptions.UnauthorizedException)
            {
                return this.Unauthorized();
            }
            catch (Shared.Exceptions.ValidationException ex)
            {
                return this.BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a folder.
        /// </summary>
        /// <param name="id">The folder ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>No content if successful.</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeleteFolder(string id, CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            try
            {
                await this.folderService.DeleteFolderAsync(id, userId, cancellationToken);
                return this.NoContent();
            }
            catch (Shared.Exceptions.NotFoundException)
            {
                return this.NotFound();
            }
            catch (Shared.Exceptions.UnauthorizedException)
            {
                return this.Unauthorized();
            }
        }
    }
}
