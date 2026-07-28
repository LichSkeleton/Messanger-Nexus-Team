namespace NexusTeam.Server.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using Serilog;

    /// <summary>
    /// Controller for chat-related endpoints.
    /// </summary>
    [ApiController]
    [Route("api/chats")]
    public class ChatsController : ControllerBase
    {
        private readonly IChatService chatService;
        private readonly IMessageService messageService;
        private readonly IWebSocketConnectionManager connectionManager;
        private readonly IUserStatusService userStatusService;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatsController"/> class.
        /// </summary>
        /// <param name="chatService">Chat service.</param>
        /// <param name="messageService">Message service.</param>
        /// <param name="connectionManager">WebSocket connection manager.</param>
        /// <param name="userStatusService">User status service.</param>
        /// <param name="logger">Logger.</param>
        public ChatsController(
            IChatService chatService,
            IMessageService messageService,
            IWebSocketConnectionManager connectionManager,
            IUserStatusService userStatusService,
            ILogger logger)
        {
            this.chatService = chatService;
            this.messageService = messageService;
            this.connectionManager = connectionManager;
            this.userStatusService = userStatusService;
            this.logger = logger;
        }

        /// <summary>
        /// Gets all chats for the authenticated user.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of chat DTOs.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ChatDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<ChatDto>>> GetChats(CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var chats = await this.chatService.GetUserChatsAsync(userId, cancellationToken);
            return this.Ok(chats);
        }

        /// <summary>
        /// Gets a specific chat by ID.
        /// </summary>
        /// <param name="id">The chat ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The chat DTO.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ChatDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ChatDto>> GetChat(string id, CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var chat = await this.chatService.GetChatByIdAsync(id, userId, cancellationToken);
            if (chat == null)
            {
                return this.NotFound();
            }

            return this.Ok(chat);
        }

        /// <summary>
        /// Gets messages for a specific chat.
        /// </summary>
        /// <param name="id">The chat ID.</param>
        /// <param name="limit">Maximum number of messages to retrieve.</param>
        /// <param name="offset">Number of messages to skip.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of message DTOs.</returns>
        [HttpGet("{id}/messages")]
        [ProducesResponseType(typeof(IEnumerable<MessageDto>), 200)]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetChatMessages(
            string id,
            [FromQuery] int limit = 50,
            [FromQuery] int offset = 0,
            CancellationToken cancellationToken = default)
        {
            if (limit <= 0 || limit > 100)
            {
                limit = 50;
            }

            if (offset < 0)
            {
                offset = 0;
            }

            var messages = await this.messageService.GetChatMessagesAsync(id, limit, offset, cancellationToken);
            return this.Ok(messages);
        }

        /// <summary>
        /// Sends a message to a specific chat (HTTP endpoint for attachments).
        /// </summary>
        /// <param name="id">The chat ID.</param>
        /// <param name="request">The send message request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created message DTO.</returns>
        [HttpPost("{id}/messages")]
        [ProducesResponseType(typeof(MessageDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<MessageDto>> SendMessage(
            string id,
            [FromBody] SendMessageRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            request.ChatId = id;
            var message = await this.messageService.SendMessageAsync(request, userId, cancellationToken);

            // Broadcast message to all chat participants via WebSocket
            try
            {
                var chat = await this.chatService.GetChatByIdAsync(id, userId, cancellationToken);
                if (chat != null)
                {
                    var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                    var envelope = new WebSocketMessageEnvelope
                    {
                        Type = WebSocketMessageType.NewMessage,
                        MessageId = message.Id,
                        Payload = JsonSerializer.SerializeToElement(message, options),
                    };

                    var messageJson = JsonSerializer.Serialize(envelope, options);

                    // Send to all participants
                    var broadcastTasks = new List<Task>();
                    foreach (var participantId in chat.ParticipantIds)
                    {
                        broadcastTasks.Add(this.connectionManager.BroadcastToUserAsync(
                            participantId,
                            messageJson,
                            cancellationToken));
                    }

                    await Task.WhenAll(broadcastTasks);
                    this.logger.Debug("Message {MessageId} broadcasted to {Count} participants in chat {ChatId}", message.Id, chat.ParticipantIds.Count, id);
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the request - message was already saved
                this.logger.Warning(ex, "Failed to broadcast message {MessageId} via WebSocket", message.Id);
            }

            return this.Created($"/api/chats/{id}/messages", message);
        }

        /// <summary>
        /// Creates a new chat.
        /// </summary>
        /// <param name="request">The create chat request data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created chat DTO.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ChatDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<ChatDto>> CreateChat([FromBody] CreateChatRequest? request, CancellationToken cancellationToken)
        {
            this.logger.Information("POST /api/chats - Creating new chat {ChatName}", request?.Name ?? "unknown");

            // Check for null request
            if (request == null)
            {
                this.logger.Warning("Null request body for chat creation");
                return this.BadRequest(new { error = "Request body is required." });
            }

            // Check authentication
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                this.logger.Warning("Unauthorized chat creation attempt");
                return this.Unauthorized();
            }

            // Check ModelState for FluentValidation errors
            if (!this.ModelState.IsValid)
            {
                this.logger.Warning("Validation failed for chat creation: {ValidationErrors}", this.ModelState);
                return this.BadRequest(this.ModelState);
            }

            try
            {
                var chat = await this.chatService.CreateChatAsync(request, userId, cancellationToken);
                this.logger.Information("Chat {ChatId} created successfully by user {UserId}", chat.Id, userId);

                await this.NotifyChatCreatedAsync(chat, userId, cancellationToken);
                await this.SyncParticipantPresenceAsync(chat, cancellationToken);

                return this.CreatedAtAction(nameof(this.GetChat), new { id = chat.Id }, chat);
            }
            catch (Shared.Exceptions.DuplicateChatException ex)
            {
                this.logger.Warning("Duplicate chat name: {Message}", ex.Message);
                return this.Conflict(new { error = ex.Message });
            }
            catch (Shared.Exceptions.ValidationException ex)
            {
                this.logger.Warning("Validation error during chat creation: {Message}", ex.Message);
                return this.BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Adds a reaction to a message.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="messageId">The message ID.</param>
        /// <param name="request">The add reaction request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated message DTO.</returns>
        [HttpPost("{chatId}/messages/{messageId}/reactions")]
        [ProducesResponseType(typeof(MessageDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<MessageDto>> AddReaction(
            string chatId,
            string messageId,
            [FromBody] AddReactionRequest request,
            CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            try
            {
                var message = await this.messageService.AddReactionAsync(messageId, request.Emoji, userId, cancellationToken);

                // Broadcast reaction update via WebSocket
                try
                {
                    var chat = await this.chatService.GetChatByIdAsync(chatId, userId, cancellationToken);
                    if (chat != null)
                    {
                        var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                        var envelope = new WebSocketMessageEnvelope
                        {
                            Type = WebSocketMessageType.MessageReaction,
                            MessageId = message.Id,
                            Payload = JsonSerializer.SerializeToElement(message, options),
                        };

                        var messageJson = JsonSerializer.Serialize(envelope, options);

                        // Send to all participants
                        var broadcastTasks = new List<Task>();
                        foreach (var participantId in chat.ParticipantIds)
                        {
                            broadcastTasks.Add(this.connectionManager.BroadcastToUserAsync(
                                participantId,
                                messageJson,
                                cancellationToken));
                        }

                        await Task.WhenAll(broadcastTasks);
                        this.logger.Debug("Reaction update for message {MessageId} broadcasted to {Count} participants", message.Id, chat.ParticipantIds.Count);
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail the request
                    this.logger.Warning(ex, "Failed to broadcast reaction update for message {MessageId} via WebSocket", messageId);
                }

                return this.Ok(message);
            }
            catch (Shared.Exceptions.ValidationException ex)
            {
                this.logger.Warning("Validation error adding reaction: {Message}", ex.Message);
                return this.BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Removes a reaction from a message.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="messageId">The message ID.</param>
        /// <param name="emoji">The emoji of the reaction to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated message DTO.</returns>
        [HttpDelete("{chatId}/messages/{messageId}/reactions/{emoji}")]
        [ProducesResponseType(typeof(MessageDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<MessageDto>> RemoveReaction(
            string chatId,
            string messageId,
            string emoji,
            CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            try
            {
                // URL decode emoji
                emoji = Uri.UnescapeDataString(emoji);

                var message = await this.messageService.RemoveReactionAsync(messageId, emoji, userId, cancellationToken);

                // Broadcast reaction update via WebSocket
                try
                {
                    var chat = await this.chatService.GetChatByIdAsync(chatId, userId, cancellationToken);
                    if (chat != null)
                    {
                        var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                        var envelope = new WebSocketMessageEnvelope
                        {
                            Type = WebSocketMessageType.MessageReaction,
                            MessageId = message.Id,
                            Payload = JsonSerializer.SerializeToElement(message, options),
                        };

                        var messageJson = JsonSerializer.Serialize(envelope, options);

                        // Send to all participants
                        var broadcastTasks = new List<Task>();
                        foreach (var participantId in chat.ParticipantIds)
                        {
                            broadcastTasks.Add(this.connectionManager.BroadcastToUserAsync(
                                participantId,
                                messageJson,
                                cancellationToken));
                        }

                        await Task.WhenAll(broadcastTasks);
                        this.logger.Debug("Reaction removal for message {MessageId} broadcasted to {Count} participants", message.Id, chat.ParticipantIds.Count);
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail the request
                    this.logger.Warning(ex, "Failed to broadcast reaction removal for message {MessageId} via WebSocket", messageId);
                }

                return this.Ok(message);
            }
            catch (Shared.Exceptions.ValidationException ex)
            {
                this.logger.Warning("Validation error removing reaction: {Message}", ex.Message);
                return this.BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a chat and all its associated data (messages, attachments).
        /// </summary>
        /// <param name="id">The chat ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>No content if successful.</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeleteChat(string id, CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            try
            {
                await this.chatService.DeleteChatAsync(id, userId, cancellationToken);
                this.logger.Information("Chat {ChatId} deleted successfully by user {UserId}", id, userId);

                // Broadcast chat deletion to all participants via WebSocket
                try
                {
                    var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                    var envelope = new WebSocketMessageEnvelope
                    {
                        Type = WebSocketMessageType.ChatDeleted,
                        Payload = JsonSerializer.SerializeToElement(new { ChatId = id }, options),
                    };

                    var messageJson = JsonSerializer.Serialize(envelope, options);

                    // Since we can't get participants from deleted chat, broadcast to requesting user at least
                    await this.connectionManager.BroadcastToUserAsync(userId, messageJson, cancellationToken);
                }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "Failed to broadcast chat deletion for chat {ChatId} via WebSocket", id);
                }

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
            catch (Shared.Exceptions.DomainException ex)
            {
                this.logger.Error(ex, "Error deleting chat {ChatId}", id);
                return this.StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Leaves a group chat. If the caller is the last participant, the group is deleted.
        /// </summary>
        /// <param name="id">The chat ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>No content if successful.</returns>
        [HttpPost("{id}/leave")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> LeaveChat(string id, CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            try
            {
                await this.chatService.LeaveChatAsync(id, userId, cancellationToken);
                this.logger.Information("User {UserId} left chat {ChatId}", userId, id);
                return this.NoContent();
            }
            catch (Shared.Exceptions.NotFoundException)
            {
                return this.NotFound();
            }
            catch (Shared.Exceptions.UnauthorizedException ex)
            {
                return this.Unauthorized(new { error = ex.Message });
            }
            catch (Shared.Exceptions.ValidationException ex)
            {
                return this.BadRequest(new { error = ex.Message });
            }
            catch (Shared.Exceptions.DomainException ex)
            {
                this.logger.Error(ex, "Error leaving chat {ChatId}", id);
                return this.StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Updates group chat properties (owner only).
        /// </summary>
        /// <param name="id">The chat ID.</param>
        /// <param name="request">Update request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated chat.</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ChatDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ChatDto>> UpdateChat(
            string id,
            [FromBody] UpdateChatRequest? request,
            CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            if (request == null)
            {
                return this.BadRequest(new { error = "Request body is required." });
            }

            try
            {
                var chat = await this.chatService.UpdateChatAsync(id, userId, request, cancellationToken);
                await this.NotifyChatUpdatedAsync(chat, userId, cancellationToken);
                return this.Ok(chat);
            }
            catch (Shared.Exceptions.NotFoundException)
            {
                return this.NotFound();
            }
            catch (Shared.Exceptions.UnauthorizedException ex)
            {
                return this.Unauthorized(new { error = ex.Message });
            }
            catch (Shared.Exceptions.ValidationException ex)
            {
                return this.BadRequest(new { error = ex.Message });
            }
            catch (Shared.Exceptions.DomainException ex)
            {
                this.logger.Error(ex, "Error updating chat {ChatId}", id);
                return this.StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Uploads a new avatar for a group chat (owner only).
        /// </summary>
        /// <param name="id">The chat ID.</param>
        /// <param name="file">The avatar image file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated chat.</returns>
        [HttpPost("{id}/avatar")]
        [ProducesResponseType(typeof(ChatDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(413)]
        public async Task<ActionResult<ChatDto>> UploadChatAvatar(
            string id,
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
                return this.BadRequest(new { error = "Avatar file is required." });
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return this.StatusCode(StatusCodes.Status413PayloadTooLarge, new { error = "Avatar must be 5MB or smaller." });
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var chat = await this.chatService.UploadChatAvatarAsync(
                    id,
                    userId,
                    file.FileName,
                    stream,
                    cancellationToken);
                await this.NotifyChatUpdatedAsync(chat, userId, cancellationToken);
                return this.Ok(chat);
            }
            catch (Shared.Exceptions.NotFoundException)
            {
                return this.NotFound();
            }
            catch (Shared.Exceptions.UnauthorizedException ex)
            {
                return this.Unauthorized(new { error = ex.Message });
            }
            catch (Shared.Exceptions.ValidationException ex)
            {
                return this.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error uploading avatar for chat {ChatId}", id);
                return this.StatusCode(500, new { error = "Failed to upload avatar." });
            }
        }

        /// <summary>
        /// Notifies other participants that a chat was created so they can show it immediately.
        /// </summary>
        private async Task NotifyChatCreatedAsync(ChatDto chat, string creatorUserId, CancellationToken cancellationToken)
        {
            try
            {
                var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                var envelope = new WebSocketMessageEnvelope
                {
                    Type = WebSocketMessageType.ChatCreated,
                    Payload = JsonSerializer.SerializeToElement(chat, options),
                };
                var messageJson = JsonSerializer.Serialize(envelope, options);

                var broadcastTasks = new List<Task>();
                foreach (var participantId in chat.ParticipantIds)
                {
                    // Creator already has the chat from the HTTP response.
                    if (participantId == creatorUserId)
                    {
                        continue;
                    }

                    broadcastTasks.Add(this.connectionManager.BroadcastToUserAsync(
                        participantId,
                        messageJson,
                        cancellationToken));
                }

                await Task.WhenAll(broadcastTasks);
                this.logger.Debug(
                    "ChatCreated broadcasted for chat {ChatId} to {Count} participants",
                    chat.Id,
                    chat.ParticipantIds.Count - 1);
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to broadcast ChatCreated for chat {ChatId}", chat.Id);
            }
        }

        /// <summary>
        /// Notifies other participants that chat metadata (name/avatar) was updated.
        /// </summary>
        private async Task NotifyChatUpdatedAsync(ChatDto chat, string editorUserId, CancellationToken cancellationToken)
        {
            try
            {
                var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                var envelope = new WebSocketMessageEnvelope
                {
                    Type = WebSocketMessageType.ChatUpdated,
                    Payload = JsonSerializer.SerializeToElement(chat, options),
                };
                var messageJson = JsonSerializer.Serialize(envelope, options);

                var broadcastTasks = new List<Task>();
                foreach (var participantId in chat.ParticipantIds)
                {
                    // Editor already applied the update from the HTTP response.
                    if (participantId == editorUserId)
                    {
                        continue;
                    }

                    broadcastTasks.Add(this.connectionManager.BroadcastToUserAsync(
                        participantId,
                        messageJson,
                        cancellationToken));
                }

                await Task.WhenAll(broadcastTasks);
                this.logger.Debug(
                    "ChatUpdated broadcasted for chat {ChatId} to {Count} participants",
                    chat.Id,
                    chat.ParticipantIds.Count - 1);
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to broadcast ChatUpdated for chat {ChatId}", chat.Id);
            }
        }

        /// <summary>
        /// Pushes each participant's current presence to the other participants after a chat is created.
        /// Without this, newly paired users never receive live Online/Offline until a later reconnect.
        /// </summary>
        private async Task SyncParticipantPresenceAsync(ChatDto chat, CancellationToken cancellationToken)
        {
            try
            {
                var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                var broadcastTasks = new List<Task>();

                foreach (var participantId in chat.ParticipantIds)
                {
                    var status = await this.userStatusService.GetPublicStatusAsync(participantId, cancellationToken);
                    var statusUpdate = new StatusUpdateDto
                    {
                        UserId = participantId,
                        Status = status,
                    };
                    var envelope = new WebSocketMessageEnvelope
                    {
                        Type = WebSocketMessageType.StatusUpdate,
                        Payload = JsonSerializer.SerializeToElement(statusUpdate, options),
                    };
                    var messageJson = JsonSerializer.Serialize(envelope, options);

                    foreach (var recipientId in chat.ParticipantIds)
                    {
                        if (recipientId == participantId)
                        {
                            continue;
                        }

                        broadcastTasks.Add(this.connectionManager.BroadcastToUserAsync(
                            recipientId,
                            messageJson,
                            cancellationToken));
                    }
                }

                await Task.WhenAll(broadcastTasks);
                this.logger.Debug("Presence synced for {Count} participants in chat {ChatId}", chat.ParticipantIds.Count, chat.Id);
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to sync presence after creating chat {ChatId}", chat.Id);
            }
        }
    }
}
