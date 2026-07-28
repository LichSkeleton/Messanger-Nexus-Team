namespace NexusTeam.Server.Services
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Service for managing file attachments.
    /// </summary>
    public interface IAttachmentService
    {
        /// <summary>
        /// Saves an attachment file.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="contentType">The content type.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The saved attachment DTO.</returns>
        Task<MessageAttachmentDto> SaveAttachmentAsync(
            string messageId,
            string fileName,
            Stream fileStream,
            string contentType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an attachment by ID.
        /// </summary>
        /// <param name="attachmentId">The attachment ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The attachment DTO.</returns>
        Task<MessageAttachmentDto?> GetAttachmentAsync(string attachmentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the file stream for an attachment.
        /// </summary>
        /// <param name="attachmentId">The attachment ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The file stream.</returns>
        Task<Stream?> GetAttachmentStreamAsync(string attachmentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the thumbnail stream for an attachment (for images).
        /// </summary>
        /// <param name="attachmentId">The attachment ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The thumbnail stream, or null if thumbnail doesn't exist.</returns>
        Task<Stream?> GetThumbnailStreamAsync(string attachmentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all attachments for a message.
        /// </summary>
        /// <param name="messageId">The message ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of attachments.</returns>
        Task<List<MessageAttachmentDto>> GetMessageAttachmentsAsync(string messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing attachment file.
        /// </summary>
        /// <param name="attachmentId">The attachment ID.</param>
        /// <param name="fileStream">The new file stream.</param>
        /// <param name="contentType">The content type.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated attachment DTO.</returns>
        Task<MessageAttachmentDto> UpdateAttachmentAsync(
            string attachmentId,
            Stream fileStream,
            string contentType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an attachment.
        /// </summary>
        /// <param name="attachmentId">The attachment ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if deleted successfully.</returns>
        Task<bool> DeleteAttachmentAsync(string attachmentId, CancellationToken cancellationToken = default);
    }
}
