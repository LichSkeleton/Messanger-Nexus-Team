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
        /// <summary>Inserts a new call history entry.</summary>
        Task CreateAsync(CallHistoryEntry entry, CancellationToken cancellationToken = default);

        /// <summary>Gets recent call history for a chat.</summary>
        Task<IEnumerable<CallHistoryEntry>> GetByChatIdAsync(string chatId, int limit, CancellationToken cancellationToken = default);

        /// <summary>Gets unseen missed/no-answer calls for a user (as callee).</summary>
        Task<IEnumerable<CallHistoryEntry>> GetUnseenMissedAsync(string calleeId, CancellationToken cancellationToken = default);

        /// <summary>Marks a call history entry as seen by the callee.</summary>
        Task MarkSeenAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>Finds an entry by its call correlation ID.</summary>
        Task<CallHistoryEntry?> GetByCallIdAsync(string callId, CancellationToken cancellationToken = default);

        /// <summary>Updates an existing entry (e.g. to attach an outcome/duration after the fact).</summary>
        Task UpdateAsync(CallHistoryEntry entry, CancellationToken cancellationToken = default);
    }
}
