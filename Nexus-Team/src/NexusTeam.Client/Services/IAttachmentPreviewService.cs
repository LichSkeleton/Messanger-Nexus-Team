namespace NexusTeam.Client.Services
{
    using System.Threading.Tasks;
    using System.Windows;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Opens in-app previews for chat attachments before downloading.
    /// </summary>
    public interface IAttachmentPreviewService
    {
        /// <summary>
        /// Opens a preview window for a supported attachment.
        /// Shows a warning instead when the file is too large to preview.
        /// </summary>
        /// <param name="attachment">The attachment to preview.</param>
        /// <param name="chatId">Optional chat ID used when editing code files.</param>
        /// <param name="owner">Optional owner window.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PreviewAsync(AttachmentViewModel attachment, string? chatId = null, Window? owner = null);
    }
}
