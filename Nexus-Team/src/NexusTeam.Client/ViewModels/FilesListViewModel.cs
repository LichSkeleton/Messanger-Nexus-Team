namespace NexusTeam.Client.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using CommunityToolkit.Mvvm.Input;
    using NexusTeam.Client.Services;
    using Serilog;

    /// <summary>
    /// View model for the files list view.
    /// </summary>
    public partial class FilesListViewModel : ViewModelBase
    {
        private readonly INavigationService navigationService;
        private readonly IFileAttachmentService fileAttachmentService;
        private readonly IErrorHandlingService errorHandlingService;
        private readonly ILogger logger;
        private string conversationName;
        private ObservableCollection<AttachmentViewModel> filesList;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilesListViewModel"/> class.
        /// </summary>
        public FilesListViewModel(
            INavigationService navigationService,
            IFileAttachmentService fileAttachmentService,
            IErrorHandlingService errorHandlingService,
            ILogger logger)
        {
            this.navigationService = navigationService;
            this.fileAttachmentService = fileAttachmentService;
            this.errorHandlingService = errorHandlingService;
            this.logger = logger;
            this.Title = "Files";
            this.conversationName = string.Empty;
            this.filesList = new ObservableCollection<AttachmentViewModel>();
        }

        /// <summary>
        /// Gets or sets the conversation name.
        /// </summary>
        public string ConversationName
        {
            get => this.conversationName;
            set => this.SetProperty(ref this.conversationName, value);
        }

        /// <summary>
        /// Gets the files list collection.
        /// </summary>
        public ObservableCollection<AttachmentViewModel> FilesList => this.filesList;

        /// <summary>
        /// Sets the files to display.
        /// </summary>
        /// <param name="files">The collection of file attachments.</param>
        /// <param name="conversationName">The name of the conversation.</param>
        public void SetFiles(ObservableCollection<AttachmentViewModel> files, string conversationName)
        {
            this.logger.Debug("SetFiles called with {Count} files", files?.Count ?? 0);

            this.filesList.Clear();
            if (files != null)
            {
                foreach (var file in files)
                {
                    this.filesList.Add(file);
                }
            }

            this.ConversationName = conversationName;

            this.logger.Debug("SetFiles completed. FilesList now has {Count} items", this.filesList.Count);

            // Thumbnails are loaded automatically by AttachmentViewModel constructor via LoadThumbnailFromService
        }

        [RelayCommand]
        private void NavigateBack()
        {
            this.navigationService.NavigateBack();
        }

        [RelayCommand]
        private async Task DownloadAttachmentAsync(AttachmentViewModel attachment)
        {
            if (attachment?.AttachmentDto == null)
            {
                return;
            }

            try
            {
                var path = await this.fileAttachmentService.DownloadAttachmentAsync(attachment.AttachmentDto);
                this.errorHandlingService.ShowInfo($"Downloaded to: {path}");
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to download attachment");
                this.errorHandlingService.ShowError(ex.Message);
            }
        }

        [RelayCommand]
        private async Task SaveAttachmentAsAsync(AttachmentViewModel attachment)
        {
            if (attachment?.AttachmentDto == null)
            {
                return;
            }

            try
            {
                var path = await this.fileAttachmentService.SaveAttachmentAsAsync(attachment.AttachmentDto);
                if (path != null)
                {
                    this.errorHandlingService.ShowInfo($"Saved to: {path}");
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to save attachment as");
                this.errorHandlingService.ShowError(ex.Message);
            }
        }

        [RelayCommand]
        private async Task OpenAttachmentAsync(AttachmentViewModel attachment)
        {
            if (attachment?.AttachmentDto == null)
            {
                return;
            }

            try
            {
                await this.fileAttachmentService.OpenAttachmentAsync(attachment.AttachmentDto);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to open attachment");
                this.errorHandlingService.ShowError(ex.Message);
            }
        }
    }
}
