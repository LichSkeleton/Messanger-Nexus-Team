namespace NexusTeam.Server.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Middleware;
    using NexusTeam.Server.Services;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Helpers;
    using Serilog;

    /// <summary>
    /// Controller for user-related endpoints.
    /// </summary>
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository userRepository;
        private readonly ILogger logger;
        private readonly IUserStatusService userStatusService;
        private readonly IAvatarService avatarService;
        private readonly IChatService chatService;
        private readonly IWebSocketConnectionManager connectionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersController"/> class.
        /// </summary>
        /// <param name="userRepository">User repository.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="userStatusService">User status service.</param>
        /// <param name="avatarService">Avatar service.</param>
        /// <param name="chatService">Chat service.</param>
        /// <param name="connectionManager">WebSocket connection manager.</param>
        public UsersController(
            IUserRepository userRepository,
            ILogger logger,
            IUserStatusService userStatusService,
            IAvatarService avatarService,
            IChatService chatService,
            IWebSocketConnectionManager connectionManager)
        {
            this.userRepository = userRepository;
            this.logger = logger;
            this.userStatusService = userStatusService;
            this.avatarService = avatarService;
            this.chatService = chatService;
            this.connectionManager = connectionManager;
        }

        /// <summary>
        /// Gets all available users for participant selection.
        /// Excludes the current authenticated user from the results.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of user DTOs.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            this.logger.Information("Getting available users for user {UserId}", userId);

            var allUsers = await this.userRepository.GetAllAsync(cancellationToken);
            var filteredUsers = allUsers.Where(u => u.Id != userId);

            var userDtos = new List<UserDto>();
            foreach (var u in filteredUsers)
            {
                // Get status from Redis instead of Oracle (Invisible appears as Offline to others)
                var status = await this.userStatusService.GetPublicStatusAsync(u.Id, cancellationToken);
                userDtos.Add(new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    DisplayName = u.DisplayName,
                    AvatarUrl = u.AvatarUrl,
                    Status = status,
                    LastSeenAt = u.LastSeenAt,
                });
            }

            return this.Ok(userDtos);
        }

        /// <summary>
        /// Updates the authenticated user's profile.
        /// </summary>
        /// <param name="request">The update request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated user DTO.</returns>
        [HttpPut("profile")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.DisplayName))
            {
                return this.BadRequest(new { error = "Display name cannot be empty" });
            }

            this.logger.Information("Updating profile for user {UserId}", userId);

            var user = await this.userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return this.NotFound();
            }

            user.DisplayName = request.DisplayName;

            await this.userRepository.UpdateAsync(user, cancellationToken);

            // Get current status
            var status = await this.userStatusService.GetStatusAsync(user.Id, cancellationToken);

            return this.Ok(new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                Status = status,
                LastSeenAt = user.LastSeenAt,
            });
        }

        /// <summary>
        /// Gets the authenticated user's current presence status (including Invisible).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The status update DTO.</returns>
        [HttpGet("status")]
        [ProducesResponseType(typeof(StatusUpdateDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<StatusUpdateDto>> GetMyStatus(CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var status = await this.userStatusService.GetStatusAsync(userId, cancellationToken);
            return this.Ok(new StatusUpdateDto
            {
                UserId = userId,
                Status = status,
            });
        }

        /// <summary>
        /// Updates the authenticated user's presence status (Online or Invisible).
        /// Invisible users appear Offline to everyone else.
        /// </summary>
        /// <param name="request">The status update request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The applied status.</returns>
        [HttpPut("status")]
        [ProducesResponseType(typeof(StatusUpdateDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<StatusUpdateDto>> UpdateMyStatus(
            [FromBody] UpdateUserStatusRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            if (request == null ||
                (request.Status != Shared.Enums.UserStatus.Online &&
                 request.Status != Shared.Enums.UserStatus.Invisible))
            {
                return this.BadRequest(new { error = "Status must be Online or Invisible." });
            }

            var isInvisible = request.Status == Shared.Enums.UserStatus.Invisible;
            await this.userStatusService.SetInvisiblePreferenceAsync(userId, isInvisible, cancellationToken);
            await this.userStatusService.SetStatusAsync(userId, request.Status, cancellationToken);

            // Others always see Offline when Invisible; Online when Online.
            var publicStatus = isInvisible ? Shared.Enums.UserStatus.Offline : Shared.Enums.UserStatus.Online;
            await this.BroadcastStatusToChatPartnersAsync(userId, publicStatus, cancellationToken);

            this.logger.Information("User {UserId} set status to {Status}", userId, request.Status);

            return this.Ok(new StatusUpdateDto
            {
                UserId = userId,
                Status = request.Status,
            });
        }

        /// <summary>
        /// Uploads a new avatar for the authenticated user.
        /// </summary>
        /// <param name="file">The avatar image file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The new avatar URL.</returns>
        [HttpPost("avatar/upload")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(413)]
        public async Task<ActionResult<UserDto>> UploadAvatarAsync(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            if (file == null || file.Length == 0)
            {
                return this.BadRequest("No file provided.");
            }

            // Validate file type - only images
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                return this.BadRequest($"File type not allowed: {extension}. Allowed types: {string.Join(", ", allowedExtensions)}");
            }

            // Validate file size - max 10MB before compression
            const long maxSizeBytes = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxSizeBytes)
            {
                return this.StatusCode(
                    StatusCodes.Status413PayloadTooLarge,
                    $"File too large. Maximum size: {FileHelper.FormatFileSize(maxSizeBytes)}");
            }

            try
            {
                using var stream = file.OpenReadStream();
                var avatarUrl = await this.avatarService.SaveAvatarAsync(
                    userId,
                    file.FileName,
                    stream,
                    cancellationToken);

                // Update user's avatar URL in database
                var user = await this.userRepository.GetByIdAsync(userId, cancellationToken);
                if (user != null)
                {
                    user.AvatarUrl = avatarUrl;
                    await this.userRepository.UpdateAsync(user, cancellationToken);

                    // Get current status
                    var status = await this.userStatusService.GetStatusAsync(user.Id, cancellationToken);

                    // Return updated user DTO
                    var userDto = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        DisplayName = user.DisplayName,
                        AvatarUrl = user.AvatarUrl,
                        Status = status,
                        LastSeenAt = user.LastSeenAt,
                    };

                    this.logger.Information("Avatar uploaded for user {UserId}: {AvatarUrl}", userId, avatarUrl);

                    // Broadcast avatar update to all users who have chats with this user
                    _ = Task.Run(async () =>
                    {
                        await WebSocketHandler.BroadcastAvatarUpdateAsync(
                            userId,
                            avatarUrl,
                            this.chatService,
                            this.connectionManager,
                            this.logger);
                    });

                    return this.Ok(userDto);
                }

                this.logger.Warning("User not found after avatar upload: {UserId}", userId);
                return this.StatusCode(StatusCodes.Status500InternalServerError, "User not found after avatar upload");
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to upload avatar for user {UserId}", userId);
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Failed to upload avatar.");
            }
        }

        /// <summary>
        /// Gets an avatar image by user ID.
        /// </summary>
        /// <param name="userId">The user ID. Use "default" for default avatar.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The avatar image file.</returns>
        [HttpGet("avatar/{userId}")]
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetAvatarAsync(
            string userId,
            CancellationToken cancellationToken)
        {
            try
            {
                Stream stream;
                if (userId == "default")
                {
                    stream = await this.avatarService.GetDefaultAvatarStreamAsync(cancellationToken);
                }
                else
                {
                    var avatarStream = await this.avatarService.GetAvatarStreamAsync(userId, cancellationToken);
                    if (avatarStream == null)
                    {
                        // Fallback to default avatar
                        stream = await this.avatarService.GetDefaultAvatarStreamAsync(cancellationToken);
                    }
                    else
                    {
                        stream = avatarStream;
                    }
                }

                return this.File(stream, "image/jpeg", $"avatar_{userId}.jpg");
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to get avatar for user {UserId}", userId);
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve avatar.");
            }
        }

        private async Task BroadcastStatusToChatPartnersAsync(
            string userId,
            Shared.Enums.UserStatus publicStatus,
            CancellationToken cancellationToken)
        {
            try
            {
                var options = Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                var statusUpdate = new StatusUpdateDto
                {
                    UserId = userId,
                    Status = publicStatus,
                };
                var envelope = new WebSocketMessageEnvelope
                {
                    Type = Shared.Enums.WebSocketMessageType.StatusUpdate,
                    Payload = System.Text.Json.JsonSerializer.SerializeToElement(statusUpdate, options),
                };
                var messageJson = System.Text.Json.JsonSerializer.Serialize(envelope, options);

                var userChats = await this.chatService.GetUserChatsAsync(userId, cancellationToken);
                var recipientIds = new HashSet<string>();
                foreach (var chat in userChats)
                {
                    foreach (var participantId in chat.ParticipantIds)
                    {
                        if (participantId != userId)
                        {
                            recipientIds.Add(participantId);
                        }
                    }
                }

                var tasks = recipientIds.Select(id =>
                    this.connectionManager.BroadcastToUserAsync(id, messageJson, cancellationToken));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to broadcast status update for user {UserId}", userId);
            }
        }
    }
}
