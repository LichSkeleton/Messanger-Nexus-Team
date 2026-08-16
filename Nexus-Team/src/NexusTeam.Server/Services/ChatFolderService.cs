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
    using NexusTeam.Shared.Exceptions;
    using NexusTeam.Shared.Models;
    using Serilog;

    /// <summary>
    /// Service for chat folder operations.
    /// </summary>
    public class ChatFolderService : IChatFolderService
    {
        private const int MaxFoldersPerUser = 5;

        private readonly IChatFolderRepository folderRepository;
        private readonly IChatRepository chatRepository;
        private readonly IIdGenerator idGenerator;
        private readonly IClock clock;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatFolderService"/> class.
        /// </summary>
        /// <param name="folderRepository">Folder repository.</param>
        /// <param name="chatRepository">Chat repository.</param>
        /// <param name="idGenerator">ID generator.</param>
        /// <param name="clock">Clock for timestamps.</param>
        /// <param name="logger">Logger.</param>
        public ChatFolderService(
            IChatFolderRepository folderRepository,
            IChatRepository chatRepository,
            IIdGenerator idGenerator,
            IClock clock,
            ILogger logger)
        {
            this.folderRepository = folderRepository;
            this.chatRepository = chatRepository;
            this.idGenerator = idGenerator;
            this.clock = clock;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ChatFolderDto>> GetUserFoldersAsync(string userId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Getting folders for user {UserId}", userId);
            var folders = await this.folderRepository.GetByUserIdAsync(userId, cancellationToken);
            return folders.Select(this.MapToDto);
        }

        /// <inheritdoc/>
        public async Task<ChatFolderDto?> GetFolderByIdAsync(string folderId, string userId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Getting folder {FolderId} for user {UserId}", folderId, userId);
            var folder = await this.folderRepository.GetByIdAsync(folderId, cancellationToken);

            if (folder == null)
            {
                return null;
            }

            if (folder.UserId != userId)
            {
                this.logger.Warning("User {UserId} attempted to access folder {FolderId} owned by {OwnerId}", userId, folderId, folder.UserId);
                return null;
            }

            return this.MapToDto(folder);
        }

        /// <inheritdoc/>
        public async Task<ChatFolderDto> CreateFolderAsync(CreateChatFolderRequest request, string userId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Creating folder {FolderName} for user {UserId}", request.Name, userId);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ValidationException("Folder name is required.");
            }

            var existingFolders = (await this.folderRepository.GetByUserIdAsync(userId, cancellationToken)).ToList();
            if (existingFolders.Count >= MaxFoldersPerUser)
            {
                throw new ValidationException("You can have at most 5 folders.");
            }

            var chatIds = await this.ValidateFolderChatsAsync(request.ChatIds, userId, cancellationToken);

            var now = this.clock.UtcNow;
            var folder = new ChatFolder
            {
                Id = this.idGenerator.GenerateId(),
                Name = request.Name.Trim(),
                UserId = userId,
                ChatIds = chatIds,
                CreatedAt = now,
                UpdatedAt = now,
            };

            await this.folderRepository.CreateAsync(folder, cancellationToken);
            this.logger.Information("Folder {FolderId} created successfully by user {UserId}", folder.Id, userId);

            return this.MapToDto(folder);
        }

        /// <inheritdoc/>
        public async Task<ChatFolderDto> UpdateFolderAsync(string folderId, CreateChatFolderRequest request, string userId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Updating folder {FolderId} for user {UserId}", folderId, userId);

            var folder = await this.folderRepository.GetByIdAsync(folderId, cancellationToken);
            if (folder == null)
            {
                throw new NotFoundException($"Folder with ID '{folderId}' not found.");
            }

            if (folder.UserId != userId)
            {
                throw new UnauthorizedException("You do not have access to this folder.");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ValidationException("Folder name is required.");
            }

            folder.Name = request.Name.Trim();
            folder.ChatIds = await this.ValidateFolderChatsAsync(request.ChatIds, userId, cancellationToken);
            folder.UpdatedAt = this.clock.UtcNow;

            await this.folderRepository.UpdateAsync(folder, cancellationToken);
            this.logger.Information("Folder {FolderId} updated successfully by user {UserId}", folderId, userId);

            return this.MapToDto(folder);
        }

        /// <inheritdoc/>
        public async Task DeleteFolderAsync(string folderId, string userId, CancellationToken cancellationToken = default)
        {
            this.logger.Information("Deleting folder {FolderId} for user {UserId}", folderId, userId);

            var folder = await this.folderRepository.GetByIdAsync(folderId, cancellationToken);
            if (folder == null)
            {
                throw new NotFoundException($"Folder with ID '{folderId}' not found.");
            }

            if (folder.UserId != userId)
            {
                throw new UnauthorizedException("You do not have access to this folder.");
            }

            await this.folderRepository.DeleteAsync(folderId, cancellationToken);
            this.logger.Information("Folder {FolderId} deleted successfully by user {UserId}", folderId, userId);
        }

        private async Task<List<string>> ValidateFolderChatsAsync(
            IEnumerable<string>? chatIds,
            string userId,
            CancellationToken cancellationToken)
        {
            var ids = (chatIds ?? new List<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (ids.Count == 0)
            {
                throw new ValidationException("Folder must contain at least one chat.");
            }

            foreach (var chatId in ids)
            {
                var chat = await this.chatRepository.GetByIdAsync(chatId, cancellationToken);
                if (chat == null)
                {
                    throw new ValidationException($"Chat with ID '{chatId}' does not exist.");
                }

                if (chat.ParticipantIds == null || !chat.ParticipantIds.Contains(userId))
                {
                    throw new ValidationException($"You do not have access to chat '{chatId}'.");
                }
            }

            return ids;
        }

        private ChatFolderDto MapToDto(ChatFolder folder)
        {
            return new ChatFolderDto
            {
                Id = folder.Id,
                Name = folder.Name,
                UserId = folder.UserId,
                ChatIds = folder.ChatIds ?? new List<string>(),
                CreatedAt = folder.CreatedAt,
                UpdatedAt = folder.UpdatedAt,
            };
        }
    }
}
