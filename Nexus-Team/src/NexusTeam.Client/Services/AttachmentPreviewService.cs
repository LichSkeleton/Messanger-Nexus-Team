namespace NexusTeam.Client.Services
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows;
    using NexusTeam.Client.Helpers;
    using NexusTeam.Client.ViewModels;
    using NexusTeam.Client.Views;
    using NexusTeam.Shared.Helpers;
    using Serilog;

    /// <summary>
    /// Opens code and document previews for chat attachments.
    /// </summary>
    public class AttachmentPreviewService : IAttachmentPreviewService
    {
        private readonly IFileAttachmentService fileAttachmentService;
        private readonly IDocumentPreviewService documentPreviewService;
        private readonly IMessagingService messagingService;
        private readonly IErrorHandlingService errorHandlingService;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentPreviewService"/> class.
        /// </summary>
        /// <param name="fileAttachmentService">The file attachment service.</param>
        /// <param name="documentPreviewService">The document preview service.</param>
        /// <param name="messagingService">The messaging service.</param>
        /// <param name="errorHandlingService">The error handling service.</param>
        /// <param name="logger">The logger.</param>
        public AttachmentPreviewService(
            IFileAttachmentService fileAttachmentService,
            IDocumentPreviewService documentPreviewService,
            IMessagingService messagingService,
            IErrorHandlingService errorHandlingService,
            ILogger logger)
        {
            this.fileAttachmentService = fileAttachmentService;
            this.documentPreviewService = documentPreviewService;
            this.messagingService = messagingService;
            this.errorHandlingService = errorHandlingService;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task PreviewAsync(AttachmentViewModel attachment, string? chatId = null, Window? owner = null)
        {
            if (attachment.AttachmentDto == null)
            {
                this.errorHandlingService.ShowError("Attachment information is not available.");
                return;
            }

            if (!FileHelper.IsPreviewableFile(attachment.FileName))
            {
                this.errorHandlingService.ShowWarning("Preview is not available for this file type. Please download the file instead.");
                return;
            }

            if (FileHelper.IsTooLargeForPreview(attachment.FileSize))
            {
                this.errorHandlingService.ShowWarning(
                    $"This file is too large to preview ({attachment.FileSizeFormatted}). " +
                    $"Preview is limited to {FileHelper.FormatFileSize(FileHelper.MaxPreviewFileSizeBytes)}. " +
                    "Please download the file instead.");
                return;
            }

            string? tempPath = null;
            try
            {
                tempPath = await this.fileAttachmentService.DownloadAttachmentToTempAsync(attachment.AttachmentDto);
                if (string.IsNullOrEmpty(tempPath) || !File.Exists(tempPath))
                {
                    this.errorHandlingService.ShowError("Failed to load the file for preview.");
                    return;
                }

                var ownerWindow = owner ?? Application.Current?.MainWindow;
                if (attachment.IsCodeFile || CodeLanguageDetector.IsCodeFile(attachment.FileName))
                {
                    await this.ShowCodePreviewAsync(attachment, tempPath, chatId, ownerWindow);
                }
                else
                {
                    this.ShowDocumentPreview(attachment, tempPath, ownerWindow);
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to preview attachment: {FileName}", attachment.FileName);
                this.errorHandlingService.ShowError($"Failed to preview file: {ex.Message}");
            }
            finally
            {
                this.TryDeleteTempFile(tempPath);
            }
        }

        private async Task ShowCodePreviewAsync(
            AttachmentViewModel attachment,
            string tempPath,
            string? chatId,
            Window? owner)
        {
            var codeContent = await File.ReadAllTextAsync(tempPath);
            var previewWindow = new CodePreviewWindow
            {
                Owner = owner,
            };

            previewWindow.LoadCode(
                codeContent,
                attachment.FileName,
                attachment.AttachmentDto?.Id,
                attachment.AttachmentDto?.MessageId,
                chatId,
                this.fileAttachmentService,
                this.messagingService);

            previewWindow.ShowDialog();
        }

        private void ShowDocumentPreview(AttachmentViewModel attachment, string tempPath, Window? owner)
        {
            var preview = this.documentPreviewService.LoadPreview(tempPath, attachment.FileName);
            var previewWindow = new DocumentPreviewWindow
            {
                Owner = owner,
            };

            previewWindow.LoadPreview(preview, attachment, this.fileAttachmentService);
            previewWindow.ShowDialog();
        }

        private void TryDeleteTempFile(string? path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to delete temp preview file: {Path}", path);
            }
        }
    }
}
