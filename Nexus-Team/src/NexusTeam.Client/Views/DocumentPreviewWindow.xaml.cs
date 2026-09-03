namespace NexusTeam.Client.Views
{
    using System;
    using System.Windows;
    using NexusTeam.Client.Services;
    using NexusTeam.Client.ViewModels;
    using Serilog;

    /// <summary>
    /// Interaction logic for DocumentPreviewWindow.xaml.
    /// </summary>
    public partial class DocumentPreviewWindow : Window
    {
        private AttachmentViewModel? attachment;
        private IFileAttachmentService? fileAttachmentService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentPreviewWindow"/> class.
        /// </summary>
        public DocumentPreviewWindow()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Loads extracted document content into the preview window.
        /// </summary>
        /// <param name="preview">The extracted preview content.</param>
        /// <param name="attachment">The attachment being previewed.</param>
        /// <param name="fileAttachmentService">The file attachment service used for download.</param>
        public void LoadPreview(
            DocumentPreviewResult preview,
            AttachmentViewModel attachment,
            IFileAttachmentService fileAttachmentService)
        {
            this.attachment = attachment;
            this.fileAttachmentService = fileAttachmentService;
            this.Title = $"Preview - {preview.FileName}";
            this.FileNameTextBlock.Text = preview.FileName;
            this.FileTypeTextBlock.Text = preview.FileTypeLabel;
            this.FileSizeTextBlock.Text = attachment.FileSizeFormatted;
            this.PreviewTextBox.Text = preview.TextContent;

            if (!string.IsNullOrWhiteSpace(preview.Notice))
            {
                this.NoticeTextBlock.Text = preview.Notice;
                this.NoticeBanner.Visibility = Visibility.Visible;
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.attachment?.AttachmentDto == null || this.fileAttachmentService == null)
            {
                return;
            }

            try
            {
                var localPath = await this.fileAttachmentService.DownloadAttachmentAsync(this.attachment.AttachmentDto);
                MessageBox.Show(
                    $"File downloaded to: {localPath}",
                    "Download",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to download attachment from preview: {FileName}", this.attachment.FileName);
                MessageBox.Show(
                    $"Failed to download file: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
