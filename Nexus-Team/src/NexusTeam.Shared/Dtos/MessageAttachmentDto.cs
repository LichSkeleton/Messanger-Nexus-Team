namespace NexusTeam.Shared.Dtos
{
    using System;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// Data transfer object for message attachment information.
    /// </summary>
    public class MessageAttachmentDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the attachment.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message ID this attachment belongs to.
        /// </summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the original file name.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the file.
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the attachment type category.
        /// </summary>
        public AttachmentType AttachmentType { get; set; }

        /// <summary>
        /// Gets or sets the download URL for the attachment.
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the thumbnail URL for the attachment (for images).
        /// </summary>
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the attachment was uploaded.
        /// </summary>
        public DateTime UploadedAt { get; set; }
    }
}
