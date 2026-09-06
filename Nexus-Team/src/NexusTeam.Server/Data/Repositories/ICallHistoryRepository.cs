namespace NexusTeam.Server.Data.Repositories
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Models;

    /// <summary>
    /// Repository interface for call history entries.
    /// </summary>
    public interface ICallHistoryRepository
    {
        /// <summary>
        /// Atomically inserts a new call history entry if one with the same <c>CallId</c> doesn't already exist.
        /// </summary>
        /// <param name="entry">The entry to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><see langword="true"/> if this call inserted the entry; <see langword="false"/> if an entry for that call already existed.</returns>
        Task<bool> TryCreateAsync(CallHistoryEntry entry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets recent call history for a chat.
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <param name="limit">The maximum number of entries to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The chat's recent call history entries, most recent first.</returns>
        Task<IEnumerable<CallHistoryEntry>> GetByChatIdAsync(string chatId, int limit, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets unseen missed/no-answer calls for a user (as callee).
        /// </summary>
        /// <param name="calleeId">The callee's user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The callee's unseen missed/no-answer call entries.</returns>
        Task<IEnumerable<CallHistoryEntry>> GetUnseenMissedAsync(string calleeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a call history entry as seen by the callee.
        /// </summary>
        /// <param name="id">The history entry identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes once the entry has been updated.</returns>
        Task MarkSeenAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds an entry by its primary key.
        /// </summary>
        /// <param name="id">The history entry identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The matching entry, or <see langword="null"/> if none was found.</returns>
        Task<CallHistoryEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds an entry by its call correlation ID.
        /// </summary>
        /// <param name="callId">The call correlation ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The matching entry, or <see langword="null"/> if none was found.</returns>
        Task<CallHistoryEntry?> GetByCallIdAsync(string callId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing entry (e.g. to attach an outcome/duration after the fact).
        /// </summary>
        /// <param name="entry">The entry with updated values.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that completes once the entry has been updated.</returns>
        Task UpdateAsync(CallHistoryEntry entry, CancellationToken cancellationToken = default);
    }
}
