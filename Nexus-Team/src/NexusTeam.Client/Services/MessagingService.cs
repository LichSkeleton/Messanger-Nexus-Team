namespace NexusTeam.Client.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Net.WebSockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Client.Models;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using NexusTeam.Shared.Exceptions;
    using Serilog;

    /// <summary>
    /// Implementation of messaging service using WebSocket and REST API with auto-reconnection and offline queue.
    /// </summary>
    public class MessagingService : IMessagingService, IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly ILogger logger;
        private readonly IOfflineMessageQueue offlineQueue;
        private readonly IErrorHandlingService errorHandler;
        private readonly IAuthenticationService authenticationService;
        private readonly ServerConfiguration serverConfiguration;
        private readonly ReconnectionStrategy reconnectionStrategy;
        private readonly HashSet<string> sentMessageIds;
        private ClientWebSocket? webSocket;
        private CancellationTokenSource? receiveCancellationTokenSource;
        private CancellationTokenSource? reconnectCancellationTokenSource;
        private string? lastAccessToken;
        private bool isDisposed;
        private bool isReconnecting;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagingService"/> class.
        /// </summary>
        /// <param name="httpClient">HTTP client for REST API calls.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="offlineQueue">Offline message queue service.</param>
        /// <param name="errorHandler">Error handling service.</param>
        /// <param name="authenticationService">Authentication service for current user context.</param>
        /// <param name="serverConfiguration">Server configuration from CLI arguments.</param>
        public MessagingService(
            HttpClient httpClient,
            ILogger logger,
            IOfflineMessageQueue offlineQueue,
            IErrorHandlingService errorHandler,
            IAuthenticationService authenticationService,
            ServerConfiguration serverConfiguration)
        {
            this.httpClient = httpClient;
            this.logger = logger;
            this.offlineQueue = offlineQueue;
            this.errorHandler = errorHandler;
            this.authenticationService = authenticationService;
            this.serverConfiguration = serverConfiguration;
            this.reconnectionStrategy = new ReconnectionStrategy();
            this.sentMessageIds = new HashSet<string>();
        }

        /// <inheritdoc/>
        public bool IsConnected => this.webSocket?.State == WebSocketState.Open;

        /// <inheritdoc/>
        public event EventHandler<MessageDto>? MessageReceived;

        /// <inheritdoc/>
        public event EventHandler<MessageDto>? MessageEdited;

        /// <inheritdoc/>
        public event EventHandler<string>? MessageDeleted;

        /// <inheritdoc/>
        public event EventHandler<string>? UserTyping;

        /// <inheritdoc/>
        public event EventHandler<StatusUpdateDto>? UserStatusChanged;

        /// <inheritdoc/>
        public event EventHandler<AvatarUpdateDto>? UserAvatarChanged;

        /// <inheritdoc/>
        public event EventHandler<bool>? ConnectionStateChanged;

        /// <inheritdoc/>
        public event EventHandler<MessageDto>? MessageReactionUpdated;

        /// <inheritdoc/>
        public event EventHandler<string>? ChatDeleted;

        /// <inheritdoc/>
        public event EventHandler<ChatDto>? ChatCreated;

        /// <inheritdoc/>
        public event EventHandler<ChatDto>? ChatUpdated;

        /// <inheritdoc/>
        public event EventHandler<Shared.Dtos.WebSocketMessageEnvelope>? CallMessageReceived;

        /// <inheritdoc/>
        public async Task ConnectAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            this.logger.Information("ConnectAsync called, IsConnected={IsConnected}, Token length={TokenLength}", this.IsConnected, accessToken?.Length ?? 0);

            if (this.IsConnected)
            {
                this.logger.Warning("WebSocket is already connected");
                return;
            }

            this.lastAccessToken = accessToken;

            try
            {
                this.logger.Information("Creating new ClientWebSocket instance");
                this.webSocket = new ClientWebSocket();

                var wsUri = new Uri(this.serverConfiguration.WebSocketUrl);
                this.logger.Information("Attempting to connect to WebSocket at {Uri}", wsUri);

                await this.webSocket.ConnectAsync(wsUri, cancellationToken);

                this.logger.Information("WebSocket connected successfully, State={State}", this.webSocket.State);

                // Send authentication message
                try
                {
                    this.logger.Information("Sending authentication message");
                    var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                    var authPayload = new NexusTeam.Shared.Dtos.AuthPayload { Token = accessToken };
                    var authEnvelope = new WebSocketMessageEnvelope
                    {
                        Type = Shared.Enums.WebSocketMessageType.Authenticate,
                        Payload = JsonSerializer.SerializeToElement(authPayload, options),
                    };

                    if (this.webSocket.State == WebSocketState.Open)
                    {
                        await this.SendWebSocketMessageAsync(authEnvelope, cancellationToken);
                        this.logger.Information("Authentication message sent successfully");
                    }
                    else
                    {
                        this.logger.Warning("WebSocket state is {State}, cannot send authentication message", this.webSocket.State);
                        throw new InvalidOperationException($"WebSocket is not in Open state: {this.webSocket.State}");
                    }
                }
                catch (WebSocketException wsEx)
                {
                    this.logger.Error(wsEx, "Failed to send authentication message: {State}", this.webSocket.State);
                    throw;
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, "Error sending authentication message");
                    throw;
                }

                // Start receive messages task before waiting
                this.logger.Information("Starting receive messages task");
                this.receiveCancellationTokenSource = new CancellationTokenSource();
                _ = Task.Run(() => this.ReceiveMessagesAsync(this.receiveCancellationTokenSource.Token), this.receiveCancellationTokenSource.Token);

                this.OnConnectionStateChanged(true);
                this.errorHandler.ShowInfo("Connected to server");

                this.logger.Information("Flushing offline message queue");
                try
                {
                    await this.FlushOfflineQueueAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "Error flushing offline queue, continuing");
                }

                this.logger.Information("WebSocket connection and initialization completed");
            }
            catch (WebSocketException wsEx)
            {
                this.logger.Error(wsEx, "WebSocket connection failed: {Message}", wsEx.Message);
                this.OnConnectionStateChanged(false);
                this.errorHandler.HandleError(wsEx, $"Failed to connect to server: {wsEx.Message}", false);
                throw;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to connect to WebSocket: {Message}", ex.Message);
                this.OnConnectionStateChanged(false);
                this.errorHandler.HandleError(ex, "Failed to connect to server", false);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            this.reconnectCancellationTokenSource?.Cancel();

            if (this.webSocket == null || this.webSocket.State != WebSocketState.Open)
            {
                return;
            }

            try
            {
                this.receiveCancellationTokenSource?.Cancel();
                await this.webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", cancellationToken);
                this.logger.Information("WebSocket disconnected");
                this.OnConnectionStateChanged(false);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error disconnecting WebSocket");
            }
            finally
            {
                this.webSocket?.Dispose();
                this.webSocket = null;
                this.receiveCancellationTokenSource?.Dispose();
                this.receiveCancellationTokenSource = null;
            }
        }

        /// <inheritdoc/>
        public async Task SendMessageAsync(string chatId, string content, string? replyToId = null, List<string>? attachmentIds = null, CancellationToken cancellationToken = default)
        {
            var messageId = Guid.NewGuid().ToString();
            var userId = this.authenticationService.CurrentUser?.Id ?? "unknown";

            if (!this.IsConnected)
            {
                var queuedMessage = new QueuedMessage
                {
                    MessageId = messageId,
                    ChatId = chatId,
                    Content = content,
                    ReplyToId = replyToId,
                    QueuedAt = DateTime.UtcNow,
                    Attempts = 0,
                };

                await this.offlineQueue.EnqueueAsync(queuedMessage, cancellationToken);
                this.logger.Information(
                    "Message {MessageId} queued for offline sending to chat {ChatId} by user {UserId}",
                    messageId,
                    chatId,
                    userId);
                this.errorHandler.ShowWarning("Message queued. Will be sent when connection is restored.");
                return;
            }

            var request = new SendMessageRequest
            {
                ChatId = chatId,
                Content = content,
                ReplyToId = replyToId,
                AttachmentIds = attachmentIds ?? new List<string>(),
            };

            var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
            var envelope = new WebSocketMessageEnvelope
            {
                Type = Shared.Enums.WebSocketMessageType.NewMessage,
                Payload = JsonSerializer.SerializeToElement(request, options),
                MessageId = messageId,
            };

            await this.SendWebSocketMessageAsync(envelope, cancellationToken);
            this.sentMessageIds.Add(messageId);

            this.logger.Information(
                "Message {MessageId} sent to chat {ChatId} by user {UserId} with {AttachmentCount} attachments",
                messageId,
                chatId,
                userId,
                attachmentIds?.Count ?? 0);
        }

        /// <inheritdoc/>
        public async Task<MessageDto> SendMessageViaHttpAsync(string chatId, string content, string? replyToId = null, List<string>? attachmentIds = null, CancellationToken cancellationToken = default)
        {
            var request = new SendMessageRequest
            {
                ChatId = chatId,
                Content = content,
                ReplyToId = replyToId,
                AttachmentIds = attachmentIds ?? new List<string>(),
            };

            var response = await this.httpClient.PostAsJsonAsync(
                $"/api/chats/{chatId}/messages",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var messageDto = await response.Content.ReadFromJsonAsync<MessageDto>(cancellationToken: cancellationToken);
            if (messageDto == null)
            {
                throw new InvalidOperationException("Failed to deserialize message response");
            }

            this.logger.Information(
                "Message sent via HTTP to chat {ChatId} with {AttachmentCount} attachments",
                chatId,
                attachmentIds?.Count ?? 0);

            return messageDto;
        }

        /// <inheritdoc/>
        public async Task<MessageDto> ForwardMessageAsync(string targetChatId, string messageId, CancellationToken cancellationToken = default)
        {
            var request = new ForwardMessageRequest
            {
                MessageId = messageId,
            };

            var response = await this.httpClient.PostAsJsonAsync(
                $"/api/chats/{targetChatId}/messages/forward",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var messageDto = await response.Content.ReadFromJsonAsync<MessageDto>(cancellationToken: cancellationToken);
            if (messageDto == null)
            {
                throw new InvalidOperationException("Failed to deserialize forwarded message response");
            }

            this.logger.Information(
                "Message {MessageId} forwarded to chat {ChatId}",
                messageId,
                targetChatId);

            return messageDto;
        }

        /// <inheritdoc/>
        public async Task EditMessageAsync(string messageId, string content, CancellationToken cancellationToken = default)
        {
            var request = new EditMessageRequest
            {
                MessageId = messageId,
                Content = content,
            };

            var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
            var envelope = new WebSocketMessageEnvelope
            {
                Type = Shared.Enums.WebSocketMessageType.EditMessage,
                Payload = JsonSerializer.SerializeToElement(request, options),
                MessageId = messageId,
            };

            await this.SendWebSocketMessageAsync(envelope, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteMessageAsync(string messageId, CancellationToken cancellationToken = default)
        {
            var request = new DeleteMessageRequest
            {
                MessageId = messageId,
            };

            var envelope = new WebSocketMessageEnvelope
            {
                Type = Shared.Enums.WebSocketMessageType.DeleteMessage,
                Payload = JsonSerializer.SerializeToElement(request),
                MessageId = messageId,
            };

            await this.SendWebSocketMessageAsync(envelope, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task SendTypingIndicatorAsync(string chatId, CancellationToken cancellationToken = default)
        {
            var payload = new TypingIndicatorPayload { ChatId = chatId };
            var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;

            var envelope = new WebSocketMessageEnvelope
            {
                Type = Shared.Enums.WebSocketMessageType.Typing,
                Payload = JsonSerializer.SerializeToElement(payload, options),
            };

            await this.SendWebSocketMessageAsync(envelope, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<MessageDto>> GetMessageHistoryAsync(string chatId, int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this.httpClient.GetAsync($"/api/chats/{chatId}/messages?limit={limit}&offset={offset}", cancellationToken);
                response.EnsureSuccessStatusCode();

                var messages = await response.Content.ReadFromJsonAsync<List<MessageDto>>(cancellationToken);

                if (messages == null)
                {
                    return new List<MessageDto>();
                }

                // CRITICAL: Validate all messages belong to the requested chat
                var validatedMessages = messages.Where(m => m.ChatId == chatId).ToList();

                if (validatedMessages.Count != messages.Count)
                {
                    this.logger.Warning(
                        "Received {Total} messages, but only {Valid} belong to chat {ChatId}",
                        messages.Count,
                        validatedMessages.Count,
                        chatId);
                }

                this.logger.Information(
                    "Loaded {Count} messages for chat {ChatId}",
                    validatedMessages.Count,
                    chatId);

                return validatedMessages;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to retrieve message history for chat {ChatId}", chatId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<MessageDto>> SearchMessagesAsync(string chatId, string query, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this.httpClient.GetAsync(
                    $"/api/messages/{chatId}/search?query={Uri.EscapeDataString(query)}",
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var messages = await response.Content.ReadFromJsonAsync<List<MessageDto>>(cancellationToken);
                return messages ?? new List<MessageDto>();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to search messages in chat {ChatId} with query '{Query}'", chatId, query);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<ChatDto>> GetChatsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this.httpClient.GetAsync("/api/chats", cancellationToken);
                response.EnsureSuccessStatusCode();

                var chats = await response.Content.ReadFromJsonAsync<List<ChatDto>>(cancellationToken);
                return chats ?? new List<ChatDto>();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to retrieve chats");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ChatDto?> GetChatAsync(string chatId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this.httpClient.GetAsync($"/api/chats/{chatId}", cancellationToken);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<ChatDto>(cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to retrieve chat {ChatId}", chatId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ChatDto> CreateChatAsync(string name, List<string> participantIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var trimmedName = name?.Trim() ?? string.Empty;
                var uniqueParticipantIds = participantIds?.Distinct().ToList() ?? new List<string>();

                var currentUserId = this.authenticationService.CurrentUser?.Id;
                if (!string.IsNullOrEmpty(currentUserId) && !uniqueParticipantIds.Contains(currentUserId))
                {
                    uniqueParticipantIds.Add(currentUserId);
                }

                var chatType = uniqueParticipantIds.Count == 2 ? ChatType.DirectMessage : ChatType.Group;

                var request = new CreateChatRequest
                {
                    Name = trimmedName,
                    Type = chatType,
                    ParticipantIds = uniqueParticipantIds,
                };

                this.logger.Information("Creating chat '{ChatName}' with {ParticipantCount} participants", trimmedName, uniqueParticipantIds.Count);

                var response = await this.httpClient.PostAsJsonAsync("/api/chats", request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var chat = await response.Content.ReadFromJsonAsync<ChatDto>(cancellationToken);
                    if (chat == null)
                    {
                        this.logger.Error("Chat creation returned null response");
                        throw new InvalidOperationException("Chat creation response was null");
                    }

                    this.logger.Information("Chat {ChatId} created successfully", chat.Id);
                    return chat;
                }

                await this.HandleErrorResponseAsync(response, trimmedName, cancellationToken);

                throw new InvalidOperationException("Unexpected error during chat creation");
            }
            catch (Exception ex) when (ex is ValidationException || ex is UnauthorizedException || ex is InvalidOperationException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                this.logger.Error(ex, "Network error during chat creation");
                throw new InvalidOperationException("Unable to connect to server. Please check your network connection.", ex);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Unexpected error during chat creation");
                throw new InvalidOperationException("An unexpected error occurred while creating the chat.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<StatusUpdateDto> GetMyStatusAsync(CancellationToken cancellationToken = default)
        {
            var response = await this.httpClient.GetAsync("/api/users/status", cancellationToken);
            response.EnsureSuccessStatusCode();
            var status = await response.Content.ReadFromJsonAsync<StatusUpdateDto>(cancellationToken);
            return status ?? new StatusUpdateDto { Status = UserStatus.Offline };
        }

        /// <inheritdoc/>
        public async Task<StatusUpdateDto> SetMyStatusAsync(UserStatus status, CancellationToken cancellationToken = default)
        {
            var request = new UpdateUserStatusRequest { Status = status };
            var response = await this.httpClient.PutAsJsonAsync("/api/users/status", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<StatusUpdateDto>(cancellationToken);
            return result ?? new StatusUpdateDto { Status = status };
        }

        /// <inheritdoc/>
        public async Task<MessageDto> AddReactionAsync(string chatId, string messageId, string emoji, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new AddReactionRequest
                {
                    Emoji = emoji,
                };

                var response = await this.httpClient.PostAsJsonAsync(
                    $"/api/chats/{chatId}/messages/{messageId}/reactions",
                    request,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var message = await response.Content.ReadFromJsonAsync<MessageDto>(cancellationToken: cancellationToken);
                if (message == null)
                {
                    throw new InvalidOperationException("Failed to deserialize message response");
                }

                this.logger.Information("Reaction {Emoji} added to message {MessageId}", emoji, messageId);
                return message;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to add reaction to message {MessageId}", messageId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<MessageDto> RemoveReactionAsync(string chatId, string messageId, string emoji, CancellationToken cancellationToken = default)
        {
            try
            {
                // URL encode emoji for path parameter
                var encodedEmoji = Uri.EscapeDataString(emoji);

                var response = await this.httpClient.DeleteAsync(
                    $"/api/chats/{chatId}/messages/{messageId}/reactions/{encodedEmoji}",
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var message = await response.Content.ReadFromJsonAsync<MessageDto>(cancellationToken: cancellationToken);
                if (message == null)
                {
                    throw new InvalidOperationException("Failed to deserialize message response");
                }

                this.logger.Information("Reaction {Emoji} removed from message {MessageId}", emoji, messageId);
                return message;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to remove reaction from message {MessageId}", messageId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<ChatFolderDto>> GetFoldersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this.httpClient.GetAsync("/api/folders", cancellationToken);
                response.EnsureSuccessStatusCode();

                var folders = await response.Content.ReadFromJsonAsync<List<ChatFolderDto>>(cancellationToken);
                return folders ?? new List<ChatFolderDto>();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to retrieve folders");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ChatFolderDto> CreateFolderAsync(string name, List<string> chatIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var trimmedName = name?.Trim() ?? string.Empty;

                var request = new CreateChatFolderRequest
                {
                    Name = trimmedName,
                    ChatIds = chatIds?.Distinct().ToList() ?? new List<string>(),
                };

                this.logger.Information("Creating folder '{FolderName}' with {ChatCount} chats", trimmedName, request.ChatIds.Count);

                var response = await this.httpClient.PostAsJsonAsync("/api/folders", request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var folder = await response.Content.ReadFromJsonAsync<ChatFolderDto>(cancellationToken);
                    if (folder == null)
                    {
                        this.logger.Error("Folder creation returned null response");
                        throw new InvalidOperationException("Folder creation response was null");
                    }

                    this.logger.Information("Folder {FolderId} created successfully", folder.Id);
                    return folder;
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                this.logger.Warning("Folder creation failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();

                throw new InvalidOperationException("Unexpected error during folder creation");
            }
            catch (Exception ex) when (ex is ValidationException || ex is UnauthorizedException || ex is InvalidOperationException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                this.logger.Error(ex, "Network error during folder creation");
                throw new InvalidOperationException("Unable to connect to server. Please check your network connection.", ex);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Unexpected error during folder creation");
                throw new InvalidOperationException("An unexpected error occurred while creating the folder.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<ChatFolderDto> UpdateFolderAsync(string folderId, string name, List<string> chatIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var trimmedName = name?.Trim() ?? string.Empty;

                var request = new CreateChatFolderRequest
                {
                    Name = trimmedName,
                    ChatIds = chatIds?.Distinct().ToList() ?? new List<string>(),
                };

                this.logger.Information("Updating folder {FolderId} '{FolderName}' with {ChatCount} chats", folderId, trimmedName, request.ChatIds.Count);

                var response = await this.httpClient.PutAsJsonAsync($"/api/folders/{folderId}", request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var folder = await response.Content.ReadFromJsonAsync<ChatFolderDto>(cancellationToken);
                    if (folder == null)
                    {
                        this.logger.Error("Folder update returned null response");
                        throw new InvalidOperationException("Folder update response was null");
                    }

                    this.logger.Information("Folder {FolderId} updated successfully", folderId);
                    return folder;
                }

                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                this.logger.Warning("Folder update failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();

                throw new InvalidOperationException("Unexpected error during folder update");
            }
            catch (Exception ex) when (ex is ValidationException || ex is UnauthorizedException || ex is InvalidOperationException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                this.logger.Error(ex, "Network error during folder update");
                throw new InvalidOperationException("Unable to connect to server. Please check your network connection.", ex);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Unexpected error during folder update");
                throw new InvalidOperationException("An unexpected error occurred while updating the folder.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task DeleteFolderAsync(string folderId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this.httpClient.DeleteAsync($"/api/folders/{folderId}", cancellationToken);
                response.EnsureSuccessStatusCode();

                this.logger.Information("Folder {FolderId} deleted successfully", folderId);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to delete folder {FolderId}", folderId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task DeleteChatAsync(string chatId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this.httpClient.DeleteAsync($"/api/chats/{chatId}", cancellationToken);
                response.EnsureSuccessStatusCode();

                this.logger.Information("Chat {ChatId} deleted successfully", chatId);

                // Raise event for local handling
                this.OnChatDeleted(chatId);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to delete chat {ChatId}", chatId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task LeaveChatAsync(string chatId, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this.httpClient.PostAsync($"/api/chats/{chatId}/leave", null, cancellationToken);
                response.EnsureSuccessStatusCode();
                this.logger.Information("Left chat {ChatId}", chatId);
                this.OnChatDeleted(chatId);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to leave chat {ChatId}", chatId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ChatDto> AddChatParticipantsAsync(
            string chatId,
            List<string> userIds,
            CancellationToken cancellationToken = default)
        {
            var request = new AddChatParticipantsRequest
            {
                UserIds = userIds ?? new List<string>(),
            };

            var response = await this.httpClient.PostAsJsonAsync(
                $"/api/chats/{chatId}/participants",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                this.logger.Warning("Add participants failed: {Status} - {Error}", response.StatusCode, error);
                throw new InvalidOperationException($"Failed to add members: {error}");
            }

            var chat = await response.Content.ReadFromJsonAsync<ChatDto>(cancellationToken);
            return chat ?? throw new InvalidOperationException("Add members returned null.");
        }

        /// <inheritdoc/>
        public async Task<ChatDto> RemoveChatParticipantAsync(
            string chatId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var response = await this.httpClient.DeleteAsync(
                $"/api/chats/{chatId}/participants/{userId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                this.logger.Warning("Remove participant failed: {Status} - {Error}", response.StatusCode, error);
                throw new InvalidOperationException($"Failed to remove member: {error}");
            }

            var chat = await response.Content.ReadFromJsonAsync<ChatDto>(cancellationToken);
            return chat ?? throw new InvalidOperationException("Remove member returned null.");
        }

        /// <inheritdoc/>
        public async Task<ChatDto> UpdateChatAsync(
            string chatId,
            string name,
            string? description = null,
            CancellationToken cancellationToken = default)
        {
            var request = new UpdateChatRequest
            {
                Name = name,
                Description = description,
            };

            var response = await this.httpClient.PutAsJsonAsync($"/api/chats/{chatId}", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                this.logger.Warning("Update chat failed: {Status} - {Error}", response.StatusCode, error);
                throw new InvalidOperationException($"Failed to update chat: {error}");
            }

            var chat = await response.Content.ReadFromJsonAsync<ChatDto>(cancellationToken);
            return chat ?? throw new InvalidOperationException("Update chat returned null.");
        }

        /// <inheritdoc/>
        public async Task<ChatDto> UploadChatAvatarAsync(string chatId, string filePath, CancellationToken cancellationToken = default)
        {
            if (!System.IO.File.Exists(filePath))
            {
                throw new System.IO.FileNotFoundException("Avatar file not found", filePath);
            }

            await using var fileStream = System.IO.File.OpenRead(filePath);
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(streamContent, "file", System.IO.Path.GetFileName(filePath));

            var response = await this.httpClient.PostAsync($"/api/chats/{chatId}/avatar", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                this.logger.Warning("Chat avatar upload failed: {Status} - {Error}", response.StatusCode, error);
                throw new InvalidOperationException($"Failed to upload chat avatar: {error}");
            }

            var chat = await response.Content.ReadFromJsonAsync<ChatDto>(cancellationToken);
            return chat ?? throw new InvalidOperationException("Chat avatar upload returned null.");
        }

        /// <inheritdoc/>
        public async Task SendCallMessageAsync(Shared.Dtos.WebSocketMessageEnvelope envelope, CancellationToken cancellationToken = default)
        {
            await this.SendWebSocketMessageAsync(envelope, cancellationToken);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources used by the service.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.receiveCancellationTokenSource?.Cancel();
                    this.receiveCancellationTokenSource?.Dispose();
                    this.reconnectCancellationTokenSource?.Cancel();
                    this.reconnectCancellationTokenSource?.Dispose();
                    this.webSocket?.Dispose();
                }

                this.isDisposed = true;
            }
        }

        private async Task HandleErrorResponseAsync(HttpResponseMessage response, string chatName, CancellationToken cancellationToken)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.BadRequest:
                    this.logger.Warning("Chat creation validation failed: {Error}", errorContent);
                    var validationMessage = this.ExtractErrorMessage(errorContent) ?? "Invalid chat creation request. Please check your input.";
                    throw new ValidationException(validationMessage);

                case System.Net.HttpStatusCode.Unauthorized:
                    this.logger.Warning("Unauthorized chat creation attempt");
                    throw new UnauthorizedException("You must be logged in to create a chat.");

                case System.Net.HttpStatusCode.Conflict:
                    this.logger.Warning("Duplicate chat creation attempt: {ChatName}", chatName);
                    var conflictMessage = this.ExtractErrorMessage(errorContent) ?? $"A chat with the name '{chatName}' already exists.";
                    throw new InvalidOperationException(conflictMessage);

                default:
                    this.logger.Error("Chat creation failed with status {StatusCode}: {Error}", response.StatusCode, errorContent);
                    throw new InvalidOperationException($"Failed to create chat. Server returned status {response.StatusCode}.");
            }
        }

        private string? ExtractErrorMessage(string errorContent)
        {
            if (string.IsNullOrWhiteSpace(errorContent))
            {
                return null;
            }

            try
            {
                var errorObject = JsonSerializer.Deserialize<JsonElement>(errorContent);

                if (errorObject.TryGetProperty("error", out var errorProp))
                {
                    return errorProp.GetString();
                }

                if (errorObject.TryGetProperty("message", out var messageProp))
                {
                    return messageProp.GetString();
                }

                if (errorObject.TryGetProperty("title", out var titleProp))
                {
                    return titleProp.GetString();
                }
            }
            catch (JsonException)
            {
                return errorContent.Length > 200 ? errorContent.Substring(0, 200) : errorContent;
            }

            return null;
        }

        private async Task SendWebSocketMessageAsync(WebSocketMessageEnvelope envelope, CancellationToken cancellationToken)
        {
            if (this.webSocket == null || this.webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException($"WebSocket is not connected. State: {this.webSocket?.State ?? WebSocketState.None}");
            }

            try
            {
                var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                var json = JsonSerializer.Serialize(envelope, options);
                var bytes = Encoding.UTF8.GetBytes(json);
                var segment = new ArraySegment<byte>(bytes);

                await this.webSocket.SendAsync(segment, System.Net.WebSockets.WebSocketMessageType.Text, true, cancellationToken);
                this.logger.Debug("Sent WebSocket message: {Type}", envelope.Type);
            }
            catch (WebSocketException wsEx)
            {
                this.logger.Error(wsEx, "WebSocket exception while sending message: {Type}, State: {State}", envelope.Type, this.webSocket?.State ?? WebSocketState.None);
                throw;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to send WebSocket message: {Type}", envelope.Type);
                throw;
            }
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (this.webSocket != null && this.webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = await this.webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                        if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                        {
                            if (this.webSocket.State == WebSocketState.Open || this.webSocket.State == WebSocketState.CloseReceived)
                            {
                                try
                                {
                                    await this.webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closed", cancellationToken);
                                }
                                catch (Exception ex)
                                {
                                    this.logger.Debug(ex, "Error closing WebSocket after Close message");
                                }
                            }

                            this.OnConnectionStateChanged(false);
                            await this.TriggerReconnectionAsync();
                            break;
                        }

                        if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
                        {
                            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            await this.ProcessWebSocketMessageAsync(json);
                        }
                    }
                    catch (WebSocketException wsEx)
                    {
                        // Connection was closed unexpectedly
                        if (this.webSocket == null || this.webSocket.State == WebSocketState.Aborted || this.webSocket.State == WebSocketState.Closed)
                        {
                            this.logger.Debug(wsEx, "WebSocket closed during receive: {State}", this.webSocket?.State ?? WebSocketState.None);
                            this.OnConnectionStateChanged(false);
                            await this.TriggerReconnectionAsync();
                            break;
                        }

                        // Re-throw other WebSocket exceptions
                        this.logger.Warning(wsEx, "Unexpected WebSocket exception while receiving data");
                        throw;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                this.logger.Information("WebSocket receive loop cancelled");
            }
            catch (WebSocketException wsEx)
            {
                this.logger.Debug(wsEx, "WebSocket exception in receive loop: {State}", this.webSocket?.State ?? WebSocketState.None);
                this.OnConnectionStateChanged(false);
                if (this.webSocket?.State != WebSocketState.Aborted && this.webSocket?.State != WebSocketState.Closed)
                {
                    await this.TriggerReconnectionAsync();
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error in WebSocket receive loop");
                this.OnConnectionStateChanged(false);
                await this.TriggerReconnectionAsync();
            }
        }

        private async Task ProcessWebSocketMessageAsync(string json)
        {
            try
            {
                var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                var envelope = JsonSerializer.Deserialize<WebSocketMessageEnvelope>(json, options);
                if (envelope == null)
                {
                    this.logger.Warning("Received null WebSocket message");
                    return;
                }

                this.logger.Debug("Received WebSocket message: {Type}", envelope.Type);

                await Task.Run(() =>
                {
                    var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                    switch (envelope.Type)
                    {
                        case Shared.Enums.WebSocketMessageType.NewMessage:
                            if (envelope.Payload.HasValue)
                            {
                                var message = JsonSerializer.Deserialize<MessageDto>(envelope.Payload.Value.GetRawText(), options);
                                if (message != null)
                                {
                                    this.logger.Information(
                                        "Received message {MessageId} for chat {ChatId} from sender {SenderId}",
                                        message.Id,
                                        message.ChatId,
                                        message.SenderId);
                                    this.OnMessageReceived(message);
                                }
                            }

                            break;

                        case Shared.Enums.WebSocketMessageType.EditMessage:
                            if (envelope.Payload.HasValue)
                            {
                                var message = JsonSerializer.Deserialize<MessageDto>(envelope.Payload.Value.GetRawText(), options);
                                if (message != null)
                                {
                                    this.OnMessageEdited(message);
                                }
                            }

                            break;

                        case Shared.Enums.WebSocketMessageType.DeleteMessage:
                            if (!string.IsNullOrEmpty(envelope.MessageId))
                            {
                                this.OnMessageDeleted(envelope.MessageId);
                            }

                            break;

                        case Shared.Enums.WebSocketMessageType.Typing:
                            if (envelope.Payload.HasValue)
                            {
                                var typingData = JsonSerializer.Deserialize<TypingIndicatorPayload>(envelope.Payload.Value.GetRawText(), options);
                                if (typingData != null)
                                {
                                    this.OnUserTyping(typingData.UserId);
                                }
                            }

                            break;

                        case Shared.Enums.WebSocketMessageType.StatusUpdate:
                            if (envelope.Payload.HasValue)
                            {
                                var statusUpdate = JsonSerializer.Deserialize<StatusUpdateDto>(envelope.Payload.Value.GetRawText(), options);
                                if (statusUpdate != null)
                                {
                                    this.OnUserStatusChanged(statusUpdate);
                                }
                            }

                            break;

                        case Shared.Enums.WebSocketMessageType.AvatarUpdate:
                            if (envelope.Payload.HasValue)
                            {
                                var avatarUpdate = JsonSerializer.Deserialize<AvatarUpdateDto>(envelope.Payload.Value.GetRawText(), options);
                                if (avatarUpdate != null)
                                {
                                    this.OnUserAvatarChanged(avatarUpdate);
                                }
                            }

                            break;

                        case Shared.Enums.WebSocketMessageType.Authenticate:
                            if (envelope.Payload.HasValue)
                            {
                                try
                                {
                                    var authResponse = JsonSerializer.Deserialize<NexusTeam.Shared.Dtos.AuthenticateResponse>(envelope.Payload.Value.GetRawText(), options);
                                    if (authResponse != null && authResponse.Success)
                                    {
                                        this.logger.Information("WebSocket authentication successful. ResumeToken: {ResumeToken}", authResponse.ResumeToken ?? "none");
                                    }
                                    else
                                    {
                                        this.logger.Warning("WebSocket authentication failed");
                                    }
                                }
                                catch (JsonException jsonEx)
                                {
                                    this.logger.Warning(jsonEx, "Failed to parse authentication response");
                                }
                            }

                            break;

                        case Shared.Enums.WebSocketMessageType.MessageReaction:
                            if (envelope.Payload.HasValue)
                            {
                                var message = JsonSerializer.Deserialize<MessageDto>(envelope.Payload.Value.GetRawText(), options);
                                if (message != null)
                                {
                                    this.logger.Debug("Received reaction update for message {MessageId}", message.Id);
                                    this.OnMessageReactionUpdated(message);
                                }
                            }

                            break;

                        case Shared.Enums.WebSocketMessageType.ChatDeleted:
                            if (envelope.Payload.HasValue)
                            {
                                try
                                {
                                    var chatDeletedData = JsonSerializer.Deserialize<ChatDeletedPayload>(envelope.Payload.Value.GetRawText(), options);
                                    if (chatDeletedData != null && !string.IsNullOrEmpty(chatDeletedData.ChatId))
                                    {
                                        this.logger.Information("Received chat deleted notification for chat {ChatId}", chatDeletedData.ChatId);
                                        this.OnChatDeleted(chatDeletedData.ChatId);
                                    }
                                }
                                catch (JsonException jsonEx)
                                {
                                    this.logger.Warning(jsonEx, "Failed to parse chat deleted payload");
                                }
                            }

                            break;

                        case Shared.Enums.WebSocketMessageType.ChatCreated:
                            if (envelope.Payload.HasValue)
                            {
                                try
                                {
                                    var createdChat = JsonSerializer.Deserialize<ChatDto>(envelope.Payload.Value.GetRawText(), options);
                                    if (createdChat != null && !string.IsNullOrEmpty(createdChat.Id))
                                    {
                                        this.logger.Information("Received chat created notification for chat {ChatId}", createdChat.Id);
                                        this.OnChatCreated(createdChat);
                                    }
                                }
                                catch (JsonException jsonEx)
                                {
                                    this.logger.Warning(jsonEx, "Failed to parse chat created payload");
                                }
                            }

                            break;

                        case Shared.Enums.WebSocketMessageType.ChatUpdated:
                            if (envelope.Payload.HasValue)
                            {
                                try
                                {
                                    var updatedChat = JsonSerializer.Deserialize<ChatDto>(envelope.Payload.Value.GetRawText(), options);
                                    if (updatedChat != null && !string.IsNullOrEmpty(updatedChat.Id))
                                    {
                                        this.logger.Information("Received chat updated notification for chat {ChatId}", updatedChat.Id);
                                        this.OnChatUpdated(updatedChat);
                                    }
                                }
                                catch (JsonException jsonEx)
                                {
                                    this.logger.Warning(jsonEx, "Failed to parse chat updated payload");
                                }
                            }

                            break;

                        case Shared.Enums.WebSocketMessageType.Error:
                            this.logger.Error("WebSocket error: {Error}", envelope.Error);
                            break;

                        case Shared.Enums.WebSocketMessageType.CallRequest:
                        case Shared.Enums.WebSocketMessageType.CallAnswer:
                        case Shared.Enums.WebSocketMessageType.CallReject:
                        case Shared.Enums.WebSocketMessageType.CallEnd:
                        case Shared.Enums.WebSocketMessageType.CallSdpOffer:
                        case Shared.Enums.WebSocketMessageType.CallSdpAnswer:
                        case Shared.Enums.WebSocketMessageType.CallIceCandidate:
                        case Shared.Enums.WebSocketMessageType.CallAudioData:
                            // Call messages are forwarded to CallService via event
                            this.logger.Information("Received call message type: {Type}, forwarding to CallService", envelope.Type);
                            this.OnCallMessageReceived(envelope);
                            break;

                        default:
                            this.logger.Warning("Received unknown WebSocket message type: {Type}", envelope.Type);
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error processing WebSocket message");
            }
        }

        private void OnMessageReceived(MessageDto message)
        {
            this.MessageReceived?.Invoke(this, message);
        }

        private void OnMessageEdited(MessageDto message)
        {
            this.MessageEdited?.Invoke(this, message);
        }

        private void OnMessageDeleted(string messageId)
        {
            this.MessageDeleted?.Invoke(this, messageId);
        }

        private void OnUserTyping(string userId)
        {
            this.UserTyping?.Invoke(this, userId);
        }

        private void OnUserStatusChanged(StatusUpdateDto statusUpdate)
        {
            this.UserStatusChanged?.Invoke(this, statusUpdate);
        }

        private void OnUserAvatarChanged(AvatarUpdateDto avatarUpdate)
        {
            this.UserAvatarChanged?.Invoke(this, avatarUpdate);
        }

        private void OnConnectionStateChanged(bool isConnected)
        {
            this.ConnectionStateChanged?.Invoke(this, isConnected);
        }

        private void OnMessageReactionUpdated(MessageDto message)
        {
            this.MessageReactionUpdated?.Invoke(this, message);
        }

        private void OnChatDeleted(string chatId)
        {
            this.ChatDeleted?.Invoke(this, chatId);
        }

        private void OnChatCreated(ChatDto chat)
        {
            this.ChatCreated?.Invoke(this, chat);
        }

        private void OnChatUpdated(ChatDto chat)
        {
            this.ChatUpdated?.Invoke(this, chat);
        }

        private void OnCallMessageReceived(Shared.Dtos.WebSocketMessageEnvelope envelope)
        {
            this.CallMessageReceived?.Invoke(this, envelope);
        }

        private Task TriggerReconnectionAsync()
        {
            if (this.isReconnecting || string.IsNullOrEmpty(this.lastAccessToken))
            {
                return Task.CompletedTask;
            }

            this.isReconnecting = true;
            this.reconnectCancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(
                async () =>
            {
                var attemptNumber = 0;

                while (this.reconnectionStrategy.ShouldRetry(attemptNumber) && !this.reconnectCancellationTokenSource.Token.IsCancellationRequested)
                {
                    var delay = this.reconnectionStrategy.GetDelay(attemptNumber);
                    this.logger.Information("Attempting reconnection in {Delay} seconds (attempt {Attempt}/{Max})", delay.TotalSeconds, attemptNumber + 1, this.reconnectionStrategy.MaxRetries);
                    this.errorHandler.ShowWarning($"Connection lost. Reconnecting in {delay.TotalSeconds:F0} seconds...");

                    try
                    {
                        await Task.Delay(delay, this.reconnectCancellationTokenSource.Token);

                        this.webSocket?.Dispose();
                        this.webSocket = null;

                        await this.ConnectAsync(this.lastAccessToken!, this.reconnectCancellationTokenSource.Token);

                        this.logger.Information("Reconnection successful");
                        this.errorHandler.ShowInfo("Reconnected to server");
                        this.isReconnecting = false;
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        this.logger.Information("Reconnection cancelled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        this.logger.Warning(ex, "Reconnection attempt {Attempt} failed", attemptNumber + 1);
                        attemptNumber++;
                    }
                }

                this.isReconnecting = false;

                if (!this.IsConnected)
                {
                    this.logger.Error("Failed to reconnect after {MaxRetries} attempts", this.reconnectionStrategy.MaxRetries);
                    this.errorHandler.HandleError(
                        new InvalidOperationException("Failed to reconnect"),
                        "Could not reconnect to server. Please check your connection and try again.",
                        true);
                }
            },
                this.reconnectCancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        private async Task FlushOfflineQueueAsync(CancellationToken cancellationToken)
        {
            try
            {
                var queuedMessages = await this.offlineQueue.GetAllAsync(cancellationToken);

                if (queuedMessages.Count == 0)
                {
                    return;
                }

                this.logger.Information("Flushing {Count} queued messages", queuedMessages.Count);

                foreach (var queuedMessage in queuedMessages)
                {
                    if (this.sentMessageIds.Contains(queuedMessage.MessageId))
                    {
                        this.logger.Debug("Message {MessageId} already sent, removing from queue", queuedMessage.MessageId);
                        await this.offlineQueue.RemoveAsync(queuedMessage.MessageId, cancellationToken);
                        continue;
                    }

                    try
                    {
                        var request = new SendMessageRequest
                        {
                            ChatId = queuedMessage.ChatId,
                            Content = queuedMessage.Content,
                            ReplyToId = queuedMessage.ReplyToId,
                        };

                        var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                        var envelope = new WebSocketMessageEnvelope
                        {
                            Type = Shared.Enums.WebSocketMessageType.NewMessage,
                            Payload = JsonSerializer.SerializeToElement(request, options),
                            MessageId = queuedMessage.MessageId,
                        };

                        await this.SendWebSocketMessageAsync(envelope, cancellationToken);
                        this.sentMessageIds.Add(queuedMessage.MessageId);

                        await this.offlineQueue.RemoveAsync(queuedMessage.MessageId, cancellationToken);
                        this.logger.Information("Successfully sent queued message {MessageId}", queuedMessage.MessageId);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Error(ex, "Failed to send queued message {MessageId}", queuedMessage.MessageId);
                        queuedMessage.Attempts++;
                        await this.offlineQueue.EnqueueAsync(queuedMessage, cancellationToken);
                    }
                }

                this.logger.Information("Finished flushing offline queue");
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error flushing offline queue");
                this.errorHandler.HandleError(ex, "Failed to send some queued messages", false);
            }
        }
    }
}
