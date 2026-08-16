namespace NexusTeam.Server.Services.Abstractions
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Service for chat operations.
    /// </summary>
    public interface IChatService
    {
        /// <summary>
        /// Gets all chats for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of chat DTOs.</returns>
        Task<IEnumerable<ChatDto>> GetUserChatsAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific chat by ID.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="userId">The user ID for context (used for DirectMessage name resolution).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The chat DTO if found.</returns>
        Task<ChatDto?> GetChatByIdAsync(string chatId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new chat.
        /// </summary>
        /// <param name="request">The create chat request data.</param>
        /// <param name="creatorUserId">The user ID of the chat creator.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created chat DTO with participant details.</returns>
        Task<ChatDto> CreateChatAsync(CreateChatRequest request, string creatorUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a chat and all its associated data (messages, attachments).
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="userId">The user ID requesting deletion.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteChatAsync(string chatId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Leaves a group chat (removes the user from participants).
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="userId">The user ID leaving the chat.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Membership change details including a system message for remaining members.</returns>
        Task<ChatMembershipChangeResult> LeaveChatAsync(string chatId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds users to a group chat. Only the owner can add members.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="ownerUserId">The user ID of the requester (must be owner).</param>
        /// <param name="userIds">User IDs to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Membership change details including system messages.</returns>
        Task<ChatMembershipChangeResult> AddParticipantsAsync(
            string chatId,
            string ownerUserId,
            IReadOnlyList<string> userIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a user from a group chat. Only the owner can remove members.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="ownerUserId">The user ID of the requester (must be owner).</param>
        /// <param name="targetUserId">The user ID to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Membership change details including a system message.</returns>
        Task<ChatMembershipChangeResult> RemoveParticipantAsync(
            string chatId,
            string ownerUserId,
            string targetUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates group chat properties. Only the owner (creator) can update.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="userId">The user ID requesting the update.</param>
        /// <param name="request">The update request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated chat DTO.</returns>
        Task<ChatDto> UpdateChatAsync(string chatId, string userId, UpdateChatRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a new avatar for a group chat. Only the owner can upload.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="userId">The user ID requesting the upload.</param>
        /// <param name="fileName">Original file name.</param>
        /// <param name="fileStream">Image stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated chat DTO.</returns>
        Task<ChatDto> UploadChatAvatarAsync(
            string chatId,
            string userId,
            string fileName,
            System.IO.Stream fileStream,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Pins or unpins a chat for the current user. Pin state is personal.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="userId">The user ID requesting the change.</param>
        /// <param name="pinned">Whether the chat should be pinned.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated chat DTO.</returns>
        Task<ChatDto> SetChatPinnedAsync(
            string chatId,
            string userId,
            bool pinned,
            CancellationToken cancellationToken = default);
    }
}
