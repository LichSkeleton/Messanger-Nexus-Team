namespace NexusTeam.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using NexusTeam.Shared.Helpers;
    using NexusTeam.Shared.Models;
    using Serilog;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.Processing;

    /// <summary>
    /// Implementation of attachment service for file storage.
    /// </summary>
    public class AttachmentService : IAttachmentService
    {
        private readonly IMessageAttachmentRepository attachmentRepository;
        private readonly ILogger logger;
        private readonly string storagePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentService"/> class.
        /// </summary>
        /// <param name="attachmentRepository">The attachment repository.</param>
        /// <param name="logger">The logger.</param>
        public AttachmentService(
            IMessageAttachmentRepository attachmentRepository,
            ILogger logger)
        {
            this.attachmentRepository = attachmentRepository;
            this.logger = logger;

            // Set storage path to a dedicated folder
            this.storagePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Storage",
                "Attachments");

            Directory.CreateDirectory(this.storagePath);
        }

        /// <inheritdoc/>
        public async Task<MessageAttachmentDto> SaveAttachmentAsync(
            string messageId,
            string fileName,
            Stream fileStream,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var attachmentId = Guid.NewGuid().ToString();
            var attachmentType = FileHelper.GetAttachmentType(fileName);

            // Generate safe file path
            var extension = Path.GetExtension(fileName);
            var safeFileName = $"{attachmentId}{extension}";
            var filePath = Path.Combine(this.storagePath, safeFileName);

            // Save file to disk
            using (var fileStreamOut = File.Create(filePath))
            {
                await fileStream.CopyToAsync(fileStreamOut, cancellationToken);
            }

            var fileInfo = new FileInfo(filePath);

            // Generate thumbnail for images
            string? thumbnailPath = null;
            if (attachmentType == AttachmentType.Image)
            {
                this.logger.Information("Attempting to generate thumbnail for image attachment: {AttachmentId}, FilePath: {FilePath}", attachmentId, filePath);
                try
                {
                    thumbnailPath = await this.GenerateThumbnailAsync(filePath, attachmentId, cancellationToken);
                    if (thumbnailPath != null)
                    {
                        this.logger.Information("Thumbnail generated successfully for image: {AttachmentId}, ThumbnailPath: {ThumbnailPath}", attachmentId, thumbnailPath);
                    }
                    else
                    {
                        this.logger.Warning("Thumbnail generation returned null for image: {AttachmentId}", attachmentId);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "Failed to generate thumbnail for image: {AttachmentId}", attachmentId);

                    // Continue without thumbnail
                }
            }
            else
            {
                this.logger.Debug("Skipping thumbnail generation for non-image attachment: {AttachmentId}, Type: {AttachmentType}", attachmentId, attachmentType);
            }

            // Create attachment model
            var attachment = new MessageAttachment
            {
                Id = attachmentId,
                MessageId = messageId,
                FileName = fileName,
                FilePath = safeFileName, // Store relative path
                ThumbnailPath = thumbnailPath, // Store relative thumbnail path if exists
                FileSize = fileInfo.Length,
                ContentType = contentType,
                AttachmentType = attachmentType,
                UploadedAt = DateTime.UtcNow,
            };

            // Save to database
            await this.attachmentRepository.AddAsync(attachment, cancellationToken);

            this.logger.Information(
                "Attachment saved: {AttachmentId} for message {MessageId}",
                attachmentId,
                messageId);

            return this.MapToDto(attachment);
        }

        /// <inheritdoc/>
        public async Task<MessageAttachmentDto?> GetAttachmentAsync(
            string attachmentId,
            CancellationToken cancellationToken = default)
        {
            var attachment = await this.attachmentRepository.GetByIdAsync(attachmentId, cancellationToken);
            return attachment != null ? this.MapToDto(attachment) : null;
        }

        /// <inheritdoc/>
        public async Task<Stream?> GetAttachmentStreamAsync(
            string attachmentId,
            CancellationToken cancellationToken = default)
        {
            var attachment = await this.attachmentRepository.GetByIdAsync(attachmentId, cancellationToken);
            if (attachment == null)
            {
                return null;
            }

            var filePath = Path.Combine(this.storagePath, attachment.FilePath);
            if (!File.Exists(filePath))
            {
                this.logger.Warning("Attachment file not found: {FilePath}", filePath);
                return null;
            }

            return File.OpenRead(filePath);
        }

        /// <inheritdoc/>
        public async Task<List<MessageAttachmentDto>> GetMessageAttachmentsAsync(
            string messageId,
            CancellationToken cancellationToken = default)
        {
            var attachments = await this.attachmentRepository.GetByMessageIdAsync(messageId, cancellationToken);
            return attachments.Select(this.MapToDto).ToList();
        }

        /// <inheritdoc/>
        public async Task<Stream?> GetThumbnailStreamAsync(
            string attachmentId,
            CancellationToken cancellationToken = default)
        {
            var attachment = await this.attachmentRepository.GetByIdAsync(attachmentId, cancellationToken);
            if (attachment == null || string.IsNullOrEmpty(attachment.ThumbnailPath))
            {
                return null;
            }

            var thumbnailPath = Path.Combine(this.storagePath, attachment.ThumbnailPath);
            if (!File.Exists(thumbnailPath))
            {
                this.logger.Warning("Thumbnail file not found: {FilePath}", thumbnailPath);
                return null;
            }

            return File.OpenRead(thumbnailPath);
        }

        /// <inheritdoc/>
        public async Task<MessageAttachmentDto> UpdateAttachmentAsync(
            string attachmentId,
            Stream fileStream,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var attachment = await this.attachmentRepository.GetByIdAsync(attachmentId, cancellationToken);
            if (attachment == null)
            {
                throw new InvalidOperationException($"Attachment not found: {attachmentId}");
            }

            // Delete old file from disk
            var oldFilePath = Path.Combine(this.storagePath, attachment.FilePath);
            if (File.Exists(oldFilePath))
            {
                try
                {
                    File.Delete(oldFilePath);
                    this.logger.Information("Old attachment file deleted: {FilePath}", oldFilePath);
                }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "Failed to delete old attachment file: {FilePath}", oldFilePath);
                }
            }

            // Delete old thumbnail if exists
            if (!string.IsNullOrEmpty(attachment.ThumbnailPath))
            {
                var oldThumbnailPath = Path.Combine(this.storagePath, attachment.ThumbnailPath);
                if (File.Exists(oldThumbnailPath))
                {
                    try
                    {
                        File.Delete(oldThumbnailPath);
                        this.logger.Information("Old thumbnail file deleted: {FilePath}", oldThumbnailPath);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Warning(ex, "Failed to delete old thumbnail file: {FilePath}", oldThumbnailPath);
                    }
                }
            }

            // Save new file to disk (reuse same file path)
            using (var fileStreamOut = File.Create(oldFilePath))
            {
                await fileStream.CopyToAsync(fileStreamOut, cancellationToken);
            }

            var fileInfo = new FileInfo(oldFilePath);
            var attachmentType = FileHelper.GetAttachmentType(attachment.FileName);

            // Generate thumbnail for images
            string? thumbnailPath = null;
            if (attachmentType == AttachmentType.Image)
            {
                try
                {
                    thumbnailPath = await this.GenerateThumbnailAsync(oldFilePath, attachmentId, cancellationToken);
                    if (thumbnailPath != null)
                    {
                        this.logger.Information("Thumbnail generated successfully for updated image: {AttachmentId}", attachmentId);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "Failed to generate thumbnail for updated image: {AttachmentId}", attachmentId);
                }
            }

            // Update attachment model
            attachment.FileSize = fileInfo.Length;
            attachment.ContentType = contentType;
            attachment.ThumbnailPath = thumbnailPath;
            attachment.UploadedAt = DateTime.UtcNow;

            // Update in database
            await this.attachmentRepository.UpdateAsync(attachment, cancellationToken);

            this.logger.Information(
                "Attachment updated: {AttachmentId}, New size: {FileSize}",
                attachmentId,
                FileHelper.FormatFileSize(fileInfo.Length));

            return this.MapToDto(attachment);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAttachmentAsync(
            string attachmentId,
            CancellationToken cancellationToken = default)
        {
            var attachment = await this.attachmentRepository.GetByIdAsync(attachmentId, cancellationToken);
            if (attachment == null)
            {
                return false;
            }

            // Delete file from disk
            var filePath = Path.Combine(this.storagePath, attachment.FilePath);
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    this.logger.Information("Attachment file deleted: {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "Failed to delete attachment file: {FilePath}", filePath);
                }
            }

            // Delete thumbnail from disk
            if (!string.IsNullOrEmpty(attachment.ThumbnailPath))
            {
                var thumbnailPath = Path.Combine(this.storagePath, attachment.ThumbnailPath);
                if (File.Exists(thumbnailPath))
                {
                    try
                    {
                        File.Delete(thumbnailPath);
                        this.logger.Information("Thumbnail file deleted: {FilePath}", thumbnailPath);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Warning(ex, "Failed to delete thumbnail file: {FilePath}", thumbnailPath);
                    }
                }
            }

            // Delete from database
            await this.attachmentRepository.DeleteAsync(attachmentId, cancellationToken);

            this.logger.Information("Attachment deleted: {AttachmentId}", attachmentId);

            return true;
        }

        /// <summary>
        /// Generates a thumbnail for an image file.
        /// </summary>
        /// <param name="imagePath">Path to the original image.</param>
        /// <param name="attachmentId">The attachment ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The relative path to the thumbnail file, or null if generation failed.</returns>
        private async Task<string?> GenerateThumbnailAsync(
            string imagePath,
            string attachmentId,
            CancellationToken cancellationToken = default)
        {
            const int maxThumbnailWidth = 200;
            const int maxThumbnailHeight = 200;

            try
            {
                var thumbnailFileName = $"{attachmentId}_thumb.jpg";
                var thumbnailPath = Path.Combine(this.storagePath, thumbnailFileName);

                using (var image = await Image.LoadAsync(imagePath, cancellationToken))
                {
                    // Calculate thumbnail dimensions maintaining aspect ratio
                    var ratioX = (double)maxThumbnailWidth / image.Width;
                    var ratioY = (double)maxThumbnailHeight / image.Height;
                    var ratio = Math.Min(ratioX, ratioY);

                    var newWidth = (int)(image.Width * ratio);
                    var newHeight = (int)(image.Height * ratio);

                    // Resize image
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(newWidth, newHeight),
                        Mode = ResizeMode.Max,
                    }));

                    // Save thumbnail as JPEG
                    await image.SaveAsJpegAsync(thumbnailPath, cancellationToken);
                }

                return thumbnailFileName;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to generate thumbnail for: {ImagePath}", imagePath);
                return null;
            }
        }

        private MessageAttachmentDto MapToDto(MessageAttachment attachment)
        {
            var thumbnailUrl = !string.IsNullOrEmpty(attachment.ThumbnailPath)
                ? $"/api/attachments/thumbnail/{attachment.Id}"
                : null;

            this.logger.Debug(
                "Mapping attachment to DTO: {AttachmentId}, HasThumbnail: {HasThumbnail}, ThumbnailUrl: {ThumbnailUrl}",
                attachment.Id,
                !string.IsNullOrEmpty(attachment.ThumbnailPath),
                thumbnailUrl);

            return new MessageAttachmentDto
            {
                Id = attachment.Id,
                MessageId = attachment.MessageId,
                FileName = attachment.FileName,
                FileSize = attachment.FileSize,
                ContentType = attachment.ContentType,
                AttachmentType = attachment.AttachmentType,
                DownloadUrl = $"/api/attachments/download/{attachment.Id}",
                ThumbnailUrl = thumbnailUrl,
                UploadedAt = attachment.UploadedAt,
            };
        }
    }
}
