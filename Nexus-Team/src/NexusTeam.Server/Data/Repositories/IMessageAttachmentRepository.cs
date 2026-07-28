namespace NexusTeam.Server.Data.Repositories
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Models;

    /// <summary>
    /// Repository interface for message attachments.
    /// </summary>
    public interface IMessageAttachmentRepository
    {
        /// <summary>
        /// Adds a new attachment.
        /// </summary>
        /// <param name="attachment">The attachment to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddAsync(MessageAttachment attachment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an attachment by ID.
        /// </summary>
        /// <param name="attachmentId">The attachment ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The attachment or null if not found.</returns>
        Task<MessageAttachment?> GetByIdAsync(string attachmentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all attachments for a message.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of attachments.</returns>
        Task<List<MessageAttachment>> GetByMessageIdAsync(string messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing attachment.
        /// </summary>
        /// <param name="attachment">The attachment to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(MessageAttachment attachment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an attachment.
        /// </summary>
        /// <param name="attachmentId">The attachment ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAsync(string attachmentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all attachments for multiple messages.
        /// </summary>
        /// <param name="messageIds">The message IDs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of attachments.</returns>
        Task<List<MessageAttachment>> GetByMessageIdsAsync(List<string> messageIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all attachments for multiple messages.
        /// </summary>
        /// <param name="messageIds">The message IDs.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The IDs of deleted attachments.</returns>
        Task<List<string>> DeleteByMessageIdsAsync(List<string> messageIds, CancellationToken cancellationToken = default);
    }
}
