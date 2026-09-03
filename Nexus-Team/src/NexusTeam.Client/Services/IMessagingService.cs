namespace NexusTeam.Client.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Service for managing real-time messaging via WebSocket and REST API.
    /// </summary>
    public interface IMessagingService
    {
        /// <summary>
        /// Gets a value indicating whether the WebSocket is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Occurs when a new message is received.
        /// </summary>
        event EventHandler<MessageDto>? MessageReceived;

        /// <summary>
        /// Occurs when a message is edited.
        /// </summary>
        event EventHandler<MessageDto>? MessageEdited;

        /// <summary>
        /// Occurs when a message is deleted.
        /// </summary>
        event EventHandler<string>? MessageDeleted;

        /// <summary>
        /// Occurs when a user starts typing.
        /// </summary>
        event EventHandler<string>? UserTyping;

        /// <summary>
        /// Occurs when a user's status changes.
        /// </summary>
        event EventHandler<StatusUpdateDto>? UserStatusChanged;

        /// <summary>
        /// Occurs when a user's avatar changes.
        /// </summary>
        event EventHandler<AvatarUpdateDto>? UserAvatarChanged;

        /// <summary>
        /// Occurs when the connection state changes.
        /// </summary>
        event EventHandler<bool>? ConnectionStateChanged;

        /// <summary>
        /// Occurs when a message reaction is updated.
        /// </summary>
        event EventHandler<MessageDto>? MessageReactionUpdated;

        /// <summary>
        /// Connects to the WebSocket server.
        /// </summary>
        /// <param name="accessToken">Authentication token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ConnectAsync(string accessToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Disconnects from the WebSocket server.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a new message via WebSocket.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="content">The message content.</param>
        /// <param name="replyToId">Optional ID of message being replied to.</param>
        /// <param name="attachmentIds">Optional list of attachment IDs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendMessageAsync(string chatId, string content, string? replyToId = null, List<string>? attachmentIds = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a new message via HTTP API (for attachments support).
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="content">The message content.</param>
        /// <param name="replyToId">Optional ID of message being replied to.</param>
        /// <param name="attachmentIds">Optional list of attachment IDs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created message DTO.</returns>
        Task<MessageDto> SendMessageViaHttpAsync(string chatId, string content, string? replyToId = null, List<string>? attachmentIds = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Forwards an existing message into another chat as an independent copy.
        /// </summary>
        /// <param name="targetChatId">The chat to send the copy to.</param>
        /// <param name="messageId">The source message ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created forwarded message DTO.</returns>
        Task<MessageDto> ForwardMessageAsync(string targetChatId, string messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Edits an existing message.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="content">The new content.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EditMessageAsync(string messageId, string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a message.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteMessageAsync(string messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends typing indicator.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendTypingIndicatorAsync(string chatId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves message history for a chat.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="limit">Number of messages to retrieve.</param>
        /// <param name="offset">Number of messages to skip.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of messages.</returns>
        Task<List<MessageDto>> GetMessageHistoryAsync(string chatId, int limit = 50, int offset = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for messages within a specific chat.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="query">The search query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of matching messages.</returns>
        Task<List<MessageDto>> SearchMessagesAsync(string chatId, string query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the list of chats for the current user.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of chats.</returns>
        Task<List<ChatDto>> GetChatsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a specific chat by ID.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The chat DTO, or null if not found.</returns>
        Task<ChatDto?> GetChatAsync(string chatId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new chat.
        /// </summary>
        /// <param name="name">The chat name.</param>
        /// <param name="participantIds">The list of participant user IDs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created chat DTO.</returns>
        Task<ChatDto> CreateChatAsync(string name, List<string> participantIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current user's presence status (including Invisible).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The status update DTO.</returns>
        Task<StatusUpdateDto> GetMyStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the current user's presence status (Online or Invisible).
        /// </summary>
        /// <param name="status">The desired status.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The applied status update DTO.</returns>
        Task<StatusUpdateDto> SetMyStatusAsync(Shared.Enums.UserStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a reaction to a message.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="messageId">The message ID.</param>
        /// <param name="emoji">The emoji for the reaction.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated message DTO.</returns>
        Task<MessageDto> AddReactionAsync(string chatId, string messageId, string emoji, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a reaction from a message.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="messageId">The message ID.</param>
        /// <param name="emoji">The emoji of the reaction to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated message DTO.</returns>
        Task<MessageDto> RemoveReactionAsync(string chatId, string messageId, string emoji, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the list of folders for the current user.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of folders.</returns>
        Task<List<ChatFolderDto>> GetFoldersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        /// <param name="name">The folder name.</param>
        /// <param name="chatIds">The list of chat IDs to include in the folder.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created folder DTO.</returns>
        Task<ChatFolderDto> CreateFolderAsync(string name, List<string> chatIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing folder.
        /// </summary>
        /// <param name="folderId">The folder ID.</param>
        /// <param name="name">The folder name.</param>
        /// <param name="chatIds">The list of chat IDs to include in the folder.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated folder DTO.</returns>
        Task<ChatFolderDto> UpdateFolderAsync(string folderId, string name, List<string> chatIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a folder.
        /// </summary>
        /// <param name="folderId">The folder ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteFolderAsync(string folderId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a chat and all its associated data.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteChatAsync(string chatId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Leaves a group chat.
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task LeaveChatAsync(string chatId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds users to a group chat (owner only).
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="userIds">User IDs to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated chat DTO.</returns>
        Task<ChatDto> AddChatParticipantsAsync(
            string chatId,
            List<string> userIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a user from a group chat (owner only).
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="userId">The user ID to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated chat DTO.</returns>
        Task<ChatDto> RemoveChatParticipantAsync(
            string chatId,
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates group chat name/description (owner only).
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="name">New name.</param>
        /// <param name="description">Optional new description.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated chat DTO.</returns>
        Task<ChatDto> UpdateChatAsync(
            string chatId,
            string name,
            string? description = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a new avatar for a group chat (owner only).
        /// </summary>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="filePath">Local image file path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated chat DTO.</returns>
        Task<ChatDto> UploadChatAvatarAsync(string chatId, string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Occurs when a chat is deleted.
        /// </summary>
        event EventHandler<string>? ChatDeleted;

        /// <summary>
        /// Occurs when a chat is created (e.g. another user started a conversation with you).
        /// </summary>
        event EventHandler<ChatDto>? ChatCreated;

        /// <summary>
        /// Occurs when chat metadata is updated (e.g. group name or avatar changed).
        /// </summary>
        event EventHandler<ChatDto>? ChatUpdated;

        /// <summary>
        /// Occurs when a call-related message is received.
        /// </summary>
        event EventHandler<Shared.Dtos.WebSocketMessageEnvelope>? CallMessageReceived;

        /// <summary>
        /// Sends a call-related message via WebSocket.
        /// </summary>
        /// <param name="envelope">The WebSocket message envelope containing call data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendCallMessageAsync(Shared.Dtos.WebSocketMessageEnvelope envelope, CancellationToken cancellationToken = default);
    }
}
