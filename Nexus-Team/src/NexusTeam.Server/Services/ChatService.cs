namespace NexusTeam.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Exceptions;
    using NexusTeam.Shared.Models;
    using Serilog;

    /// <summary>
    /// Service for chat operations.
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly IChatRepository chatRepository;
        private readonly IUserRepository userRepository;
        private readonly IMessageRepository messageRepository;
        private readonly IMessageAttachmentRepository attachmentRepository;
        private readonly IChatFolderRepository folderRepository;
        private readonly IIdGenerator idGenerator;
        private readonly IClock clock;
        private readonly ILogger logger;
        private readonly IUserStatusService userStatusService;
        private readonly IAvatarService avatarService;
        private readonly string storagePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatService"/> class.
        /// </summary>
        /// <param name="chatRepository">Chat repository.</param>
        /// <param name="userRepository">User repository.</param>
        /// <param name="messageRepository">Message repository.</param>
        /// <param name="attachmentRepository">Attachment repository.</param>
        /// <param name="folderRepository">Folder repository.</param>
        /// <param name="idGenerator">ID generator.</param>
        /// <param name="clock">Clock for timestamps.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="userStatusService">User status service.</param>
        /// <param name="avatarService">Avatar service for group avatars.</param>
        public ChatService(
            IChatRepository chatRepository,
            IUserRepository userRepository,
            IMessageRepository messageRepository,
            IMessageAttachmentRepository attachmentRepository,
            IChatFolderRepository folderRepository,
            IIdGenerator idGenerator,
            IClock clock,
            ILogger logger,
            IUserStatusService userStatusService,
            IAvatarService avatarService)
        {
            this.chatRepository = chatRepository;
            this.userRepository = userRepository;
            this.messageRepository = messageRepository;
            this.attachmentRepository = attachmentRepository;
            this.folderRepository = folderRepository;
            this.idGenerator = idGenerator;
            this.clock = clock;
            this.logger = logger;
            this.userStatusService = userStatusService;
            this.avatarService = avatarService;

            // Set storage path to match AttachmentService
            this.storagePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Storage",
                "Attachments");
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ChatDto>> GetUserChatsAsync(string userId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Getting chats for user {UserId}", userId);
            var chats = await this.chatRepository.GetByUserIdAsync(userId, cancellationToken);
            var chatDtos = new List<ChatDto>();

            foreach (var chat in chats)
            {
                var chatDto = this.MapToDto(chat);

                // Populate participant details for all chat types
                if (chat.ParticipantIds.Any())
                {
                    var participants = new List<UserDto>();
                    foreach (var participantId in chat.ParticipantIds)
                    {
                        var user = await this.userRepository.GetByIdAsync(participantId, cancellationToken);
                        if (user != null)
                        {
                            participants.Add(await this.MapUserToDtoAsync(user, cancellationToken));
                        }
                    }

                    chatDto.Participants = participants;

                    // For direct messages, set the chat name to the other participant's display name (or username)
                    if (chat.Type == NexusTeam.Shared.Enums.ChatType.DirectMessage)
                    {
                        var otherParticipant = participants.FirstOrDefault(p => p.Id != userId);
                        if (otherParticipant != null)
                        {
                            chatDto.Name = !string.IsNullOrWhiteSpace(otherParticipant.DisplayName)
                                ? otherParticipant.DisplayName
                                : otherParticipant.Username;
                        }
                    }
                }

                chatDtos.Add(chatDto);
            }

            return chatDtos;
        }

        /// <inheritdoc/>
        public async Task<ChatDto?> GetChatByIdAsync(string chatId, string userId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Getting chat {ChatId}", chatId);
            var chat = await this.chatRepository.GetByIdAsync(chatId, cancellationToken);
            if (chat == null)
            {
                return null;
            }

            var chatDto = this.MapToDto(chat);

            // Populate participant details for all chat types
            if (chat.ParticipantIds.Any())
            {
                var participants = new List<UserDto>();
                foreach (var participantId in chat.ParticipantIds)
                {
                    var user = await this.userRepository.GetByIdAsync(participantId, cancellationToken);
                    if (user != null)
                    {
                        participants.Add(await this.MapUserToDtoAsync(user, cancellationToken));
                    }
                }

                chatDto.Participants = participants;

                // For direct messages, set the chat name to the other participant's display name (or username)
                if (chat.Type == NexusTeam.Shared.Enums.ChatType.DirectMessage)
                {
                    var otherParticipant = participants.FirstOrDefault(p => p.Id != userId);
                    if (otherParticipant != null)
                    {
                        chatDto.Name = !string.IsNullOrWhiteSpace(otherParticipant.DisplayName)
                            ? otherParticipant.DisplayName
                            : otherParticipant.Username;
                    }
                }
            }

            return chatDto;
        }

        /// <inheritdoc/>
        public async Task<ChatDto> CreateChatAsync(CreateChatRequest request, string creatorUserId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Creating chat {ChatName} for user {UserId}", request.Name, creatorUserId);

            // Check for duplicate chat name for this user
            var nameExists = await this.chatRepository.ChatNameExistsForUserAsync(request.Name, creatorUserId, cancellationToken);
            if (nameExists)
            {
                this.logger.Warning("Chat name {ChatName} already exists for user {UserId}", request.Name, creatorUserId);
                throw new DuplicateChatException(request.Name);
            }

            // Deduplicate participant IDs and ensure creator is included
            var participantIds = request.ParticipantIds.Distinct().ToList();
            if (!participantIds.Contains(creatorUserId))
            {
                participantIds.Add(creatorUserId);
            }

            // Validate that all participants exist
            var participantUsers = new List<User>();
            foreach (var participantId in participantIds)
            {
                var user = await this.userRepository.GetByIdAsync(participantId, cancellationToken);
                if (user == null)
                {
                    this.logger.Warning("Participant user {UserId} not found during chat creation", participantId);
                    throw new ValidationException($"User with ID '{participantId}' does not exist.");
                }

                participantUsers.Add(user);
            }

            // Create the chat entity
            var now = this.clock.UtcNow;
            var chat = new Chat
            {
                Id = this.idGenerator.GenerateId(),
                Type = request.Type,
                Name = request.Name,
                Description = request.Description,
                AvatarUrl = request.AvatarUrl,
                ParticipantIds = participantIds,
                CreatedBy = creatorUserId,
                CreatedAt = now,
                UpdatedAt = now,
                LastMessageAt = null,
            };

            // Persist the chat
            await this.chatRepository.CreateAsync(chat, cancellationToken);
            this.logger.Information("Chat {ChatId} created successfully by user {UserId}", chat.Id, creatorUserId);

            // Build the response DTO with participant details
            var chatDto = this.MapToDto(chat);
            var participantDtos = new List<UserDto>();
            foreach (var participant in participantUsers)
            {
                participantDtos.Add(await this.MapUserToDtoAsync(participant, cancellationToken));
            }

            chatDto.Participants = participantDtos;

            return chatDto;
        }

        /// <inheritdoc/>
        public async Task LeaveChatAsync(string chatId, string userId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("User {UserId} leaving chat {ChatId}", userId, chatId);

            var chat = await this.chatRepository.GetByIdAsync(chatId, cancellationToken);
            if (chat == null)
            {
                throw new NotFoundException($"Chat with ID '{chatId}' not found.");
            }

            if (chat.Type == NexusTeam.Shared.Enums.ChatType.DirectMessage)
            {
                throw new ValidationException("Cannot leave a direct message. Delete the chat instead.");
            }

            if (chat.ParticipantIds == null || !chat.ParticipantIds.Contains(userId))
            {
                throw new UnauthorizedException("You are not a participant of this chat.");
            }

            // Anyone else still in the group after this leave?
            var remainingOthers = chat.ParticipantIds
                .Where(id => !string.Equals(id, userId, StringComparison.Ordinal))
                .Distinct()
                .ToList();

            // Last person leaving — delete the empty group entirely.
            if (remainingOthers.Count == 0)
            {
                this.logger.Information(
                    "Last participant {UserId} leaving chat {ChatId}; deleting group",
                    userId,
                    chatId);
                await this.DeleteChatAsync(chatId, userId, cancellationToken);
                return;
            }

            await this.chatRepository.RemoveParticipantAsync(chatId, userId, cancellationToken);

            // Safety: if the chat somehow ended up empty, delete it.
            var refreshed = await this.chatRepository.GetByIdAsync(chatId, cancellationToken);
            if (refreshed == null || refreshed.ParticipantIds == null || refreshed.ParticipantIds.Count == 0)
            {
                if (refreshed != null)
                {
                    await this.ForceDeleteEmptyChatAsync(chatId, cancellationToken);
                }

                await this.folderRepository.RemoveChatFromUserFoldersAsync(chatId, userId, cancellationToken);
                this.logger.Information("User {UserId} left empty chat {ChatId}; chat deleted", userId, chatId);
                return;
            }

            // Transfer ownership if the owner left
            if (chat.CreatedBy == userId)
            {
                refreshed.CreatedBy = refreshed.ParticipantIds[0];
                refreshed.UpdatedAt = this.clock.UtcNow;
                await this.chatRepository.UpdateAsync(refreshed, cancellationToken);
                this.logger.Information(
                    "Ownership of chat {ChatId} transferred to {NewOwnerId}",
                    chatId,
                    refreshed.CreatedBy);
            }

            // Remove from this user's personal folders
            await this.folderRepository.RemoveChatFromUserFoldersAsync(chatId, userId, cancellationToken);

            this.logger.Information("User {UserId} left chat {ChatId}", userId, chatId);
        }

        /// <inheritdoc/>
        public async Task<ChatDto> UpdateChatAsync(
            string chatId,
            string userId,
            UpdateChatRequest request,
            CancellationToken cancellationToken = default)
        {
            this.logger.Information("User {UserId} updating chat {ChatId}", userId, chatId);

            var chat = await this.chatRepository.GetByIdAsync(chatId, cancellationToken);
            if (chat == null)
            {
                throw new NotFoundException($"Chat with ID '{chatId}' not found.");
            }

            if (chat.Type == NexusTeam.Shared.Enums.ChatType.DirectMessage)
            {
                throw new ValidationException("Cannot update a direct message chat.");
            }

            if (!chat.ParticipantIds.Contains(userId))
            {
                throw new UnauthorizedException("You are not a participant of this chat.");
            }

            if (chat.CreatedBy != userId)
            {
                throw new UnauthorizedException("Only the group owner can edit the group.");
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                chat.Name = request.Name.Trim();
            }

            if (request.Description != null)
            {
                chat.Description = request.Description.Trim();
            }

            if (request.AvatarUrl != null)
            {
                chat.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl;
            }

            chat.UpdatedAt = this.clock.UtcNow;
            await this.chatRepository.UpdateAsync(chat, cancellationToken);

            return await this.GetChatByIdAsync(chatId, userId, cancellationToken)
                ?? throw new DomainException("Failed to load updated chat.");
        }

        /// <inheritdoc/>
        public async Task<ChatDto> UploadChatAvatarAsync(
            string chatId,
            string userId,
            string fileName,
            Stream fileStream,
            CancellationToken cancellationToken = default)
        {
            var chat = await this.chatRepository.GetByIdAsync(chatId, cancellationToken);
            if (chat == null)
            {
                throw new NotFoundException($"Chat with ID '{chatId}' not found.");
            }

            if (chat.Type == NexusTeam.Shared.Enums.ChatType.DirectMessage)
            {
                throw new ValidationException("Cannot set avatar on a direct message.");
            }

            if (chat.CreatedBy != userId)
            {
                throw new UnauthorizedException("Only the group owner can change the group avatar.");
            }

            // Reuse avatar storage keyed by chat_* id so GET /api/users/avatar/{id} works
            var avatarKey = $"chat_{chatId}";
            var avatarUrl = await this.avatarService.SaveAvatarAsync(avatarKey, fileName, fileStream, cancellationToken);

            chat.AvatarUrl = avatarUrl;
            chat.UpdatedAt = this.clock.UtcNow;
            await this.chatRepository.UpdateAsync(chat, cancellationToken);

            this.logger.Information("Avatar uploaded for chat {ChatId}: {AvatarUrl}", chatId, avatarUrl);

            return await this.GetChatByIdAsync(chatId, userId, cancellationToken)
                ?? throw new DomainException("Failed to load updated chat.");
        }

        /// <inheritdoc/>
        public async Task DeleteChatAsync(string chatId, string userId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("User {UserId} requesting deletion of chat {ChatId}", userId, chatId);

            // Verify chat exists
            var chat = await this.chatRepository.GetByIdAsync(chatId, cancellationToken);
            if (chat == null)
            {
                this.logger.Warning("Chat {ChatId} not found for deletion", chatId);
                throw new NotFoundException($"Chat with ID '{chatId}' not found.");
            }

            // Verify user is a participant of the chat
            if (!chat.ParticipantIds.Contains(userId))
            {
                this.logger.Warning("User {UserId} is not a participant of chat {ChatId}", userId, chatId);
                throw new UnauthorizedException("You are not a participant of this chat.");
            }

            try
            {
                // 1. Get all message IDs for this chat
                var messageIds = await this.messageRepository.DeleteByChatIdAsync(chatId, cancellationToken);
                this.logger.Information("Deleted {Count} messages from chat {ChatId}", messageIds.Count, chatId);

                // 2. Get all attachments for these messages and delete files from disk
                if (messageIds.Count > 0)
                {
                    var attachments = await this.attachmentRepository.GetByMessageIdsAsync(messageIds, cancellationToken);

                    // Delete attachment files from disk
                    foreach (var attachment in attachments)
                    {
                        this.DeleteAttachmentFiles(attachment);
                    }

                    // Delete attachments from database
                    var deletedAttachmentIds = await this.attachmentRepository.DeleteByMessageIdsAsync(messageIds, cancellationToken);
                    this.logger.Information("Deleted {Count} attachments from chat {ChatId}", deletedAttachmentIds.Count, chatId);
                }

                // 3. Remove chat from all folders
                await this.folderRepository.RemoveChatFromAllFoldersAsync(chatId, cancellationToken);
                this.logger.Information("Removed chat {ChatId} from all folders", chatId);

                // 4. Delete the chat itself
                await this.chatRepository.DeleteAsync(chatId, cancellationToken);
                this.logger.Information("Chat {ChatId} deleted successfully by user {UserId}", chatId, userId);
            }
            catch (Exception ex) when (ex is not NotFoundException && ex is not UnauthorizedException)
            {
                this.logger.Error(ex, "Error deleting chat {ChatId}", chatId);
                throw new DomainException($"Failed to delete chat: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes an empty group chat without requiring the caller to still be a participant.
        /// </summary>
        private async Task ForceDeleteEmptyChatAsync(string chatId, CancellationToken cancellationToken)
        {
            this.logger.Information("Force-deleting empty chat {ChatId}", chatId);

            var messageIds = await this.messageRepository.DeleteByChatIdAsync(chatId, cancellationToken);
            if (messageIds.Count > 0)
            {
                var attachments = await this.attachmentRepository.GetByMessageIdsAsync(messageIds, cancellationToken);
                foreach (var attachment in attachments)
                {
                    this.DeleteAttachmentFiles(attachment);
                }

                await this.attachmentRepository.DeleteByMessageIdsAsync(messageIds, cancellationToken);
            }

            await this.folderRepository.RemoveChatFromAllFoldersAsync(chatId, cancellationToken);
            await this.chatRepository.DeleteAsync(chatId, cancellationToken);
            this.logger.Information("Empty chat {ChatId} deleted", chatId);
        }

        private void DeleteAttachmentFiles(MessageAttachment attachment)
        {
            // Delete main file
            if (!string.IsNullOrEmpty(attachment.FilePath))
            {
                var filePath = Path.Combine(this.storagePath, attachment.FilePath);
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                        this.logger.Debug("Deleted attachment file: {FilePath}", filePath);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Warning(ex, "Failed to delete attachment file: {FilePath}", filePath);
                    }
                }
            }

            // Delete thumbnail if exists
            if (!string.IsNullOrEmpty(attachment.ThumbnailPath))
            {
                var thumbnailPath = Path.Combine(this.storagePath, attachment.ThumbnailPath);
                if (File.Exists(thumbnailPath))
                {
                    try
                    {
                        File.Delete(thumbnailPath);
                        this.logger.Debug("Deleted thumbnail file: {FilePath}", thumbnailPath);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Warning(ex, "Failed to delete thumbnail file: {FilePath}", thumbnailPath);
                    }
                }
            }
        }

        private ChatDto MapToDto(Chat chat)
        {
            return new ChatDto
            {
                Id = chat.Id,
                Type = chat.Type,
                Name = chat.Name,
                Description = chat.Description,
                AvatarUrl = chat.AvatarUrl,
                ParticipantIds = chat.ParticipantIds,
                Participants = new List<UserDto>(), // Will be populated by async methods when needed
                CreatedBy = chat.CreatedBy,
                CreatedAt = chat.CreatedAt,
                LastMessageAt = chat.LastMessageAt,
            };
        }

        private async Task<UserDto> MapUserToDtoAsync(User user, CancellationToken cancellationToken = default)
        {
            // Get status from Redis instead of Oracle (Invisible appears as Offline to others)
            var status = await this.userStatusService.GetPublicStatusAsync(user.Id, cancellationToken);

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                Status = status,
                LastSeenAt = user.LastSeenAt,
            };
        }
    }
}
