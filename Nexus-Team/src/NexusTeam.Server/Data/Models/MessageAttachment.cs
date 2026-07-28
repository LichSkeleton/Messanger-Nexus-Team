namespace NexusTeam.Server.Data.Models
{
    using System;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    /// <summary>
    /// MongoDB model for MessageAttachment.
    /// </summary>
    public class MessageAttachment
    {
        /// <summary>
        /// Gets or sets the unique identifier for the attachment.
        /// </summary>
        [BsonId]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the message ID this attachment belongs to.
        /// </summary>
        [BsonElement("messageId")]
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the original file name.
        /// </summary>
        [BsonElement("fileName")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the server storage path.
        /// </summary>
        [BsonElement("filePath")]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the thumbnail storage path (for images).
        /// </summary>
        [BsonElement("thumbnailPath")]
        public string? ThumbnailPath { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        [BsonElement("fileSize")]
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the file.
        /// </summary>
        [BsonElement("contentType")]
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the attachment type (0=Image, 1=Video, 2=Audio, 3=Document, 4=Archive, 5=Code, 99=Other).
        /// </summary>
        [BsonElement("attachmentType")]
        public int AttachmentType { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the attachment was uploaded.
        /// </summary>
        [BsonElement("uploadedAt")]
        public DateTime UploadedAt { get; set; }
    }
}
