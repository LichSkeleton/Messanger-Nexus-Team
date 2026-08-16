namespace NexusTeam.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using NexusTeam.Shared.Exceptions;
    using NexusTeam.Shared.Models;
    using Serilog;

    /// <summary>
    /// Service for message operations with Oracle persistence and Redis caching.
    /// </summary>
    public class MessageService : IMessageService
    {
        private const int ReplyPreviewMaxLength = 200;

        private readonly IMessageRepository messageRepository;
        private readonly IChatRepository chatRepository;
        private readonly IMessageAttachmentRepository attachmentRepository;
        private readonly IUserRepository userRepository;
        private readonly ICacheService cacheService;
        private readonly IIdGenerator idGenerator;
        private readonly IClock clock;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageService"/> class.
        /// </summary>
        /// <param name="messageRepository">Message repository.</param>
        /// <param name="chatRepository">Chat repository.</param>
        /// <param name="attachmentRepository">Attachment repository.</param>
        /// <param name="userRepository">User repository.</param>
        /// <param name="cacheService">Cache service.</param>
        /// <param name="idGenerator">ID generator.</param>
        /// <param name="clock">System clock.</param>
        /// <param name="logger">Logger.</param>
        public MessageService(
            IMessageRepository messageRepository,
            IChatRepository chatRepository,
            IMessageAttachmentRepository attachmentRepository,
            IUserRepository userRepository,
            ICacheService cacheService,
            IIdGenerator idGenerator,
            IClock clock,
            ILogger logger)
        {
            this.messageRepository = messageRepository;
            this.chatRepository = chatRepository;
            this.attachmentRepository = attachmentRepository;
            this.userRepository = userRepository;
            this.cacheService = cacheService;
            this.idGenerator = idGenerator;
            this.clock = clock;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task<MessageDto> SendMessageAsync(SendMessageRequest request, string senderId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Sending message from user {SenderId} to chat {ChatId}", senderId, request.ChatId);

            var chat = await this.chatRepository.GetByIdAsync(request.ChatId, cancellationToken);
            if (chat == null)
            {
                throw new ValidationException($"Chat {request.ChatId} not found");
            }

            if (!chat.ParticipantIds.Contains(senderId))
            {
                throw new ValidationException("User is not a participant in this chat");
            }

            string? replyToId = null;
            string? replyToSenderId = null;
            string? replyToSenderName = null;
            string? replyToContent = null;
            if (!string.IsNullOrWhiteSpace(request.ReplyToId))
            {
                var replySnapshot = await this.BuildReplySnapshotAsync(request.ReplyToId, request.ChatId, cancellationToken);
                replyToId = replySnapshot.ReplyToId;
                replyToSenderId = replySnapshot.ReplyToSenderId;
                replyToSenderName = replySnapshot.ReplyToSenderName;
                replyToContent = replySnapshot.ReplyToContent;
            }

            var message = new Message
            {
                Id = this.idGenerator.GenerateId(),
                ChatId = request.ChatId,
                SenderId = senderId,
                Content = request.Content,
                Status = MessageStatus.Sent,
                CreatedAt = this.clock.UtcNow,
                ReplyToId = replyToId,
                ReplyToSenderId = replyToSenderId,
                ReplyToSenderName = replyToSenderName,
                ReplyToContent = replyToContent,
                IsDeleted = false,
            };

            await this.messageRepository.CreateAsync(message, cancellationToken);

            chat.LastMessageAt = message.CreatedAt;
            chat.UpdatedAt = message.CreatedAt;
            await this.chatRepository.UpdateAsync(chat, cancellationToken);

            await this.InvalidateChatMessagesCache(request.ChatId, cancellationToken);

            this.logger.Information("Message {MessageId} sent successfully to chat {ChatId} with {AttachmentCount} attachments", message.Id, request.ChatId, request.AttachmentIds?.Count ?? 0);

            // Load attachments for response
            var attachments = new List<MessageAttachment>();
            if (request.AttachmentIds != null && request.AttachmentIds.Count > 0)
            {
                foreach (var attachmentId in request.AttachmentIds)
                {
                    var attachment = await this.attachmentRepository.GetByIdAsync(attachmentId, cancellationToken);
                    if (attachment != null)
                    {
                        attachments.Add(attachment);
                    }
                }
            }

            message.Attachments = attachments;

            return this.MapToDto(message);
        }

        /// <inheritdoc/>
        public async Task<MessageDto> ForwardMessageAsync(string targetChatId, string messageId, string userId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Forwarding message {MessageId} to chat {ChatId} by user {UserId}", messageId, targetChatId, userId);

            var source = await this.messageRepository.GetByIdAsync(messageId, cancellationToken);
            if (source == null)
            {
                throw new ValidationException($"Message {messageId} not found");
            }

            if (source.IsDeleted)
            {
                throw new ValidationException("Cannot forward a deleted message");
            }

            if (source.IsSystem)
            {
                throw new ValidationException("Cannot forward a system message");
            }

            var sourceChat = await this.chatRepository.GetByIdAsync(source.ChatId, cancellationToken);
            if (sourceChat == null || sourceChat.ParticipantIds == null || !sourceChat.ParticipantIds.Contains(userId))
            {
                throw new ValidationException("You do not have access to the original message");
            }

            var targetChat = await this.chatRepository.GetByIdAsync(targetChatId, cancellationToken);
            if (targetChat == null)
            {
                throw new ValidationException($"Chat {targetChatId} not found");
            }

            if (targetChat.ParticipantIds == null || !targetChat.ParticipantIds.Contains(userId))
            {
                throw new ValidationException("User is not a participant in this chat");
            }

            var originalSenderId = source.IsForwarded && !string.IsNullOrWhiteSpace(source.ForwardedFromSenderId)
                ? source.ForwardedFromSenderId
                : source.SenderId;
            var originalSenderName = source.IsForwarded && !string.IsNullOrWhiteSpace(source.ForwardedFromSenderName)
                ? source.ForwardedFromSenderName
                : await this.ResolveDisplayNameAsync(originalSenderId, cancellationToken);

            var forwarded = new Message
            {
                Id = this.idGenerator.GenerateId(),
                ChatId = targetChatId,
                SenderId = userId,
                Content = source.Content,
                Status = MessageStatus.Sent,
                CreatedAt = this.clock.UtcNow,
                IsDeleted = false,
                IsForwarded = true,
                ForwardedFromSenderId = originalSenderId,
                ForwardedFromSenderName = originalSenderName,
            };

            await this.messageRepository.CreateAsync(forwarded, cancellationToken);

            var sourceAttachments = (await this.attachmentRepository.GetByMessageIdAsync(source.Id, cancellationToken) ?? new List<MessageAttachment>()).ToList();
            var clonedAttachments = new List<MessageAttachment>();
            foreach (var attachment in sourceAttachments)
            {
                var clone = new MessageAttachment
                {
                    Id = Guid.NewGuid().ToString(),
                    MessageId = forwarded.Id,
                    FileName = attachment.FileName,
                    FilePath = attachment.FilePath,
                    ThumbnailPath = attachment.ThumbnailPath,
                    FileSize = attachment.FileSize,
                    ContentType = attachment.ContentType,
                    AttachmentType = attachment.AttachmentType,
                    UploadedAt = this.clock.UtcNow,
                };

                await this.attachmentRepository.AddAsync(clone, cancellationToken);
                clonedAttachments.Add(clone);
            }

            forwarded.Attachments = clonedAttachments;

            targetChat.LastMessageAt = forwarded.CreatedAt;
            targetChat.UpdatedAt = forwarded.CreatedAt;
            await this.chatRepository.UpdateAsync(targetChat, cancellationToken);
            await this.InvalidateChatMessagesCache(targetChatId, cancellationToken);

            this.logger.Information(
                "Message {SourceMessageId} forwarded as {MessageId} to chat {ChatId} with {AttachmentCount} attachments",
                source.Id,
                forwarded.Id,
                targetChatId,
                clonedAttachments.Count);

            return this.MapToDto(forwarded);
        }

        /// <inheritdoc/>
        public async Task<MessageDto> EditMessageAsync(string messageId, string newContent, string userId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Editing message {MessageId} by user {UserId}", messageId, userId);

            var message = await this.messageRepository.GetByIdAsync(messageId, cancellationToken);
            if (message == null)
            {
                throw new ValidationException($"Message {messageId} not found");
            }

            if (message.SenderId != userId)
            {
                throw new ValidationException("User is not the sender of this message");
            }

            if (message.IsDeleted)
            {
                throw new ValidationException("Cannot edit a deleted message");
            }

            if (message.IsSystem)
            {
                throw new ValidationException("Cannot edit a system message");
            }

            message.Content = newContent;
            message.EditedAt = this.clock.UtcNow;

            await this.messageRepository.UpdateAsync(message, cancellationToken);
            await this.InvalidateChatMessagesCache(message.ChatId, cancellationToken);

            this.logger.Information("Message {MessageId} edited successfully in chat {ChatId}", messageId, message.ChatId);

            return this.MapToDto(message);
        }

        /// <inheritdoc/>
        public async Task<string> DeleteMessageAsync(string messageId, string userId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Deleting message {MessageId} by user {UserId}", messageId, userId);

            var message = await this.messageRepository.GetByIdAsync(messageId, cancellationToken);
            if (message == null)
            {
                this.logger.Warning("Message {MessageId} not found for deletion", messageId);
                throw new ValidationException($"Message {messageId} not found");
            }

            if (message.SenderId != userId)
            {
                this.logger.Warning("User {UserId} attempted to delete message {MessageId} owned by {OwnerId}", userId, messageId, message.SenderId);
                throw new ValidationException("User is not the sender of this message");
            }

            if (message.IsDeleted)
            {
                this.logger.Warning("Message {MessageId} already deleted", messageId);
                throw new ValidationException("Message is already deleted");
            }

            if (message.IsSystem)
            {
                throw new ValidationException("Cannot delete a system message");
            }

            var chatId = message.ChatId;

            try
            {
                await this.messageRepository.DeleteAsync(messageId, cancellationToken);
                this.logger.Information("Message {MessageId} marked as deleted in database", messageId);

                await this.InvalidateChatMessagesCache(chatId, cancellationToken);
                this.logger.Information("Cache invalidated for chat {ChatId} after message deletion", chatId);

                this.logger.Information("Message {MessageId} deleted successfully from chat {ChatId}", messageId, chatId);

                return chatId;
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException oraEx)
            {
                this.logger.Error(oraEx, "Oracle error deleting message {MessageId}: ORA-{ErrorNumber} - {ErrorMessage}", messageId, oraEx.Number, oraEx.Message);
                throw new DomainException($"Database error while deleting message: {oraEx.Message}", oraEx);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Unexpected error deleting message {MessageId} from chat {ChatId}", messageId, chatId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<MessageDto>> GetChatMessagesAsync(string chatId, string userId, int limit, int offset, CancellationToken cancellationToken = default)
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

            var cacheKey = $"chat:messages:{chatId}:{limit}:{offset}";
            var cached = await this.cacheService.GetAsync<List<MessageDto>>(cacheKey, cancellationToken);

            if (cached != null)
            {
                this.logger.Information("Cache HIT for chat {ChatId}: returning {Count} cached messages (applying limit={Limit}, offset={Offset})", chatId, cached.Count, limit, offset);
                return cached.Skip(offset).Take(limit).ToList();
            }

            this.logger.Information("Cache MISS for chat {ChatId}: loading from database with limit={Limit}, offset={Offset}", chatId, limit, offset);

            var messagesEnumerable = await this.messageRepository.GetByChatIdAsync(chatId, limit, offset, cancellationToken);
            var messages = messagesEnumerable.ToList(); // Materialize to list BEFORE loading attachments

            // Load attachments for each message
            foreach (var message in messages)
            {
                var attachments = await this.attachmentRepository.GetByMessageIdAsync(message.Id, cancellationToken);
                this.logger.Debug("Loaded {Count} attachments for message {MessageId}", attachments?.Count ?? 0, message.Id);
                message.Attachments = attachments ?? new List<MessageAttachment>();
            }

            var dtos = messages.Select(this.MapToDto).ToList();

            this.logger.Information("Loaded {Count} messages from database for chat {ChatId}", dtos.Count, chatId);

            // Log attachment info for debugging
            foreach (var dto in dtos)
            {
                this.logger.Debug("Message {MessageId} has {AttachmentCount} attachments", dto.Id, dto.Attachments?.Count ?? 0);
                if (dto.Attachments != null && dto.Attachments.Count > 0)
                {
                    foreach (var att in dto.Attachments)
                    {
                        this.logger.Debug(
                            "  - Attachment {AttachmentId}: ThumbnailUrl={ThumbnailUrl}, DownloadUrl={DownloadUrl}",
                            att.Id,
                            att.ThumbnailUrl,
                            att.DownloadUrl);
                    }
                }
            }

            await this.cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(5), cancellationToken);
            this.logger.Debug("Cached {Count} messages for chat {ChatId}", dtos.Count, chatId);

            return dtos;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<MessageDto>> SearchMessagesAsync(string chatId, string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<MessageDto>();
            }

            this.logger.Information("Searching messages in chat {ChatId} with query '{Query}'", chatId, query);

            var messagesEnumerable = await this.messageRepository.SearchAsync(chatId, query, cancellationToken);
            var messages = messagesEnumerable.ToList();

            // Load attachments for found messages
            foreach (var message in messages)
            {
                var attachments = await this.attachmentRepository.GetByMessageIdAsync(message.Id, cancellationToken);
                message.Attachments = attachments ?? new List<MessageAttachment>();
            }

            var dtos = messages.Select(this.MapToDto).ToList();

            this.logger.Information("Found {Count} messages matching query '{Query}' in chat {ChatId}", dtos.Count, query, chatId);

            return dtos;
        }

        /// <inheritdoc/>
        public async Task MarkAsDeliveredAsync(string messageId, CancellationToken cancellationToken = default)
        {
            var message = await this.messageRepository.GetByIdAsync(messageId, cancellationToken);
            if (message != null && message.Status == MessageStatus.Sent)
            {
                message.Status = MessageStatus.Delivered;
                await this.messageRepository.UpdateAsync(message, cancellationToken);
                await this.InvalidateChatMessagesCache(message.ChatId, cancellationToken);
                this.logger.Debug("Message {MessageId} marked as delivered", messageId);
            }
        }

        /// <inheritdoc/>
        public async Task MarkAsReadAsync(string messageId, CancellationToken cancellationToken = default)
        {
            var message = await this.messageRepository.GetByIdAsync(messageId, cancellationToken);
            if (message != null && message.Status != MessageStatus.Read)
            {
                message.Status = MessageStatus.Read;
                await this.messageRepository.UpdateAsync(message, cancellationToken);
                await this.InvalidateChatMessagesCache(message.ChatId, cancellationToken);
                this.logger.Debug("Message {MessageId} marked as read", messageId);
            }
        }

        /// <inheritdoc/>
        public async Task<MessageDto> AddReactionAsync(string messageId, string emoji, string userId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Adding reaction {Emoji} to message {MessageId} by user {UserId}", emoji, messageId, userId);

            var message = await this.messageRepository.GetByIdAsync(messageId, cancellationToken);
            if (message == null)
            {
                throw new ValidationException($"Message {messageId} not found");
            }

            var chat = await this.chatRepository.GetByIdAsync(message.ChatId, cancellationToken);
            if (chat == null)
            {
                throw new ValidationException($"Chat {message.ChatId} not found");
            }

            if (chat.ParticipantIds == null || !chat.ParticipantIds.Contains(userId))
            {
                throw new UnauthorizedException("You are not a participant of this chat.");
            }

            if (message.IsSystem)
            {
                throw new ValidationException("Cannot react to a system message");
            }

            // Initialize reactions dictionary if null
            if (message.Reactions == null)
            {
                message.Reactions = new Dictionary<string, List<string>>();
            }

            // Add user to reaction list if not already present
            if (!message.Reactions.ContainsKey(emoji))
            {
                message.Reactions[emoji] = new List<string>();
            }

            if (!message.Reactions[emoji].Contains(userId))
            {
                message.Reactions[emoji].Add(userId);
                await this.messageRepository.UpdateAsync(message, cancellationToken);
                await this.InvalidateChatMessagesCache(message.ChatId, cancellationToken);
                this.logger.Debug("Reaction {Emoji} added to message {MessageId} by user {UserId}", emoji, messageId, userId);
            }

            return this.MapToDto(message);
        }

        /// <inheritdoc/>
        public async Task<MessageDto> RemoveReactionAsync(string messageId, string emoji, string userId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Removing reaction {Emoji} from message {MessageId} by user {UserId}", emoji, messageId, userId);

            var message = await this.messageRepository.GetByIdAsync(messageId, cancellationToken);
            if (message == null)
            {
                throw new ValidationException($"Message {messageId} not found");
            }

            var chat = await this.chatRepository.GetByIdAsync(message.ChatId, cancellationToken);
            if (chat == null)
            {
                throw new ValidationException($"Chat {message.ChatId} not found");
            }

            if (chat.ParticipantIds == null || !chat.ParticipantIds.Contains(userId))
            {
                throw new UnauthorizedException("You are not a participant of this chat.");
            }

            if (message.IsSystem)
            {
                throw new ValidationException("Cannot react to a system message");
            }

            // Remove user from reaction list
            if (message.Reactions != null && message.Reactions.ContainsKey(emoji))
            {
                if (message.Reactions[emoji].Remove(userId))
                {
                    // Remove emoji entry if no users left
                    if (message.Reactions[emoji].Count == 0)
                    {
                        message.Reactions.Remove(emoji);
                    }

                    await this.messageRepository.UpdateAsync(message, cancellationToken);
                    await this.InvalidateChatMessagesCache(message.ChatId, cancellationToken);
                    this.logger.Debug("Reaction {Emoji} removed from message {MessageId} by user {UserId}", emoji, messageId, userId);
                }
            }

            return this.MapToDto(message);
        }

        /// <inheritdoc/>
        public async Task<MessageDto?> GetMessageByIdAsync(string messageId, CancellationToken cancellationToken = default)
        {
            var message = await this.messageRepository.GetByIdAsync(messageId, cancellationToken);
            if (message == null)
            {
                return null;
            }

            // Load attachments
            var attachments = await this.attachmentRepository.GetByMessageIdAsync(messageId, cancellationToken);
            message.Attachments = attachments.ToList();

            return this.MapToDto(message);
        }

        private MessageDto MapToDto(Message message)
        {
            return new MessageDto
            {
                Id = message.Id,
                ChatId = message.ChatId,
                SenderId = message.SenderId,
                Content = message.Content,
                Status = message.Status,
                CreatedAt = message.CreatedAt,
                EditedAt = message.EditedAt,
                ReplyToId = message.ReplyToId,
                ReplyToSenderId = message.ReplyToSenderId,
                ReplyToSenderName = message.ReplyToSenderName,
                ReplyToContent = message.ReplyToContent,
                IsForwarded = message.IsForwarded,
                ForwardedFromSenderId = message.ForwardedFromSenderId,
                ForwardedFromSenderName = message.ForwardedFromSenderName,
                IsDeleted = message.IsDeleted,
                IsSystem = message.IsSystem,
                Attachments = message.Attachments?.Select(a => new MessageAttachmentDto
                {
                    Id = a.Id,
                    MessageId = a.MessageId,
                    FileName = a.FileName,
                    FileSize = a.FileSize,
                    ContentType = a.ContentType,
                    AttachmentType = a.AttachmentType,
                    DownloadUrl = $"/api/attachments/download/{a.Id}",
                    ThumbnailUrl = !string.IsNullOrEmpty(a.ThumbnailPath)
                        ? $"/api/attachments/thumbnail/{a.Id}"
                        : null,
                    UploadedAt = a.UploadedAt,
                }).ToList() ?? new List<MessageAttachmentDto>(),
                Reactions = message.Reactions ?? new Dictionary<string, List<string>>(),
            };
        }

        private async Task<(string ReplyToId, string? ReplyToSenderId, string? ReplyToSenderName, string? ReplyToContent)> BuildReplySnapshotAsync(
            string replyToId,
            string chatId,
            CancellationToken cancellationToken)
        {
            var parent = await this.messageRepository.GetByIdAsync(replyToId, cancellationToken);
            if (parent == null)
            {
                throw new ValidationException("Message being replied to was not found");
            }

            if (!string.Equals(parent.ChatId, chatId, StringComparison.Ordinal))
            {
                throw new ValidationException("Cannot reply to a message from another chat");
            }

            if (parent.IsSystem)
            {
                throw new ValidationException("Cannot reply to a system message");
            }

            if (parent.IsDeleted)
            {
                throw new ValidationException("Cannot reply to a deleted message");
            }

            var preview = this.TruncatePreview(parent.Content, ReplyPreviewMaxLength);
            if (string.IsNullOrWhiteSpace(preview))
            {
                var attachments = await this.attachmentRepository.GetByMessageIdAsync(parent.Id, cancellationToken);
                if (attachments != null && attachments.Count > 0)
                {
                    preview = attachments[0].FileName;
                }
            }

            return (
                parent.Id,
                parent.SenderId,
                await this.ResolveDisplayNameAsync(parent.SenderId, cancellationToken),
                preview);
        }

        private async Task<string> ResolveDisplayNameAsync(string userId, CancellationToken cancellationToken)
        {
            var user = await this.userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return userId;
            }

            if (!string.IsNullOrWhiteSpace(user.DisplayName))
            {
                return user.DisplayName;
            }

            if (!string.IsNullOrWhiteSpace(user.Username))
            {
                return user.Username;
            }

            return userId;
        }

        private string TruncatePreview(string? content, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            var trimmed = content.Trim();
            if (trimmed.Length <= maxLength)
            {
                return trimmed;
            }

            return trimmed.Substring(0, maxLength - 3) + "...";
        }

        private async Task InvalidateChatMessagesCache(string chatId, CancellationToken cancellationToken = default)
        {
            var cacheKeyBase = $"chat:messages:{chatId}";
            await this.cacheService.RemoveAsync($"{cacheKeyBase}:50:0", cancellationToken);
            await this.cacheService.RemoveAsync($"{cacheKeyBase}:1:0", cancellationToken);
            await this.cacheService.RemoveAsync($"{cacheKeyBase}:20:0", cancellationToken);
            this.logger.Debug("Cache invalidated for chat {ChatId}", chatId);
        }
    }
}
