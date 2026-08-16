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
        private readonly IUserPreferenceRepository preferenceRepository;
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
        /// <param name="preferenceRepository">User preference repository (pinned chats).</param>
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
            IUserPreferenceRepository preferenceRepository,
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
            this.preferenceRepository = preferenceRepository;
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
            var chats = (await this.chatRepository.GetByUserIdAsync(userId, cancellationToken)).ToList();

            if (!chats.Any(this.IsSavedMessagesChat))
            {
                try
                {
                    chats.Add(await this.CreateSavedMessagesChatAsync(userId, cancellationToken));
                }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "Could not create Saved Messages for user {UserId}", userId);
                    var existing = await this.chatRepository.GetByIdAsync("saved-" + userId, cancellationToken);
                    if (existing != null && !chats.Any(chat => chat.Id == existing.Id))
                    {
                        chats.Add(existing);
                    }
                }
            }

            List<string> pinnedIds;
            try
            {
                pinnedIds = await this.GetPinnedChatIdsAsync(userId, cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Could not load pinned chats for user {UserId}", userId);
                pinnedIds = new List<string>();
            }

            var chatDtos = new List<ChatDto>();

            foreach (var chat in chats)
            {
                try
                {
                    chatDtos.Add(await this.BuildChatDtoAsync(chat, userId, pinnedIds, cancellationToken));
                }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "Could not map chat {ChatId} for user {UserId}", chat.Id, userId);
                }
            }

            return this.SortChats(chatDtos);
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

            // Internal system callers bypass membership; API callers must be participants.
            if (!string.Equals(userId, "system", StringComparison.Ordinal)
                && (chat.ParticipantIds == null || !chat.ParticipantIds.Contains(userId)))
            {
                this.logger.Warning("User {UserId} is not a participant of chat {ChatId}", userId, chatId);
                return null;
            }

            var pinnedIds = new List<string>();
            try
            {
                pinnedIds = await this.GetPinnedChatIdsAsync(userId, cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Could not load pinned chats for chat {ChatId}", chatId);
            }

            return await this.BuildChatDtoAsync(chat, userId, pinnedIds, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ChatDto> CreateChatAsync(CreateChatRequest request, string creatorUserId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Creating chat {ChatName} for user {UserId}", request.Name, creatorUserId);

            if (request.Type == NexusTeam.Shared.Enums.ChatType.SavedMessages)
            {
                throw new ValidationException("Saved Messages chats are created automatically.");
            }

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
        public async Task<ChatMembershipChangeResult> LeaveChatAsync(string chatId, string userId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("User {UserId} leaving chat {ChatId}", userId, chatId);

            var chat = await this.chatRepository.GetByIdAsync(chatId, cancellationToken);
            if (chat == null)
            {
                throw new NotFoundException($"Chat with ID '{chatId}' not found.");
            }

            if (this.IsSavedMessagesChat(chat))
            {
                throw new ValidationException("Cannot leave Saved Messages.");
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
                return new ChatMembershipChangeResult
                {
                    ChatDeleted = true,
                    RemovedUserId = userId,
                };
            }

            var displayName = await this.ResolveDisplayNameAsync(userId, cancellationToken);

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
                return new ChatMembershipChangeResult
                {
                    ChatDeleted = true,
                    RemovedUserId = userId,
                };
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

            var systemMessage = await this.CreateSystemMessageAsync(
                chatId,
                userId,
                $"{displayName} left the group",
                cancellationToken);

            this.logger.Information("User {UserId} left chat {ChatId}", userId, chatId);

            return new ChatMembershipChangeResult
            {
                Chat = await this.GetChatByIdAsync(chatId, refreshed.ParticipantIds[0], cancellationToken),
                SystemMessages = new List<MessageDto> { systemMessage },
                RemovedUserId = userId,
            };
        }

        /// <inheritdoc/>
        public async Task<ChatMembershipChangeResult> AddParticipantsAsync(
            string chatId,
            string ownerUserId,
            IReadOnlyList<string> userIds,
            CancellationToken cancellationToken = default)
        {
            var chat = await this.RequireOwnedGroupAsync(chatId, ownerUserId, cancellationToken);

            var uniqueIds = userIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (uniqueIds.Count == 0)
            {
                throw new ValidationException("Select at least one user to add.");
            }

            var addedIds = new List<string>();
            var systemMessages = new List<MessageDto>();

            foreach (var userId in uniqueIds)
            {
                if (chat.ParticipantIds.Contains(userId))
                {
                    throw new ValidationException("One or more selected users are already in the group.");
                }

                var user = await this.userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null)
                {
                    throw new ValidationException($"User with ID '{userId}' does not exist.");
                }

                await this.chatRepository.AddParticipantAsync(chatId, userId, cancellationToken);
                if (!chat.ParticipantIds.Contains(userId))
                {
                    chat.ParticipantIds.Add(userId);
                }

                addedIds.Add(userId);

                systemMessages.Add(await this.CreateSystemMessageAsync(
                    chatId,
                    ownerUserId,
                    $"{this.GetUserDisplayName(user)} was added to the group",
                    cancellationToken));
            }

            this.logger.Information(
                "Owner {OwnerId} added {Count} members to chat {ChatId}",
                ownerUserId,
                addedIds.Count,
                chatId);

            return new ChatMembershipChangeResult
            {
                Chat = await this.GetChatByIdAsync(chatId, ownerUserId, cancellationToken),
                SystemMessages = systemMessages,
                AddedUserIds = addedIds,
            };
        }

        /// <inheritdoc/>
        public async Task<ChatMembershipChangeResult> RemoveParticipantAsync(
            string chatId,
            string ownerUserId,
            string targetUserId,
            CancellationToken cancellationToken = default)
        {
            var chat = await this.RequireOwnedGroupAsync(chatId, ownerUserId, cancellationToken);

            if (string.Equals(ownerUserId, targetUserId, StringComparison.Ordinal))
            {
                throw new ValidationException("Leave the group instead of removing yourself.");
            }

            if (chat.ParticipantIds == null || !chat.ParticipantIds.Contains(targetUserId))
            {
                throw new NotFoundException("That user is not a member of this group.");
            }

            var displayName = await this.ResolveDisplayNameAsync(targetUserId, cancellationToken);

            await this.chatRepository.RemoveParticipantAsync(chatId, targetUserId, cancellationToken);
            await this.folderRepository.RemoveChatFromUserFoldersAsync(chatId, targetUserId, cancellationToken);

            var systemMessage = await this.CreateSystemMessageAsync(
                chatId,
                ownerUserId,
                $"{displayName} was removed from the group",
                cancellationToken);

            this.logger.Information(
                "Owner {OwnerId} removed {TargetId} from chat {ChatId}",
                ownerUserId,
                targetUserId,
                chatId);

            return new ChatMembershipChangeResult
            {
                Chat = await this.GetChatByIdAsync(chatId, ownerUserId, cancellationToken),
                SystemMessages = new List<MessageDto> { systemMessage },
                RemovedUserId = targetUserId,
            };
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

            if (chat.Type == NexusTeam.Shared.Enums.ChatType.SavedMessages
                || chat.Type == NexusTeam.Shared.Enums.ChatType.DirectMessage)
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

            if (chat.Type == NexusTeam.Shared.Enums.ChatType.SavedMessages
                || chat.Type == NexusTeam.Shared.Enums.ChatType.DirectMessage)
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
        public async Task<ChatDto> SetChatPinnedAsync(
            string chatId,
            string userId,
            bool pinned,
            CancellationToken cancellationToken = default)
        {
            var chat = await this.chatRepository.GetByIdAsync(chatId, cancellationToken);
            if (chat == null)
            {
                throw new NotFoundException($"Chat with ID '{chatId}' not found.");
            }

            if (chat.ParticipantIds == null || !chat.ParticipantIds.Contains(userId))
            {
                throw new UnauthorizedException("You are not a participant of this chat.");
            }

            var existing = await this.preferenceRepository.GetByUserIdAsync(userId, cancellationToken);
            var isNew = existing == null;
            var preference = existing ?? new NexusTeam.Server.Data.Models.UserPreference
            {
                UserId = userId,
                NotificationsEnabled = true,
                SoundEnabled = true,
                Theme = "light",
                Language = "en",
                PinnedChats = new List<string>(),
                CreatedAt = this.clock.UtcNow,
            };

            preference.PinnedChats ??= new List<string>();
            preference.PinnedChats.RemoveAll(id => id == chatId);

            if (pinned)
            {
                if (preference.PinnedChats.Count >= 100)
                {
                    throw new ValidationException("Pinned chats list cannot exceed 100 items.");
                }

                preference.PinnedChats.Insert(0, chatId);
            }

            preference.UpdatedAt = this.clock.UtcNow;
            if (isNew)
            {
                await this.preferenceRepository.CreateAsync(preference, cancellationToken);
            }
            else
            {
                await this.preferenceRepository.UpdateAsync(preference, cancellationToken);
            }

            return await this.GetChatByIdAsync(chatId, userId, cancellationToken)
                ?? throw new DomainException("Failed to load pinned chat.");
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

            if (this.IsSavedMessagesChat(chat))
            {
                throw new ValidationException("Saved Messages cannot be deleted.");
            }

            // Verify user is a participant of the chat
            if (!chat.ParticipantIds.Contains(userId))
            {
                this.logger.Warning("User {UserId} is not a participant of chat {ChatId}", userId, chatId);
                throw new UnauthorizedException("You are not a participant of this chat.");
            }

            // Groups/channels: only the owner may delete the entire chat
            if (chat.Type != NexusTeam.Shared.Enums.ChatType.DirectMessage && chat.CreatedBy != userId)
            {
                this.logger.Warning("User {UserId} attempted to delete chat {ChatId} without ownership", userId, chatId);
                throw new UnauthorizedException("Only the group owner can delete the entire group.");
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

        private async Task<Chat> RequireOwnedGroupAsync(
            string chatId,
            string ownerUserId,
            CancellationToken cancellationToken)
        {
            var chat = await this.chatRepository.GetByIdAsync(chatId, cancellationToken);
            if (chat == null)
            {
                throw new NotFoundException($"Chat with ID '{chatId}' not found.");
            }

            if (chat.Type == NexusTeam.Shared.Enums.ChatType.SavedMessages
                || chat.Type == NexusTeam.Shared.Enums.ChatType.DirectMessage)
            {
                throw new ValidationException("Members can only be managed in a group chat.");
            }

            if (chat.ParticipantIds == null || !chat.ParticipantIds.Contains(ownerUserId))
            {
                throw new UnauthorizedException("You are not a participant of this chat.");
            }

            if (chat.CreatedBy != ownerUserId)
            {
                throw new UnauthorizedException("Only the group owner can add or remove members.");
            }

            return chat;
        }

        private async Task<MessageDto> CreateSystemMessageAsync(
            string chatId,
            string actorUserId,
            string content,
            CancellationToken cancellationToken)
        {
            var now = this.clock.UtcNow;
            var message = new Message
            {
                Id = this.idGenerator.GenerateId(),
                ChatId = chatId,
                SenderId = actorUserId,
                Content = content,
                Status = NexusTeam.Shared.Enums.MessageStatus.Sent,
                CreatedAt = now,
                IsDeleted = false,
                IsSystem = true,
            };

            await this.messageRepository.CreateAsync(message, cancellationToken);

            var chat = await this.chatRepository.GetByIdAsync(chatId, cancellationToken);
            if (chat != null)
            {
                chat.LastMessageAt = now;
                chat.UpdatedAt = now;
                await this.chatRepository.UpdateAsync(chat, cancellationToken);
            }

            return new MessageDto
            {
                Id = message.Id,
                ChatId = message.ChatId,
                SenderId = message.SenderId,
                Content = message.Content,
                Status = message.Status,
                CreatedAt = message.CreatedAt,
                IsSystem = true,
            };
        }

        private async Task<string> ResolveDisplayNameAsync(string userId, CancellationToken cancellationToken)
        {
            var user = await this.userRepository.GetByIdAsync(userId, cancellationToken);
            return user == null ? "A member" : this.GetUserDisplayName(user);
        }

        private string GetUserDisplayName(User user)
        {
            return !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : user.Username;
        }

        private ChatDto MapToDto(Chat chat, bool isPinned = false)
        {
            var chatDto = new ChatDto
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
                IsPinned = isPinned,
            };

            this.ApplyDefaultGroupAvatar(chatDto);
            return chatDto;
        }

        private void ApplyDefaultGroupAvatar(ChatDto chatDto)
        {
            if (chatDto.Type != NexusTeam.Shared.Enums.ChatType.Group
                && chatDto.Type != NexusTeam.Shared.Enums.ChatType.Channel)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(chatDto.AvatarUrl))
            {
                chatDto.AvatarUrl = $"/api/users/avatar/chat_{chatDto.Id}";
            }
        }

        private async Task<ChatDto> BuildChatDtoAsync(
            Chat chat,
            string userId,
            IReadOnlyCollection<string> pinnedIds,
            CancellationToken cancellationToken)
        {
            var chatDto = this.MapToDto(chat, pinnedIds.Contains(chat.Id));

            if (this.IsSavedMessagesChat(chat))
            {
                chatDto.Name = "Saved Messages";
            }

            if (chat.ParticipantIds != null && chat.ParticipantIds.Any())
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

                if (chat.Type == NexusTeam.Shared.Enums.ChatType.DirectMessage && !this.IsSavedMessagesChat(chat))
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

        private async Task<Chat> CreateSavedMessagesChatAsync(string userId, CancellationToken cancellationToken)
        {
            var now = this.clock.UtcNow;
            var chat = new Chat
            {
                Id = "saved-" + userId,
                Type = NexusTeam.Shared.Enums.ChatType.SavedMessages,
                Name = "Saved Messages",
                ParticipantIds = new List<string> { userId },
                CreatedBy = userId,
                CreatedAt = now,
                UpdatedAt = now,
                LastMessageAt = null,
            };

            await this.chatRepository.CreateAsync(chat, cancellationToken);
            this.logger.Information("Created Saved Messages chat {ChatId} for user {UserId}", chat.Id, userId);
            return chat;
        }

        private bool IsSavedMessagesChat(Chat chat)
        {
            if (chat.Type == NexusTeam.Shared.Enums.ChatType.SavedMessages)
            {
                return true;
            }

            return !string.IsNullOrEmpty(chat.Id)
                && chat.Id.StartsWith("saved-", StringComparison.Ordinal);
        }

        private async Task<List<string>> GetPinnedChatIdsAsync(string userId, CancellationToken cancellationToken)
        {
            var preference = await this.preferenceRepository.GetByUserIdAsync(userId, cancellationToken);
            return preference?.PinnedChats ?? new List<string>();
        }

        private List<ChatDto> SortChats(List<ChatDto> chatDtos)
        {
            return chatDtos
                .OrderByDescending(this.IsSavedMessagesDto)
                .ThenByDescending(chat => chat.LastMessageAt ?? chat.CreatedAt)
                .ToList();
        }

        private bool IsSavedMessagesDto(ChatDto chat)
        {
            return chat.Type == NexusTeam.Shared.Enums.ChatType.SavedMessages
                || (!string.IsNullOrEmpty(chat.Id) && chat.Id.StartsWith("saved-", StringComparison.Ordinal));
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
