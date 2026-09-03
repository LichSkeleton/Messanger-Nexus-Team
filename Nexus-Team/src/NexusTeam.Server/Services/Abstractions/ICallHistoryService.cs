namespace NexusTeam.Server.Services.Abstractions
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Service for recording and querying call history.
    /// </summary>
    public interface ICallHistoryService
    {
        /// <summary>
        /// Records an outcome reported by the client that ended the call (caller or callee).
        /// </summary>
        /// <param name="reportingUserId">The authenticated user submitting this report (must be the caller or callee).</param>
        /// <param name="request">The call outcome details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The recorded entry, or <see langword="null"/> if the reporting user is not a participant of the call.</returns>
        Task<CallHistoryDto?> RecordAsync(string reportingUserId, RecordCallHistoryRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records a missed call server-side, when the callee had no active connection to ring.
        /// </summary>
        /// <param name="callId">The call correlation ID.</param>
        /// <param name="chatId">The chat the call belongs to.</param>
        /// <param name="callerId">The user who initiated the call.</param>
        /// <param name="calleeId">The user who was called.</param>
        /// <param name="callType">The call type ("Audio" or "Video").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes once the missed-call entry has been recorded.</returns>
        Task RecordMissedAsync(string callId, string chatId, string callerId, string calleeId, string callType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets recent call history for a chat.
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <param name="limit">The maximum number of entries to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The chat's recent call history.</returns>
        Task<IEnumerable<CallHistoryDto>> GetByChatIdAsync(string chatId, int limit, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets unseen missed calls for the given user, to surface a "missed call" toast.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user's unseen missed calls.</returns>
        Task<IEnumerable<CallHistoryDto>> GetUnseenMissedAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a missed-call entry as seen so it isn't surfaced again.
        /// </summary>
        /// <param name="id">The history entry identifier.</param>
        /// <param name="userId">The authenticated user requesting this (must be the callee on the entry).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see langword="true"/> if the entry was found and marked seen; otherwise <see langword="false"/>.</returns>
        Task<bool> MarkSeenAsync(string id, string userId, CancellationToken cancellationToken = default);
    }
}
