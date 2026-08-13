namespace NexusTeam.Client.ViewModels
{
    using System;
    using System.IO;
    using System.Windows.Media.Imaging;
    using CommunityToolkit.Mvvm.ComponentModel;
    using NexusTeam.Client.Services;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using NexusTeam.Shared.Helpers;
    using Serilog;

    /// <summary>
    /// View model for a file attachment.
    /// </summary>
    public partial class AttachmentViewModel : ObservableObject
    {
        private readonly IFileAttachmentService? fileAttachmentService;

        private string filePath;
        private string fileName;
        private long fileSize;
        private AttachmentType attachmentType;
        private bool isUploading;
        private BitmapImage? thumbnailImage;
        private bool isCompressed = true;
        private bool isPlaying;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentViewModel"/> class from local file.
        /// </summary>
        /// <param name="filePath">Local file path.</param>
        public AttachmentViewModel(string filePath)
        {
            this.filePath = filePath;
            this.fileName = System.IO.Path.GetFileName(filePath);

            var fileInfo = new System.IO.FileInfo(filePath);
            this.fileSize = fileInfo.Length;
            this.attachmentType = FileHelper.GetAttachmentType(this.fileName);

            // Load thumbnail for images
            if (this.IsImage && File.Exists(filePath))
            {
                this.LoadThumbnail(filePath);
            }

            Log.Debug(
                "Created AttachmentViewModel for {FileName}, IsImage: {IsImage}, IsCompressed (default): {IsCompressed}",
                this.fileName,
                this.IsImage,
                this.isCompressed);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentViewModel"/> class from DTO.
        /// </summary>
        /// <param name="dto">The attachment DTO.</param>
        /// <param name="fileAttachmentService">Optional file attachment service for loading images.</param>
        public AttachmentViewModel(MessageAttachmentDto dto, IFileAttachmentService? fileAttachmentService = null)
        {
            this.filePath = dto.DownloadUrl;
            this.fileName = dto.FileName;
            this.fileSize = dto.FileSize;
            this.attachmentType = dto.AttachmentType;
            this.AttachmentDto = dto;
            this.fileAttachmentService = fileAttachmentService;

            // Load thumbnail for images from server
            // Always try to load thumbnail for images, even if ThumbnailUrl is not set (will fallback to full image)
            if (this.IsImage && fileAttachmentService != null)
            {
                this.LoadThumbnailFromService(dto);
            }
        }

        /// <summary>
        /// Gets or sets the local file path or download URL.
        /// </summary>
        public string FilePath
        {
            get => this.filePath;
            set => this.SetProperty(ref this.filePath, value);
        }

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string FileName
        {
            get => this.fileName;
            set => this.SetProperty(ref this.fileName, value);
        }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        public long FileSize
        {
            get => this.fileSize;
            set => this.SetProperty(ref this.fileSize, value);
        }

        /// <summary>
        /// Gets the formatted file size.
        /// </summary>
        public string FileSizeFormatted => FileHelper.FormatFileSize(this.fileSize);

        /// <summary>
        /// Gets or sets the attachment type.
        /// </summary>
        public AttachmentType AttachmentType
        {
            get => this.attachmentType;
            set => this.SetProperty(ref this.attachmentType, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the file is currently uploading.
        /// </summary>
        public bool IsUploading
        {
            get => this.isUploading;
            set => this.SetProperty(ref this.isUploading, value);
        }

        /// <summary>
        /// Gets or sets the attachment DTO (after upload).
        /// </summary>
        public MessageAttachmentDto? AttachmentDto { get; set; }

        /// <summary>
        /// Gets the icon for this attachment type.
        /// </summary>
        public string Icon => this.attachmentType switch
        {
            AttachmentType.Image => "\uE91B",      // Image icon
            AttachmentType.Video => "\uE714",      // Video icon
            AttachmentType.Audio => "\uE8D6",      // Audio icon
            AttachmentType.Document => "\uE8A5",   // Document icon
            AttachmentType.Archive => "\uE8B7",    // Archive icon
            AttachmentType.Code => "\uE943",       // Code icon
            _ => "\uE7C3",                         // Default file icon
        };

        /// <summary>
        /// Gets a value indicating whether this is an image attachment.
        /// </summary>
        public bool IsImage => this.attachmentType == AttachmentType.Image;

        /// <summary>
        /// Gets a value indicating whether this is an audio attachment.
        /// </summary>
        public bool IsAudio => this.attachmentType == AttachmentType.Audio;

        /// <summary>
        /// Gets a value indicating whether this is a code file attachment.
        /// </summary>
        public bool IsCodeFile => this.attachmentType == AttachmentType.Code;

        /// <summary>
        /// Gets a value indicating whether this attachment can be previewed in the app.
        /// </summary>
        public bool CanPreview => this.AttachmentDto != null && FileHelper.IsPreviewableFile(this.fileName);

        /// <summary>
        /// Gets or sets the thumbnail image for preview.
        /// </summary>
        public BitmapImage? ThumbnailImage
        {
            get => this.thumbnailImage;
            set
            {
                if (this.SetProperty(ref this.thumbnailImage, value))
                {
                    this.OnPropertyChanged(nameof(this.HasThumbnail));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether a thumbnail is available.
        /// </summary>
        public bool HasThumbnail => this.thumbnailImage != null;

        /// <summary>
        /// Gets or sets a value indicating whether the image should be compressed before upload.
        /// </summary>
        public bool IsCompressed
        {
            get => this.isCompressed;
            set
            {
                if (this.SetProperty(ref this.isCompressed, value))
                {
                    Log.Debug(
                        "IsCompressed changed for {FileName}: {IsCompressed}",
                        this.fileName,
                        value);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the attachment can be compressed (is an image).
        /// </summary>
        public bool CanCompress => this.IsImage;

        /// <summary>
        /// Gets or sets a value indicating whether the audio is currently playing.
        /// </summary>
        public bool IsPlaying
        {
            get => this.isPlaying;
            set => this.SetProperty(ref this.isPlaying, value);
        }

        /// <summary>
        /// Loads a thumbnail image from the file path.
        /// </summary>
        /// <param name="imagePath">The path to the image file.</param>
        private void LoadThumbnail(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 100; // Thumbnail width
                bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze(); // Make it thread-safe

                this.ThumbnailImage = bitmap;
            }
            catch
            {
                // Failed to load thumbnail - will show icon instead
                this.ThumbnailImage = null;
            }
        }

        /// <summary>
        /// Loads a thumbnail image from server using file attachment service (async).
        /// </summary>
        /// <param name="attachment">The attachment DTO.</param>
        private async void LoadThumbnailFromService(MessageAttachmentDto attachment)
        {
            if (this.fileAttachmentService == null)
            {
                Log.Warning("FileAttachmentService is null, cannot load thumbnail for {FileName}", attachment.FileName);
                return;
            }

            try
            {
                // Use thumbnail URL if available, otherwise use full image
                Stream imageStream;
                if (!string.IsNullOrEmpty(attachment.ThumbnailUrl))
                {
                    Log.Debug("Loading thumbnail from {ThumbnailUrl} for {FileName}", attachment.ThumbnailUrl, attachment.FileName);

                    // Download thumbnail stream from server
                    imageStream = await this.fileAttachmentService.DownloadThumbnailStreamAsync(attachment);
                }
                else
                {
                    Log.Debug("Thumbnail URL not available, using full image for {FileName}", attachment.FileName);

                    // Fallback to full image if thumbnail is not available
                    imageStream = await this.fileAttachmentService.DownloadImageStreamAsync(attachment);
                }

                using (imageStream)
                {
                    // Read stream into memory (HttpBaseStream doesn't support Length property)
                    using (var memoryStream = new MemoryStream())
                    {
                        await imageStream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;

                        // Create bitmap from bytes
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = memoryStream;
                        bitmap.DecodePixelWidth = 200; // Preview width
                        bitmap.EndInit();
                        bitmap.Freeze(); // Make it thread-safe

                        this.ThumbnailImage = bitmap;
                        Log.Debug("Thumbnail loaded successfully for {FileName}", attachment.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                // Failed to load thumbnail - will show icon instead
                Log.Warning(ex, "Failed to load thumbnail for {FileName}: {Error}", attachment.FileName, ex.Message);
                this.ThumbnailImage = null;
            }
        }
    }
}
