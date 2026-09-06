namespace NexusTeam.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Models;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using Serilog;

    /// <summary>
    /// Service for recording and querying call history.
    /// </summary>
    public class CallHistoryService : ICallHistoryService
    {
        private readonly ICallHistoryRepository repository;
        private readonly IIdGenerator idGenerator;
        private readonly IClock clock;
        private readonly IMessageService messageService;
        private readonly IWebSocketConnectionManager connectionManager;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallHistoryService"/> class.
        /// </summary>
        /// <param name="repository">The call history repository.</param>
        /// <param name="idGenerator">Generator for new entry identifiers.</param>
        /// <param name="clock">Abstraction over system time.</param>
        /// <param name="messageService">Message service, used to post a call-summary message into the chat.</param>
        /// <param name="connectionManager">WebSocket connection manager, used to broadcast the summary message live.</param>
        /// <param name="logger">Logger instance.</param>
        public CallHistoryService(
            ICallHistoryRepository repository,
            IIdGenerator idGenerator,
            IClock clock,
            IMessageService messageService,
            IWebSocketConnectionManager connectionManager,
            ILogger logger)
        {
            this.repository = repository;
            this.idGenerator = idGenerator;
            this.clock = clock;
            this.messageService = messageService;
            this.connectionManager = connectionManager;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task<CallHistoryDto?> RecordAsync(string reportingUserId, RecordCallHistoryRequest request, CancellationToken cancellationToken = default)
        {
            // Only the two actual participants may report an outcome for this call.
            if (reportingUserId != request.CallerId && reportingUserId != request.CalleeId)
            {
                return null;
            }

            var entry = new CallHistoryEntry
            {
                Id = this.idGenerator.GenerateId(),
                CallId = request.CallId,
                ChatId = request.ChatId,
                CallerId = request.CallerId,
                CalleeId = request.CalleeId,
                CallType = request.CallType,
                Status = request.Status,
                StartedAt = request.StartedAt,
                EndedAt = this.clock.UtcNow,
                DurationSeconds = request.DurationSeconds,
                SeenByCallee = request.Status == "Completed",
            };

            // Caller and callee both end the call around the same instant and may both report an outcome
            // within milliseconds of each other. TryCreateAsync is backed by a unique index on CallId, so
            // only the first of the two ever actually inserts (and therefore ever posts the summary message)
            // — the loser here just falls through to reading back whichever entry actually won.
            var created = await this.repository.TryCreateAsync(entry, cancellationToken);
            if (!created)
            {
                var existing = await this.repository.GetByCallIdAsync(request.CallId, cancellationToken);
                return existing == null ? null : this.MapToDto(existing);
            }

            await this.PostSummaryMessageAsync(entry, cancellationToken);
            return this.MapToDto(entry);
        }

        /// <inheritdoc/>
        public async Task RecordMissedAsync(string callId, string chatId, string callerId, string calleeId, string callType, CancellationToken cancellationToken = default)
        {
            var entry = new CallHistoryEntry
            {
                Id = this.idGenerator.GenerateId(),
                CallId = callId,
                ChatId = chatId,
                CallerId = callerId,
                CalleeId = calleeId,
                CallType = callType,
                Status = "Missed",
                StartedAt = this.clock.UtcNow,
                EndedAt = null,
                DurationSeconds = 0,
                SeenByCallee = false,
            };

            var created = await this.repository.TryCreateAsync(entry, cancellationToken);
            if (!created)
            {
                return;
            }

            await this.PostSummaryMessageAsync(entry, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CallHistoryDto>> GetByChatIdAsync(string chatId, int limit, CancellationToken cancellationToken = default)
        {
            var entries = await this.repository.GetByChatIdAsync(chatId, limit, cancellationToken);
            return entries.Select(this.MapToDto);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<CallHistoryDto>> GetUnseenMissedAsync(string userId, CancellationToken cancellationToken = default)
        {
            var entries = await this.repository.GetUnseenMissedAsync(userId, cancellationToken);
            return entries.Select(this.MapToDto);
        }

        /// <inheritdoc/>
        public async Task<bool> MarkSeenAsync(string id, string userId, CancellationToken cancellationToken = default)
        {
            var entry = await this.repository.GetByIdAsync(id, cancellationToken);
            if (entry == null || entry.CalleeId != userId)
            {
                return false;
            }

            await this.repository.MarkSeenAsync(id, cancellationToken);
            return true;
        }

        private static string BuildSummaryText(CallHistoryEntry entry)
        {
            var icon = entry.CallType == "Video" ? "📹" : "🎧";
            var label = entry.CallType == "Video" ? "Video call" : "Audio call";

            return entry.Status switch
            {
                "Completed" => $"{icon} {label} · {FormatDuration(entry.DurationSeconds)}",
                "Rejected" => $"{icon} Declined {label.ToLowerInvariant()}",
                "Missed" => $"{icon} Missed {label.ToLowerInvariant()}",
                "NoAnswer" => $"{icon} Missed {label.ToLowerInvariant()}",
                "Failed" => $"{icon} {label} failed to connect",
                _ => $"{icon} {label}",
            };
        }

        private static string FormatDuration(int totalSeconds)
        {
            var span = TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
            return span.TotalHours >= 1
                ? $"{(int)span.TotalHours}:{span.Minutes:D2}:{span.Seconds:D2}"
                : $"{span.Minutes}:{span.Seconds:D2}";
        }

        /// <summary>
        /// Posts a call-summary message ("🎧 Audio call · 1:23", "📹 Missed video call", ...) into the chat and
        /// broadcasts it live to both participants, the same way a normal chat message is delivered.
        /// </summary>
        /// <param name="entry">The call history entry describing the outcome.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes once the message has been posted (best-effort; failures are logged, not thrown).</returns>
        private async Task PostSummaryMessageAsync(CallHistoryEntry entry, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(entry.ChatId))
            {
                // No chat context (e.g. a direct call without a known chat) — nothing to post into.
                return;
            }

            try
            {
                var content = BuildSummaryText(entry);
                var request = new SendMessageRequest { ChatId = entry.ChatId, Content = content };

                // Attribute the summary message to the caller — there's no dedicated "system" sender concept
                // in the messaging pipeline, and the caller is a real participant of the chat either way.
                var message = await this.messageService.SendMessageAsync(request, entry.CallerId, cancellationToken);

                var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                var envelope = new NexusTeam.Shared.Dtos.WebSocketMessageEnvelope
                {
                    Type = WebSocketMessageType.NewMessage,
                    MessageId = message.Id,
                    Payload = JsonSerializer.SerializeToElement(message, options),
                };
                var messageJson = JsonSerializer.Serialize(envelope, options);

                await this.connectionManager.BroadcastToUserAsync(entry.CallerId, messageJson, cancellationToken);
                await this.connectionManager.BroadcastToUserAsync(entry.CalleeId, messageJson, cancellationToken);
            }
            catch (Exception ex)
            {
                // The call itself already succeeded (or failed) independently of this summary message —
                // never let a failure here surface as a call error to the user.
                this.logger.Warning(ex, "Failed to post call-summary message for call {CallId}", entry.CallId);
            }
        }

        private CallHistoryDto MapToDto(CallHistoryEntry entry)
        {
            return new CallHistoryDto
            {
                Id = entry.Id ?? string.Empty,
                CallId = entry.CallId,
                ChatId = entry.ChatId,
                CallerId = entry.CallerId,
                CalleeId = entry.CalleeId,
                CallType = entry.CallType,
                Status = entry.Status,
                StartedAt = entry.StartedAt,
                EndedAt = entry.EndedAt,
                DurationSeconds = entry.DurationSeconds,
            };
        }
    }
}
