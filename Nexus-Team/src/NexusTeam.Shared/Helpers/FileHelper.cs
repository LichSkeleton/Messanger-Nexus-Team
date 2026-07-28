namespace NexusTeam.Shared.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// Helper class for file operations and validation.
    /// </summary>
    public static class FileHelper
    {
        /// <summary>
        /// Maximum file size allowed: 100 MB.
        /// </summary>
        public const long MaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB

        /// <summary>
        /// Maximum file size for images: 10 MB.
        /// </summary>
        public const long MaxImageSizeBytes = 10 * 1024 * 1024; // 10 MB

        private static readonly Dictionary<string, AttachmentType> ExtensionToTypeMap = new Dictionary<string, AttachmentType>(StringComparer.OrdinalIgnoreCase)
        {
            // Images
            { ".jpg", AttachmentType.Image },
            { ".jpeg", AttachmentType.Image },
            { ".png", AttachmentType.Image },
            { ".gif", AttachmentType.Image },
            { ".bmp", AttachmentType.Image },
            { ".webp", AttachmentType.Image },
            { ".svg", AttachmentType.Image },

            // Videos
            { ".mp4", AttachmentType.Video },
            { ".avi", AttachmentType.Video },
            { ".mkv", AttachmentType.Video },
            { ".mov", AttachmentType.Video },
            { ".wmv", AttachmentType.Video },
            { ".flv", AttachmentType.Video },
            { ".webm", AttachmentType.Video },

            // Audio
            { ".mp3", AttachmentType.Audio },
            { ".wav", AttachmentType.Audio },
            { ".ogg", AttachmentType.Audio },
            { ".flac", AttachmentType.Audio },
            { ".aac", AttachmentType.Audio },
            { ".m4a", AttachmentType.Audio },

            // Documents
            { ".pdf", AttachmentType.Document },
            { ".doc", AttachmentType.Document },
            { ".docx", AttachmentType.Document },
            { ".xls", AttachmentType.Document },
            { ".xlsx", AttachmentType.Document },
            { ".ppt", AttachmentType.Document },
            { ".pptx", AttachmentType.Document },
            { ".txt", AttachmentType.Document },
            { ".rtf", AttachmentType.Document },
            { ".odt", AttachmentType.Document },

            // Archives
            { ".zip", AttachmentType.Archive },
            { ".rar", AttachmentType.Archive },
            { ".7z", AttachmentType.Archive },
            { ".tar", AttachmentType.Archive },
            { ".gz", AttachmentType.Archive },
            { ".bz2", AttachmentType.Archive },

            // Code
            { ".cs", AttachmentType.Code },
            { ".js", AttachmentType.Code },
            { ".ts", AttachmentType.Code },
            { ".py", AttachmentType.Code },
            { ".java", AttachmentType.Code },
            { ".cpp", AttachmentType.Code },
            { ".c", AttachmentType.Code },
            { ".h", AttachmentType.Code },
            { ".hpp", AttachmentType.Code },
            { ".go", AttachmentType.Code },
            { ".rs", AttachmentType.Code },
            { ".php", AttachmentType.Code },
            { ".rb", AttachmentType.Code },
            { ".swift", AttachmentType.Code },
            { ".kt", AttachmentType.Code },
            { ".scala", AttachmentType.Code },
            { ".html", AttachmentType.Code },
            { ".css", AttachmentType.Code },
            { ".json", AttachmentType.Code },
            { ".xml", AttachmentType.Code },
            { ".yaml", AttachmentType.Code },
            { ".yml", AttachmentType.Code },
            { ".sql", AttachmentType.Code },
            { ".sh", AttachmentType.Code },
            { ".bat", AttachmentType.Code },
            { ".ps1", AttachmentType.Code },
        };

        /// <summary>
        /// Gets the attachment type based on file extension.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The attachment type.</returns>
        public static AttachmentType GetAttachmentType(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return ExtensionToTypeMap.TryGetValue(extension, out var type) ? type : AttachmentType.Other;
        }

        /// <summary>
        /// Validates if a file size is within allowed limits.
        /// </summary>
        /// <param name="fileSize">The file size in bytes.</param>
        /// <param name="attachmentType">The attachment type.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool IsValidFileSize(long fileSize, AttachmentType attachmentType)
        {
            if (fileSize <= 0)
            {
                return false;
            }

            if (attachmentType == AttachmentType.Image)
            {
                return fileSize <= MaxImageSizeBytes;
            }

            return fileSize <= MaxFileSizeBytes;
        }

        /// <summary>
        /// Gets a human-readable file size string.
        /// </summary>
        /// <param name="bytes">The size in bytes.</param>
        /// <returns>Formatted file size string.</returns>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Gets the MIME type for a file based on extension.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The MIME type string.</returns>
        public static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".7z" => "application/x-7z-compressed",
                ".txt" => "text/plain",
                ".json" => "application/json",
                ".xml" => "application/xml",
                _ => "application/octet-stream",
            };
        }

        /// <summary>
        /// Validates if a file extension is allowed.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>True if allowed, false otherwise.</returns>
        public static bool IsAllowedFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            // Block potentially dangerous extensions
            var blockedExtensions = new[] { ".exe", ".dll", ".bat", ".cmd", ".scr", ".vbs", ".js" };
            return !blockedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }
    }
}
