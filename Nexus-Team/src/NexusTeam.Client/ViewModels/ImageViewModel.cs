namespace NexusTeam.Client.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using NexusTeam.Client.Services;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// View model for image viewer dialog.
    /// </summary>
    public partial class ImageViewModel : ObservableObject
    {
        private readonly IList<MessageAttachmentDto> allImages;
        private readonly IFileAttachmentService fileAttachmentService;
        private int currentIndex;
        private MessageAttachmentDto attachment;
        private BitmapImage? imageSource;
        private bool isLoading = true;
        private bool hasImage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageViewModel"/> class.
        /// </summary>
        /// <param name="attachment">The attachment DTO.</param>
        /// <param name="fileAttachmentService">The file attachment service.</param>
        public ImageViewModel(MessageAttachmentDto attachment, IFileAttachmentService fileAttachmentService)
            : this(new List<MessageAttachmentDto> { attachment }, 0, fileAttachmentService)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageViewModel"/> class with navigation support.
        /// </summary>
        /// <param name="allImages">List of all image attachments.</param>
        /// <param name="currentIndex">Current image index.</param>
        /// <param name="fileAttachmentService">The file attachment service.</param>
        public ImageViewModel(IList<MessageAttachmentDto> allImages, int currentIndex, IFileAttachmentService fileAttachmentService)
        {
            this.allImages = allImages ?? throw new ArgumentNullException(nameof(allImages));
            this.currentIndex = Math.Max(0, Math.Min(currentIndex, allImages.Count - 1));
            this.attachment = allImages[this.currentIndex];
            this.fileAttachmentService = fileAttachmentService;
            this.LoadImageAsync();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageViewModel"/> class with a BitmapImage.
        /// </summary>
        /// <param name="bitmapImage">The bitmap image to display.</param>
        /// <param name="imageName">The name of the image.</param>
        public ImageViewModel(BitmapImage bitmapImage, string imageName = "Image")
        {
            this.allImages = new List<MessageAttachmentDto>();
            this.fileAttachmentService = null!;
            this.currentIndex = 0;
            this.attachment = new MessageAttachmentDto
            {
                FileName = imageName,
                FileSize = 0,
            };
            this.imageSource = bitmapImage;
            this.hasImage = bitmapImage != null;
            this.isLoading = false;
        }

        /// <summary>
        /// Gets the image name.
        /// </summary>
        public string ImageName => this.attachment?.FileName ?? "Image";

        /// <summary>
        /// Gets the formatted image size.
        /// </summary>
        public string ImageSize => $"{this.attachment.FileName} ({this.attachment.FileSize / 1024.0:F2} KB)";

        /// <summary>
        /// Gets the current image position (e.g., "1 / 3").
        /// </summary>
        public string ImagePosition => this.allImages.Count > 1
            ? $"{this.currentIndex + 1} / {this.allImages.Count}"
            : string.Empty;

        /// <summary>
        /// Gets a value indicating whether there is a previous image.
        /// </summary>
        public bool HasPrevious => this.allImages.Count > 1 && this.currentIndex > 0;

        /// <summary>
        /// Gets a value indicating whether there is a next image.
        /// </summary>
        public bool HasNext => this.allImages.Count > 1 && this.currentIndex < this.allImages.Count - 1;

        /// <summary>
        /// Gets or sets the image source.
        /// </summary>
        public BitmapImage? ImageSource
        {
            get => this.imageSource;
            set
            {
                if (this.SetProperty(ref this.imageSource, value))
                {
                    this.HasImage = value != null;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the image is loading.
        /// </summary>
        public bool IsLoading
        {
            get => this.isLoading;
            set => this.SetProperty(ref this.isLoading, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the image is available.
        /// </summary>
        public bool HasImage
        {
            get => this.hasImage;
            set => this.SetProperty(ref this.hasImage, value);
        }

        /// <summary>
        /// Downloads the image to local storage.
        /// </summary>
        [RelayCommand]
        private async Task Download()
        {
            try
            {
                await this.fileAttachmentService.DownloadAttachmentAsync(this.attachment);
            }
            catch (Exception)
            {
                // Error handling is done in the service
            }
        }

        /// <summary>
        /// Prompts user to save image with custom location.
        /// </summary>
        [RelayCommand]
        private async Task SaveAs()
        {
            try
            {
                await this.fileAttachmentService.SaveAttachmentAsAsync(this.attachment);
            }
            catch (Exception)
            {
                // Error handling is done in the service
            }
        }

        /// <summary>
        /// Navigates to the previous image.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasPrevious))]
        private void NavigatePrevious()
        {
            if (this.HasPrevious)
            {
                var newIndex = this.currentIndex - 1;
                this.ChangeImage(newIndex);
            }
        }

        /// <summary>
        /// Navigates to the next image.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasNext))]
        private void NavigateNext()
        {
            if (this.HasNext)
            {
                var newIndex = this.currentIndex + 1;
                this.ChangeImage(newIndex);
            }
        }

        /// <summary>
        /// Changes the current image to the one at the specified index.
        /// </summary>
        /// <param name="index">The index of the image to display.</param>
        private void ChangeImage(int index)
        {
            if (index < 0 || index >= this.allImages.Count)
            {
                return;
            }

            this.currentIndex = index;
            this.attachment = this.allImages[index];
            this.OnPropertyChanged(nameof(this.ImageName));
            this.OnPropertyChanged(nameof(this.ImageSize));
            this.OnPropertyChanged(nameof(this.ImagePosition));
            this.OnPropertyChanged(nameof(this.HasPrevious));
            this.OnPropertyChanged(nameof(this.HasNext));
            this.NavigatePreviousCommand.NotifyCanExecuteChanged();
            this.NavigateNextCommand.NotifyCanExecuteChanged();

            this.LoadImageAsync();
        }

        /// <summary>
        /// Resets zoom to 100%.
        /// </summary>
        private void ResetZoom()
        {
            // This will be called from the dialog to reset zoom
            // We can add an event or property change notification if needed
        }

        /// <summary>
        /// Loads the image from the server.
        /// </summary>
        private async void LoadImageAsync()
        {
            try
            {
                this.IsLoading = true;

                // Always load full image for viewer (not thumbnail) to show best quality
                // Thumbnails are only used for preview in message list
                Stream imageStream = await this.fileAttachmentService.DownloadImageStreamAsync(this.attachment);

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
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = memoryStream;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Make it thread-safe

                        this.ImageSource = bitmap;
                    }
                }
            }
            catch (Exception)
            {
                // Failed to load image - will show error message
                this.HasImage = false;
            }
            finally
            {
                this.IsLoading = false;
            }
        }
    }
}
