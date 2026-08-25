namespace NexusTeam.Server.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Models;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Service for recording and querying call history.
    /// </summary>
    public class CallHistoryService : ICallHistoryService
    {
        private readonly ICallHistoryRepository repository;
        private readonly IIdGenerator idGenerator;
        private readonly IClock clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallHistoryService"/> class.
        /// </summary>
        public CallHistoryService(ICallHistoryRepository repository, IIdGenerator idGenerator, IClock clock)
        {
            this.repository = repository;
            this.idGenerator = idGenerator;
            this.clock = clock;
        }

        /// <inheritdoc/>
        public async Task<CallHistoryDto?> RecordAsync(string reportingUserId, RecordCallHistoryRequest request, CancellationToken cancellationToken = default)
        {
            // Only the two actual participants may report an outcome for this call.
            if (reportingUserId != request.CallerId && reportingUserId != request.CalleeId)
            {
                return null;
            }

            // Avoid duplicate entries if both peers report the same call.
            var existing = await this.repository.GetByCallIdAsync(request.CallId, cancellationToken);
            if (existing != null)
            {
                existing.Status = request.Status;
                existing.DurationSeconds = request.DurationSeconds > 0 ? request.DurationSeconds : existing.DurationSeconds;
                existing.EndedAt = this.clock.UtcNow;
                await this.repository.UpdateAsync(existing, cancellationToken);
                return this.MapToDto(existing);
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

            await this.repository.CreateAsync(entry, cancellationToken);
            return this.MapToDto(entry);
        }

        /// <inheritdoc/>
        public async Task RecordMissedAsync(string callId, string chatId, string callerId, string calleeId, string callType, CancellationToken cancellationToken = default)
        {
            var existing = await this.repository.GetByCallIdAsync(callId, cancellationToken);
            if (existing != null)
            {
                return;
            }

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

            await this.repository.CreateAsync(entry, cancellationToken);
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
        public async Task MarkSeenAsync(string id, CancellationToken cancellationToken = default)
        {
            await this.repository.MarkSeenAsync(id, cancellationToken);
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
