namespace NexusTeam.Shared.Models
{
    using System;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// Represents a file attachment in a message.
    /// </summary>
    public class MessageAttachment
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
        /// Gets or sets the file path on server storage.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the thumbnail path on server storage (for images).
        /// </summary>
        public string? ThumbnailPath { get; set; }

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
        /// Gets or sets the timestamp when the attachment was uploaded.
        /// </summary>
        public DateTime UploadedAt { get; set; }
    }
}
