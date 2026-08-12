namespace NexusTeam.Server.Controllers
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Services;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using NexusTeam.Shared.Helpers;
    using Serilog;

    /// <summary>
    /// Controller for handling file attachments.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AttachmentsController : ControllerBase
    {
        private readonly IAttachmentService attachmentService;
        private readonly IMessageService messageService;
        private readonly IChatService chatService;
        private readonly IWebSocketConnectionManager connectionManager;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentsController"/> class.
        /// </summary>
        /// <param name="attachmentService">The attachment service.</param>
        /// <param name="messageService">The message service.</param>
        /// <param name="chatService">The chat service.</param>
        /// <param name="connectionManager">The WebSocket connection manager.</param>
        /// <param name="logger">The logger.</param>
        public AttachmentsController(
            IAttachmentService attachmentService,
            IMessageService messageService,
            IChatService chatService,
            IWebSocketConnectionManager connectionManager,
            ILogger logger)
        {
            this.attachmentService = attachmentService;
            this.messageService = messageService;
            this.chatService = chatService;
            this.connectionManager = connectionManager;
            this.logger = logger;
        }

        /// <summary>
        /// Uploads a file attachment for a message.
        /// </summary>
        /// <param name="file">The file to upload.</param>
        /// <param name="messageId">The message ID to attach to (can be linked later if not provided).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The uploaded attachment DTO.</returns>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(MessageAttachmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
        public async Task<ActionResult<MessageAttachmentDto>> UploadAttachmentAsync(
            IFormFile file,
            [FromForm] string messageId,
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

            // Validate file
            if (!FileHelper.IsAllowedFileType(file.FileName))
            {
                return this.BadRequest($"File type not allowed: {Path.GetExtension(file.FileName)}");
            }

            var attachmentType = FileHelper.GetAttachmentType(file.FileName);
            if (!FileHelper.IsValidFileSize(file.Length, attachmentType))
            {
                var maxSize = attachmentType == Shared.Enums.AttachmentType.Image
                    ? FileHelper.MaxImageSizeBytes
                    : FileHelper.MaxFileSizeBytes;

                return this.StatusCode(
                    StatusCodes.Status413PayloadTooLarge,
                    $"File too large. Maximum size: {FileHelper.FormatFileSize(maxSize)}");
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(messageId))
                {
                    await this.EnsureMessageParticipantAsync(messageId, userId, cancellationToken);
                }

                using var stream = file.OpenReadStream();
                var attachment = await this.attachmentService.SaveAttachmentAsync(
                    messageId,
                    file.FileName,
                    stream,
                    file.ContentType,
                    cancellationToken);

                this.logger.Information(
                    "File uploaded: {FileName} ({Size}) for message {MessageId}",
                    file.FileName,
                    FileHelper.FormatFileSize(file.Length),
                    messageId);

                // Broadcast updated message with attachments to all chat participants
                try
                {
                    var message = await this.messageService.GetMessageByIdAsync(messageId, cancellationToken);
                    if (message != null)
                    {
                        var chat = await this.chatService.GetChatByIdAsync(message.ChatId, "system", cancellationToken);
                        if (chat != null)
                        {
                            var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                            var envelope = new WebSocketMessageEnvelope
                            {
                                Type = WebSocketMessageType.EditMessage,
                                MessageId = message.Id,
                                Payload = JsonSerializer.SerializeToElement(message, options),
                            };

                            var messageJson = JsonSerializer.Serialize(envelope, options);

                            // Send to all participants
                            var broadcastTasks = new System.Collections.Generic.List<Task>();
                            foreach (var participantId in chat.ParticipantIds)
                            {
                                broadcastTasks.Add(this.connectionManager.BroadcastToUserAsync(
                                    participantId,
                                    messageJson,
                                    cancellationToken));
                            }

                            await Task.WhenAll(broadcastTasks);
                            this.logger.Debug(
                                "Updated message {MessageId} with attachment broadcasted to {Count} participants in chat {ChatId}",
                                message.Id,
                                chat.ParticipantIds.Count,
                                message.ChatId);
                        }
                    }
                }
                catch (Exception broadcastEx)
                {
                    // Log but don't fail the upload if broadcast fails
                    this.logger.Warning(broadcastEx, "Failed to broadcast updated message after attachment upload: {MessageId}", messageId);
                }

                return this.Ok(attachment);
            }
            catch (Shared.Exceptions.UnauthorizedException)
            {
                return this.Unauthorized();
            }
            catch (Shared.Exceptions.NotFoundException)
            {
                return this.NotFound();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to upload attachment: {FileName}", file.FileName);
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Failed to upload file.");
            }
        }

        /// <summary>
        /// Downloads an attachment file.
        /// </summary>
        /// <param name="attachmentId">The attachment ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The file stream.</returns>
        [HttpGet("download/{attachmentId}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadAttachmentAsync(
            string attachmentId,
            CancellationToken cancellationToken)
        {
            try
            {
                var attachment = await this.attachmentService.GetAttachmentAsync(attachmentId, cancellationToken);
                if (attachment == null)
                {
                    return this.NotFound("Attachment not found.");
                }

                var userId = this.HttpContext.Items["UserId"] as string;
                if (string.IsNullOrEmpty(userId))
                {
                    return this.Unauthorized();
                }

                if (!string.IsNullOrWhiteSpace(attachment.MessageId))
                {
                    await this.EnsureMessageParticipantAsync(attachment.MessageId, userId, cancellationToken);
                }

                var stream = await this.attachmentService.GetAttachmentStreamAsync(attachmentId, cancellationToken);
                if (stream == null)
                {
                    return this.NotFound("Attachment file not found.");
                }

                this.logger.Information("Attachment downloaded: {AttachmentId}", attachmentId);

                return this.File(stream, attachment.ContentType, attachment.FileName);
            }
            catch (Shared.Exceptions.UnauthorizedException)
            {
                return this.Unauthorized();
            }
            catch (Shared.Exceptions.NotFoundException)
            {
                return this.NotFound();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to download attachment: {AttachmentId}", attachmentId);
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Failed to download file.");
            }
        }

        /// <summary>
        /// Downloads a thumbnail for an attachment (for images).
        /// </summary>
        /// <param name="attachmentId">The attachment ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The thumbnail file stream.</returns>
        [HttpGet("thumbnail/{attachmentId}")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadThumbnailAsync(
            string attachmentId,
            CancellationToken cancellationToken)
        {
            try
            {
                var attachment = await this.attachmentService.GetAttachmentAsync(attachmentId, cancellationToken);
                if (attachment == null)
                {
                    return this.NotFound("Attachment not found.");
                }

                var userId = this.HttpContext.Items["UserId"] as string;
                if (string.IsNullOrEmpty(userId))
                {
                    return this.Unauthorized();
                }

                if (!string.IsNullOrWhiteSpace(attachment.MessageId))
                {
                    await this.EnsureMessageParticipantAsync(attachment.MessageId, userId, cancellationToken);
                }

                var stream = await this.attachmentService.GetThumbnailStreamAsync(attachmentId, cancellationToken);
                if (stream == null)
                {
                    return this.NotFound("Thumbnail not found.");
                }

                this.logger.Information("Thumbnail downloaded: {AttachmentId}", attachmentId);

                // Thumbnails are always JPEG
                return this.File(stream, "image/jpeg", $"{attachmentId}_thumb.jpg");
            }
            catch (Shared.Exceptions.UnauthorizedException)
            {
                return this.Unauthorized();
            }
            catch (Shared.Exceptions.NotFoundException)
            {
                return this.NotFound();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to download thumbnail: {AttachmentId}", attachmentId);
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Failed to download thumbnail.");
            }
        }

        /// <summary>
        /// Gets attachments for a specific message.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of attachments.</returns>
        [HttpGet("message/{messageId}")]
        [ProducesResponseType(typeof(MessageAttachmentDto[]), StatusCodes.Status200OK)]
        public async Task<ActionResult<MessageAttachmentDto[]>> GetMessageAttachmentsAsync(
            string messageId,
            CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            try
            {
                await this.EnsureMessageParticipantAsync(messageId, userId, cancellationToken);
                var attachments = await this.attachmentService.GetMessageAttachmentsAsync(messageId, cancellationToken);
                return this.Ok(attachments);
            }
            catch (Shared.Exceptions.UnauthorizedException)
            {
                return this.Unauthorized();
            }
            catch (Shared.Exceptions.NotFoundException)
            {
                return this.NotFound();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to get attachments for message: {MessageId}", messageId);
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve attachments.");
            }
        }

        /// <summary>
        /// Updates an existing attachment file.
        /// </summary>
        /// <param name="attachmentId">The attachment ID.</param>
        /// <param name="file">The new file to upload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated attachment DTO.</returns>
        [HttpPut("{attachmentId}")]
        [ProducesResponseType(typeof(MessageAttachmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
        public async Task<ActionResult<MessageAttachmentDto>> UpdateAttachmentAsync(
            string attachmentId,
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

            // Validate file
            if (!FileHelper.IsAllowedFileType(file.FileName))
            {
                return this.BadRequest($"File type not allowed: {Path.GetExtension(file.FileName)}");
            }

            var attachmentType = FileHelper.GetAttachmentType(file.FileName);
            if (!FileHelper.IsValidFileSize(file.Length, attachmentType))
            {
                var maxSize = attachmentType == Shared.Enums.AttachmentType.Image
                    ? FileHelper.MaxImageSizeBytes
                    : FileHelper.MaxFileSizeBytes;

                return this.StatusCode(
                    StatusCodes.Status413PayloadTooLarge,
                    $"File too large. Maximum size: {FileHelper.FormatFileSize(maxSize)}");
            }

            try
            {
                await this.EnsureAttachmentParticipantAsync(attachmentId, userId, cancellationToken);

                using var stream = file.OpenReadStream();
                var attachment = await this.attachmentService.UpdateAttachmentAsync(
                    attachmentId,
                    stream,
                    file.ContentType,
                    cancellationToken);

                this.logger.Information(
                    "File updated: {FileName} ({Size}) for attachment {AttachmentId}",
                    file.FileName,
                    FileHelper.FormatFileSize(file.Length),
                    attachmentId);

                return this.Ok(attachment);
            }
            catch (Shared.Exceptions.UnauthorizedException)
            {
                return this.Unauthorized();
            }
            catch (Shared.Exceptions.NotFoundException)
            {
                return this.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                this.logger.Warning(ex, "Attachment not found: {AttachmentId}", attachmentId);
                return this.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to update attachment: {AttachmentId}", attachmentId);
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Failed to update file.");
            }
        }

        /// <summary>
        /// Deletes an attachment.
        /// </summary>
        /// <param name="attachmentId">The attachment ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Success status.</returns>
        [HttpDelete("{attachmentId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAttachmentAsync(
            string attachmentId,
            CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            try
            {
                await this.EnsureAttachmentParticipantAsync(attachmentId, userId, cancellationToken);

                var success = await this.attachmentService.DeleteAttachmentAsync(attachmentId, cancellationToken);
                if (!success)
                {
                    return this.NotFound("Attachment not found.");
                }

                this.logger.Information("Attachment deleted: {AttachmentId}", attachmentId);

                return this.NoContent();
            }
            catch (Shared.Exceptions.UnauthorizedException)
            {
                return this.Unauthorized();
            }
            catch (Shared.Exceptions.NotFoundException)
            {
                return this.NotFound();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to delete attachment: {AttachmentId}", attachmentId);
                return this.StatusCode(StatusCodes.Status500InternalServerError, "Failed to delete attachment.");
            }
        }

        private async Task EnsureMessageParticipantAsync(string messageId, string userId, CancellationToken cancellationToken)
        {
            var message = await this.messageService.GetMessageByIdAsync(messageId, cancellationToken);
            if (message == null)
            {
                throw new Shared.Exceptions.NotFoundException($"Message with ID '{messageId}' not found.");
            }

            var chat = await this.chatService.GetChatByIdAsync(message.ChatId, userId, cancellationToken);
            if (chat == null)
            {
                throw new Shared.Exceptions.UnauthorizedException("You are not a participant of this chat.");
            }
        }

        private async Task EnsureAttachmentParticipantAsync(string attachmentId, string userId, CancellationToken cancellationToken)
        {
            var attachment = await this.attachmentService.GetAttachmentAsync(attachmentId, cancellationToken);
            if (attachment == null)
            {
                throw new Shared.Exceptions.NotFoundException($"Attachment with ID '{attachmentId}' not found.");
            }

            if (!string.IsNullOrWhiteSpace(attachment.MessageId))
            {
                await this.EnsureMessageParticipantAsync(attachment.MessageId, userId, cancellationToken);
            }
        }
    }
}
