namespace NexusTeam.Client.Views
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.Win32;
    using NexusTeam.Client.Services;
    using NexusTeam.Shared.Dtos;
    using Serilog;

    /// <summary>
    /// Interaction logic for CodePreviewWindow.xaml.
    /// </summary>
    public partial class CodePreviewWindow : Window
    {
        private string originalCodeContent = string.Empty;
        private string codeContent = string.Empty;
        private string fileName = string.Empty;
        private string? attachmentId;
        private string? messageId;
        private string? chatId;
        private string? tempFilePath;
        private bool isEditMode = false;
        private bool hasUnsavedChanges = false;
        private IFileAttachmentService? fileAttachmentService;
        private IMessagingService? messagingService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodePreviewWindow"/> class.
        /// </summary>
        public CodePreviewWindow()
        {
            this.InitializeComponent();
            this.KeyDown += this.CodePreviewWindow_KeyDown;
            this.Closing += this.CodePreviewWindow_Closing;
        }

        /// <summary>
        /// Loads code content into the preview window.
        /// </summary>
        /// <param name="code">The code content.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="attachmentId">The attachment ID.</param>
        /// <param name="messageId">The message ID.</param>
        /// <param name="chatId">The chat ID.</param>
        /// <param name="fileAttachmentService">The file attachment service.</param>
        /// <param name="messagingService">The messaging service.</param>
        public void LoadCode(
            string code,
            string fileName,
            string? attachmentId = null,
            string? messageId = null,
            string? chatId = null,
            IFileAttachmentService? fileAttachmentService = null,
            IMessagingService? messagingService = null)
        {
            this.originalCodeContent = code;
            this.codeContent = code;
            this.fileName = fileName;
            this.attachmentId = attachmentId;
            this.messageId = messageId;
            this.chatId = chatId;
            this.fileAttachmentService = fileAttachmentService;
            this.messagingService = messagingService;
            this.Title = $"Code Preview - {fileName}";
            this.CodePreview.LoadCode(code, fileName);
            this.UpdateButtonVisibility();

            // Subscribe to text changes for tracking unsaved changes
            this.CodePreview.CodeChanged += (s, e) =>
            {
                this.CheckForChanges();
                if (this.isEditMode)
                {
                    this.UpdateUndoRedoButtons();
                }
            };
        }

        private void CodePreviewWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                if (this.isEditMode && this.hasUnsavedChanges)
                {
                    var result = MessageBox.Show(
                        "You have unsaved changes. Are you sure you want to close?\nYour changes will not be saved.",
                        "Unsaved Changes",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (result == MessageBoxResult.No)
                    {
                        e.Handled = true;
                        return;
                    }
                }

                this.Close();
            }
            else if (e.Key == System.Windows.Input.Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (this.isEditMode)
                {
                    e.Handled = true;
                    this.SaveButton_Click(this, new RoutedEventArgs());
                }
            }
        }

        private void CodePreviewWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.isEditMode && this.hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Are you sure you want to close?\nYour changes will not be saved.",
                    "Unsaved Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            // Cleanup temp file
            if (!string.IsNullOrEmpty(this.tempFilePath) && File.Exists(this.tempFilePath))
            {
                try
                {
                    File.Delete(this.tempFilePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(this.codeContent);
                MessageBox.Show("Code copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Information("Code copied to clipboard from {FileName}", this.fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy code: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Failed to copy code to clipboard");
            }
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    FileName = this.fileName,
                    Filter = "All Files (*.*)|*.*",
                    Title = "Save Code File",
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveFileDialog.FileName, this.codeContent);
                    MessageBox.Show("File saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    Log.Information("Code file saved to {FilePath}", saveFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Failed to save code file");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.attachmentId))
            {
                MessageBox.Show("Cannot edit: Attachment ID is missing.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.isEditMode = true;
            this.hasUnsavedChanges = false;
            this.CodePreview.SetEditMode(true);
            this.UpdateButtonVisibility();
            this.UpdateUndoRedoButtons();
            this.Title = $"Code Preview - {this.fileName} (Editing)";
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.attachmentId) || this.fileAttachmentService == null)
            {
                MessageBox.Show("Cannot save: Attachment service is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Get current content from editor
                var currentContent = this.CodePreview.GetCode();
                if (currentContent == this.originalCodeContent)
                {
                    MessageBox.Show("No changes to save.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Create temp file with updated content
                this.tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + Path.GetExtension(this.fileName));
                await File.WriteAllTextAsync(this.tempFilePath, currentContent);

                // Update attachment on server
                var updatedAttachment = await this.fileAttachmentService.UpdateAttachmentAsync(
                    this.attachmentId,
                    this.tempFilePath);

                // Update local state
                this.originalCodeContent = currentContent;
                this.codeContent = currentContent;
                this.hasUnsavedChanges = false;

                // Send "File Edited" message to chat
                if (!string.IsNullOrEmpty(this.chatId) && this.messagingService != null)
                {
                    try
                    {
                        await this.messagingService.SendMessageViaHttpAsync(
                            this.chatId,
                            $"File Edited: {this.fileName}",
                            replyToId: this.messageId);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to send 'File Edited' message");
                    }
                }

                // Exit edit mode
                this.isEditMode = false;
                this.CodePreview.SetEditMode(false);
                this.UpdateButtonVisibility();
                this.Title = $"Code Preview - {this.fileName}";

                MessageBox.Show("File saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Log.Information("Code file updated: {FileName}, AttachmentId: {AttachmentId}", this.fileName, this.attachmentId);

                // Cleanup temp file
                if (!string.IsNullOrEmpty(this.tempFilePath) && File.Exists(this.tempFilePath))
                {
                    try
                    {
                        File.Delete(this.tempFilePath);
                        this.tempFilePath = null;
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Log.Error(ex, "Failed to save code file");
            }
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            this.CodePreview.Undo();
            this.UpdateUndoRedoButtons();
            this.CheckForChanges();
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            this.CodePreview.Redo();
            this.UpdateUndoRedoButtons();
            this.CheckForChanges();
        }

        private void UpdateButtonVisibility()
        {
            if (this.isEditMode)
            {
                this.EditButton.Visibility = Visibility.Collapsed;
                this.SaveButton.Visibility = Visibility.Visible;
                this.UndoButton.Visibility = Visibility.Visible;
                this.RedoButton.Visibility = Visibility.Visible;
            }
            else
            {
                this.EditButton.Visibility = string.IsNullOrEmpty(this.attachmentId) ? Visibility.Collapsed : Visibility.Visible;
                this.SaveButton.Visibility = Visibility.Collapsed;
                this.UndoButton.Visibility = Visibility.Collapsed;
                this.RedoButton.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateUndoRedoButtons()
        {
            if (this.isEditMode)
            {
                this.UndoButton.IsEnabled = this.CodePreview.CanUndo();
                this.RedoButton.IsEnabled = this.CodePreview.CanRedo();
            }
        }

        private void CheckForChanges()
        {
            if (this.isEditMode)
            {
                var currentContent = this.CodePreview.GetCode();
                this.hasUnsavedChanges = currentContent != this.originalCodeContent;
            }
        }
    }
}
