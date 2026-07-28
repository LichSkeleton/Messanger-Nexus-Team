namespace NexusTeam.Client.ViewModels
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using Microsoft.Win32;
    using NexusTeam.Client.Services;
    using NexusTeam.Client.Views;
    using NexusTeam.Shared.Enums;
    using Serilog;

    /// <summary>
    /// View model for the settings view.
    /// </summary>
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly INavigationService navigationService;
        private readonly IAuthenticationService authService;
        private readonly IMessagingService messagingService;
        private readonly ILogger logger;
        private readonly IErrorHandlingService errorHandlingService;
        private readonly IAvatarService avatarService;
        private BitmapImage? avatarImage;
        private bool isLoadingStatus;
        private bool isUpdatingStatus;
        private UserStatus myStatus = UserStatus.Online;
        private bool isStatusDropdownOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        /// <param name="navigationService">The navigation service.</param>
        /// <param name="authService">The authentication service.</param>
        /// <param name="messagingService">The messaging service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="errorHandlingService">The error handling service.</param>
        /// <param name="avatarService">The avatar service.</param>
        public SettingsViewModel(
            INavigationService navigationService,
            IAuthenticationService authService,
            IMessagingService messagingService,
            ILogger logger,
            IErrorHandlingService errorHandlingService,
            IAvatarService avatarService)
        {
            this.navigationService = navigationService;
            this.authService = authService;
            this.messagingService = messagingService;
            this.logger = logger;
            this.errorHandlingService = errorHandlingService;
            this.avatarService = avatarService;
            this.Title = "Settings";
            this.LoadAvatarAsync();
            _ = this.LoadStatusAsync();
        }

        /// <summary>
        /// Gets or sets the current user's presence status (Online or Invisible).
        /// </summary>
        public UserStatus MyStatus
        {
            get => this.myStatus;
            set
            {
                if (this.SetProperty(ref this.myStatus, value))
                {
                    this.OnPropertyChanged(nameof(this.StatusLabel));
                    this.OnPropertyChanged(nameof(this.ShowOnlineStatus));
                }
            }
        }

        /// <summary>
        /// Gets the display label for the current status.
        /// </summary>
        public string StatusLabel => this.myStatus == UserStatus.Invisible ? "Invisible" : "Online";

        /// <summary>
        /// Gets or sets a value indicating whether the status dropdown is open.
        /// </summary>
        public bool IsStatusDropdownOpen
        {
            get => this.isStatusDropdownOpen;
            set => this.SetProperty(ref this.isStatusDropdownOpen, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the user appears online to others.
        /// When false, Invisible mode is used (others see gray/offline).
        /// </summary>
        public bool ShowOnlineStatus
        {
            get => this.myStatus != UserStatus.Invisible;
            set
            {
                // Kept for compatibility; prefer SetOnlineStatus / SetInvisibleStatus commands.
                if (this.isLoadingStatus || this.isUpdatingStatus)
                {
                    return;
                }

                _ = this.ApplyStatusAsync(value ? UserStatus.Online : UserStatus.Invisible);
            }
        }

        /// <summary>
        /// Gets the current user's email.
        /// </summary>
        public string UserEmail => this.authService.CurrentUser?.Email ?? "No Email";

        /// <summary>
        /// Gets the current user's username.
        /// </summary>
        public string Username => this.authService.CurrentUser?.Username ?? "Guest";

        private string editableDisplayName = string.Empty;
        private bool isEditingDisplayName;

        /// <summary>
        /// Gets the current user's display name.
        /// </summary>
        public string DisplayName => this.authService.CurrentUser?.DisplayName ?? "Guest";

        /// <summary>
        /// Gets or sets the display name being edited.
        /// </summary>
        public string EditableDisplayName
        {
            get => this.editableDisplayName;
            set => this.SetProperty(ref this.editableDisplayName, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the display name is being edited.
        /// </summary>
        public bool IsEditingDisplayName
        {
            get => this.isEditingDisplayName;
            set => this.SetProperty(ref this.isEditingDisplayName, value);
        }

        /// <summary>
        /// Command to start editing the display name.
        /// </summary>
        [RelayCommand]
        private void StartEditingDisplayName()
        {
            this.EditableDisplayName = this.DisplayName;
            this.IsEditingDisplayName = true;
        }

        /// <summary>
        /// Command to save the display name.
        /// </summary>
        [RelayCommand]
        private async Task SaveDisplayNameAsync()
        {
            if (string.IsNullOrWhiteSpace(this.EditableDisplayName))
            {
                this.errorHandlingService.ShowWarning("Display name cannot be empty");
                return;
            }

            if (this.EditableDisplayName == this.DisplayName)
            {
                this.IsEditingDisplayName = false;
                return;
            }

            try
            {
                await this.authService.UpdateProfileAsync(this.EditableDisplayName);
                this.IsEditingDisplayName = false;
                this.OnPropertyChanged(nameof(this.DisplayName));
                this.errorHandlingService.ShowInfo("Display name updated successfully");
            }
            catch (Exception ex)
            {
                this.errorHandlingService.HandleError(ex, "Failed to update display name");
            }
        }

        /// <summary>
        /// Command to cancel editing the display name.
        /// </summary>
        [RelayCommand]
        private void CancelEditingDisplayName()
        {
            this.IsEditingDisplayName = false;
            this.EditableDisplayName = string.Empty;
        }

        /// <summary>
        /// Command to copy user email to clipboard.
        /// </summary>
        [RelayCommand]
        private void CopyEmail()
        {
            if (!string.IsNullOrEmpty(this.UserEmail))
            {
                System.Windows.Clipboard.SetText(this.UserEmail);
                this.errorHandlingService.ShowInfo("Email copied to clipboard");
            }
        }

        /// <summary>
        /// Command to copy username to clipboard.
        /// </summary>
        [RelayCommand]
        private void CopyUsername()
        {
            if (!string.IsNullOrEmpty(this.Username))
            {
                System.Windows.Clipboard.SetText(this.Username);
                this.errorHandlingService.ShowInfo("Username copied to clipboard");
            }
        }

        /// <summary>
        /// Command to navigate back to the previous view.
        /// </summary>
        [RelayCommand]
        private void NavigateBack()
        {
            this.navigationService.NavigateBack();
        }

        /// <summary>
        /// Command to navigate to the chat view.
        /// </summary>
        [RelayCommand]
        private void NavigateToChat()
        {
            this.navigationService.NavigateTo<ChatViewModel>();
        }

        /// <summary>
        /// Command to navigate to the generator view.
        /// </summary>
        [RelayCommand]
        private void NavigateToGenerator()
        {
            this.navigationService.NavigateTo<GeneratorViewModel>();
        }

        /// <summary>
        /// Gets the current user's avatar image.
        /// </summary>
        public BitmapImage? AvatarImage
        {
            get => this.avatarImage;
            private set => this.SetProperty(ref this.avatarImage, value);
        }

        /// <summary>
        /// Gets the current user's avatar URL.
        /// </summary>
        public string? AvatarUrl => this.authService.CurrentUser?.AvatarUrl;

        /// <summary>
        /// Loads the avatar image asynchronously.
        /// </summary>
        private async void LoadAvatarAsync()
        {
            try
            {
                var avatarUrl = this.AvatarUrl;
                if (string.IsNullOrEmpty(avatarUrl))
                {
                    this.AvatarImage = this.avatarService.GetDefaultAvatar();
                    return;
                }

                // Add timestamp to force refresh
                var separator = avatarUrl.Contains('?') ? "&" : "?";
                var cacheBustingUrl = $"{avatarUrl}{separator}t={DateTime.UtcNow.Ticks}";
                this.AvatarImage = await this.avatarService.LoadAvatarAsync(cacheBustingUrl);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to load avatar");
                this.AvatarImage = this.avatarService.GetDefaultAvatar();
            }
        }

        /// <summary>
        /// Command to change the avatar.
        /// </summary>
        [RelayCommand]
        private async Task ChangeAvatarAsync()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Image files (*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|All files (*.*)|*.*",
                    Title = "Select Avatar Image",
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var filePath = openFileDialog.FileName;
                    this.logger.Information("User selected avatar file: {FilePath}", filePath);

                    // Upload avatar - this updates the avatar on the server and returns updated UserDto
                    this.logger.Information("Starting avatar upload from file: {FilePath}", filePath);
                    var updatedUser = await this.avatarService.UploadAvatarAsync(filePath);

                    if (updatedUser == null || string.IsNullOrEmpty(updatedUser.AvatarUrl))
                    {
                        throw new InvalidOperationException("Avatar upload failed: server did not return valid user data or avatar URL");
                    }

                    this.logger.Information("Avatar uploaded successfully. Server returned AvatarUrl: {Url}", updatedUser.AvatarUrl);

                    // Update CurrentUser with the complete updated user data
                    this.authService.UpdateCurrentUserAvatar(updatedUser.AvatarUrl);
                    this.logger.Debug("Updated CurrentUser.AvatarUrl to: {Url}", updatedUser.AvatarUrl);

                    // Reload avatar with new URL (cache is bypassed via IgnoreImageCache in AvatarService)
                    // Add timestamp to URL to force refresh
                    var separator = updatedUser.AvatarUrl.Contains('?') ? "&" : "?";
                    var cacheBustingUrl = $"{updatedUser.AvatarUrl}{separator}t={DateTime.UtcNow.Ticks}";
                    this.logger.Debug("Loading avatar from URL: {Url}", cacheBustingUrl);
                    this.AvatarImage = await this.avatarService.LoadAvatarAsync(cacheBustingUrl);
                    this.logger.Debug("Avatar image loaded. AvatarImage is null: {IsNull}", this.AvatarImage == null);

                    // Notify property changes
                    this.OnPropertyChanged(nameof(this.AvatarUrl));
                    this.OnPropertyChanged(nameof(this.AvatarImage));
                    this.logger.Debug("Property change notifications sent for AvatarUrl and AvatarImage");

                    this.errorHandlingService.ShowInfo("Avatar updated successfully");
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to change avatar");
                this.errorHandlingService.HandleError(ex, "Failed to change avatar");
            }
        }

        /// <summary>
        /// Command to view the avatar in full screen.
        /// </summary>
        [RelayCommand]
        private void ViewAvatar()
        {
            try
            {
                if (this.AvatarImage == null)
                {
                    return;
                }

                // Open avatar in ImageViewerDialog with proper owner for size adaptation
                var dialog = new ImageViewerDialog(this.AvatarImage)
                {
                    Owner = System.Windows.Application.Current.MainWindow,
                };

                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to view avatar");
                this.errorHandlingService.HandleError(ex, "Failed to view avatar");
            }
        }

        /// <summary>
        /// Command to logout the current user.
        /// </summary>
        [RelayCommand]
        private async Task LogoutAsync()
        {
            try
            {
                this.logger.Information("Logout command executed");
                await this.authService.LogoutAsync();
                this.logger.Information("User logged out successfully");
                this.navigationService.NavigateTo<LoginViewModel>();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error during logout");

                // Navigate to login anyway even if there's an error
                this.navigationService.NavigateTo<LoginViewModel>();
            }
        }

        /// <summary>
        /// Opens or closes the status dropdown under the profile title.
        /// </summary>
        [RelayCommand]
        private void ToggleStatusDropdown()
        {
            this.IsStatusDropdownOpen = !this.IsStatusDropdownOpen;
        }

        /// <summary>
        /// Sets status to Online (visible to others).
        /// </summary>
        [RelayCommand]
        private async Task SetOnlineStatusAsync()
        {
            this.IsStatusDropdownOpen = false;
            await this.ApplyStatusAsync(UserStatus.Online);
        }

        /// <summary>
        /// Sets status to Invisible (appears offline to others).
        /// </summary>
        [RelayCommand]
        private async Task SetInvisibleStatusAsync()
        {
            this.IsStatusDropdownOpen = false;
            await this.ApplyStatusAsync(UserStatus.Invisible);
        }

        private async Task LoadStatusAsync()
        {
            try
            {
                this.isLoadingStatus = true;
                var status = await this.messagingService.GetMyStatusAsync();
                this.MyStatus = status.Status == UserStatus.Invisible
                    ? UserStatus.Invisible
                    : UserStatus.Online;
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to load user status preference");
                this.MyStatus = UserStatus.Online;
            }
            finally
            {
                this.isLoadingStatus = false;
            }
        }

        private async Task ApplyStatusAsync(UserStatus targetStatus)
        {
            if (this.isUpdatingStatus || this.myStatus == targetStatus)
            {
                return;
            }

            var previous = this.myStatus;
            this.isUpdatingStatus = true;
            this.MyStatus = targetStatus;

            try
            {
                await this.messagingService.SetMyStatusAsync(targetStatus);
                this.logger.Information("Updated presence status to {Status}", targetStatus);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to update status to {Status}", targetStatus);
                this.MyStatus = previous;
                this.errorHandlingService.HandleError(ex, "Failed to update online status");
            }
            finally
            {
                this.isUpdatingStatus = false;
            }
        }

        private async Task UpdateOnlineVisibilityAsync(bool showOnline)
        {
            await this.ApplyStatusAsync(showOnline ? UserStatus.Online : UserStatus.Invisible);
        }
    }
}
