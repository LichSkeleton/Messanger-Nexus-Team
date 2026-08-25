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
        /// <summary>Records an outcome reported by the client that ended the call (caller or callee).</summary>
        /// <param name="reportingUserId">The authenticated user submitting this report (must be the caller or callee).</param>
        /// <param name="request">The call outcome details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<CallHistoryDto?> RecordAsync(string reportingUserId, RecordCallHistoryRequest request, CancellationToken cancellationToken = default);

        /// <summary>Records a missed call server-side, when the callee had no active connection to ring.</summary>
        Task RecordMissedAsync(string callId, string chatId, string callerId, string calleeId, string callType, CancellationToken cancellationToken = default);

        /// <summary>Gets recent call history for a chat.</summary>
        Task<IEnumerable<CallHistoryDto>> GetByChatIdAsync(string chatId, int limit, CancellationToken cancellationToken = default);

        /// <summary>Gets unseen missed calls for the given user, to surface a "missed call" toast.</summary>
        Task<IEnumerable<CallHistoryDto>> GetUnseenMissedAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>Marks a missed-call entry as seen so it isn't surfaced again.</summary>
        Task MarkSeenAsync(string id, CancellationToken cancellationToken = default);
    }
}
