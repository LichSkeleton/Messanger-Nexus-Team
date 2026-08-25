namespace NexusTeam.Client.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Win32;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Helpers;
    using Serilog;

    /// <summary>
    /// Implementation of file attachment service for WPF client.
    /// </summary>
    public class FileAttachmentService : IFileAttachmentService
    {
        private readonly HttpClient httpClient;
        private readonly ILogger logger;
        private readonly IErrorHandlingService errorHandler;
        private readonly string downloadPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileAttachmentService"/> class.
        /// </summary>
        /// <param name="httpClient">HTTP client for file transfers.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="errorHandler">Error handling service.</param>
        public FileAttachmentService(
            HttpClient httpClient,
            ILogger logger,
            IErrorHandlingService errorHandler)
        {
            this.httpClient = httpClient;
            this.logger = logger;
            this.errorHandler = errorHandler;

            // Create downloads directory in user's Documents folder
            this.downloadPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "NexusTeam",
                "Downloads");

            Directory.CreateDirectory(this.downloadPath);
        }

        /// <inheritdoc/>
        public Task<List<string>> PickFilesAsync(bool multiSelect = true)
        {
            return Task.Run(() =>
            {
                var dialog = new OpenFileDialog
                {
                    Multiselect = multiSelect,
                    Title = "Select files to attach",
                    Filter = "All Files (*.*)|*.*|" +
                             "Images (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|" +
                             "Documents (*.pdf;*.doc;*.docx;*.txt)|*.pdf;*.doc;*.docx;*.txt|" +
                             "Archives (*.zip;*.rar;*.7z)|*.zip;*.rar;*.7z|" +
                             "Code Files (*.cs;*.js;*.py;*.java)|*.cs;*.js;*.py;*.java",
                };

                var result = dialog.ShowDialog();
                if (result == true)
                {
                    return dialog.FileNames.ToList();
                }

                return new List<string>();
            });
        }

        /// <inheritdoc/>
        public async Task<MessageAttachmentDto> UploadAttachmentAsync(
            string filePath,
            string messageId,
            Stream? contentStream = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate file
                var validation = this.ValidateFile(filePath);
                if (!validation.IsValid)
                {
                    throw new InvalidOperationException(validation.ErrorMessage);
                }

                var fileInfo = new FileInfo(filePath);
                var fileName = fileInfo.Name;

                this.logger.Debug(
                    "Uploading file: {FileName}, Original size: {Size}, Using compressed stream: {HasCompressedStream}",
                    fileName,
                    FileHelper.FormatFileSize(fileInfo.Length),
                    contentStream != null);

                // Create multipart form data
                using var content = new MultipartFormDataContent();

                // Use provided compressed stream or open original file
                Stream streamToUpload;
                FileStream? fileStream = null;
                long? compressedStreamLength = null;

                if (contentStream != null)
                {
                    // Use compressed stream - caller will dispose it
                    // Reset position in case it was read before
                    if (contentStream.CanSeek)
                    {
                        contentStream.Position = 0;
                        compressedStreamLength = contentStream.Length;
                    }

                    streamToUpload = contentStream;
                    this.logger.Debug(
                        "Using compressed stream for upload: {FileName}, Stream length: {Length}",
                        fileName,
                        compressedStreamLength?.ToString() ?? "unknown");
                }
                else
                {
                    // Use original file - we'll dispose it after upload
                    fileStream = File.OpenRead(filePath);
                    streamToUpload = fileStream;
                    this.logger.Debug("Using original file for upload: {FileName}", fileName);
                }

                // StreamContent will NOT dispose the underlying stream by default
                // We need to manage disposal ourselves
                using var streamContent = new StreamContent(streamToUpload);

                streamContent.Headers.ContentType = new MediaTypeHeaderValue(FileHelper.GetContentType(fileName));
                content.Add(streamContent, "file", fileName);
                content.Add(new StringContent(messageId), "messageId");

                // Upload to server
                var response = await this.httpClient.PostAsync(
                    "/api/attachments/upload",
                    content,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                // Dispose file stream if we opened it (compressed stream is disposed by caller)
                if (fileStream != null)
                {
                    fileStream.Dispose();
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var attachment = JsonSerializer.Deserialize<MessageAttachmentDto>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidOperationException("Failed to deserialize attachment response");

                var uploadedSize = compressedStreamLength ?? fileInfo.Length;

                this.logger.Information(
                    "File uploaded successfully: {FileName}, Original size: {OriginalSize}, Uploaded size: {UploadedSize}",
                    fileName,
                    FileHelper.FormatFileSize(fileInfo.Length),
                    FileHelper.FormatFileSize(uploadedSize));

                return attachment;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to upload attachment: {FilePath}", filePath);
                this.errorHandler.ShowError($"Failed to upload file: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<MessageAttachmentDto> UpdateAttachmentAsync(
            string attachmentId,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                var fileInfo = new FileInfo(filePath);
                var fileName = Path.GetFileName(filePath);

                this.logger.Information(
                    "Updating attachment: {AttachmentId}, File: {FileName}, Size: {Size}",
                    attachmentId,
                    fileName,
                    FileHelper.FormatFileSize(fileInfo.Length));

                // Create multipart form data
                using var content = new MultipartFormDataContent();
                using var fileStream = File.OpenRead(filePath);
                using var streamContent = new StreamContent(fileStream);

                streamContent.Headers.ContentType = new MediaTypeHeaderValue(FileHelper.GetContentType(fileName));
                content.Add(streamContent, "file", fileName);

                // Update on server
                var response = await this.httpClient.PutAsync(
                    $"/api/attachments/{attachmentId}",
                    content,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var attachment = JsonSerializer.Deserialize<MessageAttachmentDto>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new InvalidOperationException("Failed to deserialize attachment response");

                this.logger.Information(
                    "File updated successfully: {FileName}, Size: {Size}",
                    fileName,
                    FileHelper.FormatFileSize(fileInfo.Length));

                return attachment;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to update attachment: {AttachmentId}, FilePath: {FilePath}", attachmentId, filePath);
                this.errorHandler.ShowError($"Failed to update file: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> DownloadAttachmentAsync(
            MessageAttachmentDto attachment,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Download file from server
                var response = await this.httpClient.GetAsync(
                    attachment.DownloadUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                // Save to downloads folder
                var localPath = Path.Combine(this.downloadPath, attachment.FileName);

                // If file exists, add number suffix
                var counter = 1;
                while (File.Exists(localPath))
                {
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(attachment.FileName);
                    var extension = Path.GetExtension(attachment.FileName);
                    localPath = Path.Combine(this.downloadPath, $"{nameWithoutExt} ({counter}){extension}");
                    counter++;
                }

                using var fileStream = File.Create(localPath);
                using var contentStream = await response.Content.ReadAsStreamAsync();
                await contentStream.CopyToAsync(fileStream, cancellationToken);

                this.logger.Information("File downloaded: {FileName} to {Path}", attachment.FileName, localPath);

                return localPath;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to download attachment: {AttachmentId}", attachment.Id);
                this.errorHandler.ShowError($"Failed to download file: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string?> SaveAttachmentAsAsync(
            MessageAttachmentDto attachment,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Show SaveFileDialog for  user to choose location
                var saveDialog = new SaveFileDialog
                {
                    FileName = attachment.FileName,
                    Title = "Save Image As",
                    Filter = $"Image files (*{Path.GetExtension(attachment.FileName)})|*{Path.GetExtension(attachment.FileName)}|All files (*.*)|*.*",
                    DefaultExt = Path.GetExtension(attachment.FileName),
                };

                var result = saveDialog.ShowDialog();
                if (result != true)
                {
                    // User cancelled
                    return null;
                }

                var localPath = saveDialog.FileName;

                // Download file from server
                var response = await this.httpClient.GetAsync(
                    attachment.DownloadUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                // Save to chosen location
                using var fileStream = File.Create(localPath);
                using var contentStream = await response.Content.ReadAsStreamAsync();
                await contentStream.CopyToAsync(fileStream, cancellationToken);

                this.logger.Information("File saved: {FileName} to {Path}", attachment.FileName, localPath);
                this.errorHandler.ShowInfo($"Image saved to: {localPath}");

                return localPath;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to save attachment: {AttachmentId}", attachment.Id);
                this.errorHandler.ShowError($"Failed to save file: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task OpenAttachmentAsync(
            MessageAttachmentDto attachment,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // First download the file
                var localPath = await this.DownloadAttachmentAsync(attachment, cancellationToken);

                // Open with default application
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo(localPath)
                    {
                        UseShellExecute = true,
                    },
                };

                process.Start();

                this.logger.Information("Opened attachment: {FileName}", attachment.FileName);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to open attachment: {AttachmentId}", attachment.Id);
                this.errorHandler.ShowError($"Failed to open file: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<Stream> DownloadImageStreamAsync(
            MessageAttachmentDto attachment,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Download file from server
                var response = await this.httpClient.GetAsync(
                    attachment.DownloadUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                // Return stream for preview
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return stream;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to download image stream: {AttachmentId}", attachment.Id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Stream> DownloadThumbnailStreamAsync(
            MessageAttachmentDto attachment,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(attachment.ThumbnailUrl))
                {
                    throw new InvalidOperationException("Thumbnail URL is not available for this attachment.");
                }

                // Download thumbnail from server
                var response = await this.httpClient.GetAsync(
                    attachment.ThumbnailUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                // Return stream for preview
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return stream;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to download thumbnail stream: {AttachmentId}", attachment.Id);
                throw;
            }
        }

        /// <inheritdoc/>
        public (bool IsValid, string ErrorMessage) ValidateFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return (false, "File does not exist.");
                }

                var fileInfo = new FileInfo(filePath);
                var fileName = fileInfo.Name;
                var fileSize = fileInfo.Length;

                // Check if file type is allowed
                if (!FileHelper.IsAllowedFileType(fileName))
                {
                    return (false, $"File type is not allowed for security reasons: {Path.GetExtension(fileName)}");
                }

                // Check file size
                var attachmentType = FileHelper.GetAttachmentType(fileName);
                if (!FileHelper.IsValidFileSize(fileSize, attachmentType))
                {
                    var maxSize = attachmentType == Shared.Enums.AttachmentType.Image
                        ? FileHelper.MaxImageSizeBytes
                        : FileHelper.MaxFileSizeBytes;

                    return (false, $"File is too large. Maximum size: {FileHelper.FormatFileSize(maxSize)}");
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error validating file: {FilePath}", filePath);
                return (false, $"Error validating file: {ex.Message}");
            }
        }
    }
}
