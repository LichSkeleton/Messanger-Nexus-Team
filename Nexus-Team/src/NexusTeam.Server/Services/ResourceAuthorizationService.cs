namespace NexusTeam.Server.Services
{
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Models;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Enums;
    using NexusTeam.Shared.Exceptions;
    using Serilog;
    using SharedChat = NexusTeam.Shared.Models.Chat;
    using SharedMessage = NexusTeam.Shared.Models.Message;

    /// <summary>
    /// Authorization checks for chat membership and resource ownership.
    /// </summary>
    public class ResourceAuthorizationService : IResourceAuthorizationService
    {
        private readonly IChatRepository chatRepository;
        private readonly IMessageRepository messageRepository;
        private readonly IGeneratedImageRepository generatedImageRepository;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceAuthorizationService"/> class.
        /// </summary>
        /// <param name="chatRepository">Chat repository.</param>
        /// <param name="messageRepository">Message repository.</param>
        /// <param name="generatedImageRepository">Generated image repository.</param>
        /// <param name="logger">Logger.</param>
        public ResourceAuthorizationService(
            IChatRepository chatRepository,
            IMessageRepository messageRepository,
            IGeneratedImageRepository generatedImageRepository,
            ILogger logger)
        {
            this.chatRepository = chatRepository;
            this.messageRepository = messageRepository;
            this.generatedImageRepository = generatedImageRepository;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task<SharedChat> EnsureChatParticipantAsync(string chatId, string userId, CancellationToken cancellationToken = default)
        {
            var chat = await this.chatRepository.GetByIdAsync(chatId, cancellationToken);
            if (chat == null)
            {
                throw new NotFoundException($"Chat with ID '{chatId}' not found.");
            }

            if (chat.ParticipantIds == null || !chat.ParticipantIds.Contains(userId))
            {
                this.logger.Warning("User {UserId} is not a participant of chat {ChatId}", userId, chatId);
                throw new UnauthorizedException("You are not a participant of this chat.");
            }

            return chat;
        }

        /// <inheritdoc/>
        public async Task<SharedChat> EnsureChatOwnerForDeleteAsync(string chatId, string userId, CancellationToken cancellationToken = default)
        {
            var chat = await this.EnsureChatParticipantAsync(chatId, userId, cancellationToken);

            if (chat.Type != ChatType.DirectMessage && chat.CreatedBy != userId)
            {
                this.logger.Warning("User {UserId} attempted to delete chat {ChatId} without ownership", userId, chatId);
                throw new UnauthorizedException("Only the group owner can delete the entire group.");
            }

            return chat;
        }

        /// <inheritdoc/>
        public async Task<SharedMessage> EnsureMessageChatParticipantAsync(string messageId, string userId, CancellationToken cancellationToken = default)
        {
            var message = await this.messageRepository.GetByIdAsync(messageId, cancellationToken);
            if (message == null)
            {
                throw new NotFoundException($"Message with ID '{messageId}' not found.");
            }

            await this.EnsureChatParticipantAsync(message.ChatId, userId, cancellationToken);
            return message;
        }

        /// <inheritdoc/>
        public async Task<GeneratedImage> EnsureGeneratedImageOwnerAsync(string imageId, string userId, CancellationToken cancellationToken = default)
        {
            var image = await this.generatedImageRepository.GetByIdAsync(imageId, cancellationToken);
            if (image == null || image.IsDeleted)
            {
                throw new NotFoundException($"Generated image with ID '{imageId}' not found.");
            }

            if (image.UserId != userId)
            {
                this.logger.Warning("User {UserId} attempted to access generated image {ImageId} owned by {OwnerId}", userId, imageId, image.UserId);
                throw new UnauthorizedException("You do not have access to this generated image.");
            }

            return image;
        }
    }
}
