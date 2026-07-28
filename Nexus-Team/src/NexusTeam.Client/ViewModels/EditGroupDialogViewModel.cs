namespace NexusTeam.Client.ViewModels
{
    using System.Windows.Media.Imaging;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using Microsoft.Win32;

    /// <summary>
    /// View model for editing a group chat name and avatar (owner only).
    /// </summary>
    public class EditGroupDialogViewModel : ObservableObject
    {
        private string groupName = string.Empty;
        private string? avatarFilePath;
        private BitmapImage? avatarPreview;
        private string? validationError;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditGroupDialogViewModel"/> class.
        /// </summary>
        public EditGroupDialogViewModel()
        {
            this.BrowseAvatarCommand = new RelayCommand(this.BrowseAvatar);
            this.ConfirmCommand = new RelayCommand(this.OnConfirm, this.CanConfirm);
            this.CancelCommand = new RelayCommand(this.OnCancel);
        }

        /// <summary>
        /// Gets or sets the group name.
        /// </summary>
        public string GroupName
        {
            get => this.groupName;
            set
            {
                if (this.SetProperty(ref this.groupName, value))
                {
                    this.ConfirmCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets the selected local avatar file path, if any.
        /// </summary>
        public string? AvatarFilePath => this.avatarFilePath;

        /// <summary>
        /// Gets the avatar preview image.
        /// </summary>
        public BitmapImage? AvatarPreview
        {
            get => this.avatarPreview;
            private set => this.SetProperty(ref this.avatarPreview, value);
        }

        /// <summary>
        /// Gets or sets validation error text.
        /// </summary>
        public string? ValidationError
        {
            get => this.validationError;
            set => this.SetProperty(ref this.validationError, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the dialog was confirmed.
        /// </summary>
        public bool DialogResult { get; set; }

        /// <summary>
        /// Gets the browse avatar command.
        /// </summary>
        public RelayCommand BrowseAvatarCommand { get; }

        /// <summary>
        /// Gets the confirm command.
        /// </summary>
        public RelayCommand ConfirmCommand { get; }

        /// <summary>
        /// Gets the cancel command.
        /// </summary>
        public RelayCommand CancelCommand { get; }

        /// <summary>
        /// Initializes dialog fields from an existing conversation.
        /// </summary>
        /// <param name="name">Current group name.</param>
        /// <param name="avatarImage">Current avatar image preview.</param>
        public void Initialize(string name, BitmapImage? avatarImage)
        {
            this.GroupName = name ?? string.Empty;
            this.AvatarPreview = avatarImage;
            this.avatarFilePath = null;
        }

        private void BrowseAvatar()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files|*.*",
                Title = "Select group avatar",
            };

            if (dialog.ShowDialog() == true)
            {
                this.avatarFilePath = dialog.FileName;
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new System.Uri(dialog.FileName);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    this.AvatarPreview = bitmap;
                }
                catch
                {
                    this.ValidationError = "Could not load the selected image.";
                }
            }
        }

        private bool CanConfirm() => !string.IsNullOrWhiteSpace(this.groupName);

        private void OnConfirm()
        {
            if (!this.CanConfirm())
            {
                this.ValidationError = "Group name is required.";
                return;
            }

            this.DialogResult = true;
        }

        private void OnCancel()
        {
            this.DialogResult = false;
        }
    }
}
