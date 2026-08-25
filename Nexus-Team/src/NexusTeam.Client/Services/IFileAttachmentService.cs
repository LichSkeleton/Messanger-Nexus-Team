namespace NexusTeam.Client.Services
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Service for handling file attachments in messages.
    /// </summary>
    public interface IFileAttachmentService
    {
        /// <summary>
        /// Opens a file picker dialog and returns selected file paths.
        /// </summary>
        /// <param name="multiSelect">Whether to allow multiple file selection.</param>
        /// <returns>List of selected file paths.</returns>
        Task<List<string>> PickFilesAsync(bool multiSelect = true);

        /// <summary>
        /// Uploads a file attachment for a message.
        /// </summary>
        /// <param name="filePath">The local file path.</param>
        /// <param name="messageId">The message ID to attach to.</param>
        /// <param name="content">Optional stream content to upload instead of reading from file path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The uploaded attachment DTO.</returns>
        Task<MessageAttachmentDto> UploadAttachmentAsync(string filePath, string messageId, Stream? content = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads an attachment to local storage.
        /// </summary>
        /// <param name="attachment">The attachment to download.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The local file path where attachment was saved.</returns>
        Task<string> DownloadAttachmentAsync(MessageAttachmentDto attachment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prompts user to choose a location and saves the attachment there.
        /// </summary>
        /// <param name="attachment">The attachment to save.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The local file path where attachment was saved, or null if cancelled.</returns>
        Task<string?> SaveAttachmentAsAsync(MessageAttachmentDto attachment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Opens an attachment with the default system application.
        /// </summary>
        /// <param name="attachment">The attachment to open.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task OpenAttachmentAsync(MessageAttachmentDto attachment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads an image attachment as a stream for preview.
        /// </summary>
        /// <param name="attachment">The attachment to download.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The image stream.</returns>
        Task<Stream> DownloadImageStreamAsync(MessageAttachmentDto attachment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a thumbnail stream for preview (for images).
        /// </summary>
        /// <param name="attachment">The attachment DTO.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The thumbnail stream.</returns>
        Task<Stream> DownloadThumbnailStreamAsync(MessageAttachmentDto attachment, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing attachment file.
        /// </summary>
        /// <param name="attachmentId">The attachment ID.</param>
        /// <param name="filePath">The local file path with updated content.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated attachment DTO.</returns>
        Task<MessageAttachmentDto> UpdateAttachmentAsync(
            string attachmentId,
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates if a file can be attached.
        /// </summary>
        /// <param name="filePath">The file path to validate.</param>
        /// <returns>A tuple containing validation result and error message if invalid.</returns>
        (bool IsValid, string ErrorMessage) ValidateFile(string filePath);
    }
}
