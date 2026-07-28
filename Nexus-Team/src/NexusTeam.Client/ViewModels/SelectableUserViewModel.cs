namespace NexusTeam.Client.ViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// View model for a selectable user in chat creation dialog.
    /// </summary>
    public class SelectableUserViewModel : ObservableObject
    {
        private bool isSelected;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectableUserViewModel"/> class.
        /// </summary>
        public SelectableUserViewModel()
        {
            this.User = new UserDto();
        }

        /// <summary>
        /// Gets or sets the underlying user DTO.
        /// </summary>
        public UserDto User { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this user is selected.
        /// </summary>
        public bool IsSelected
        {
            get => this.isSelected;
            set => this.SetProperty(ref this.isSelected, value);
        }

        /// <summary>
        /// Gets the display name of the user.
        /// </summary>
        public string DisplayName => !string.IsNullOrWhiteSpace(this.User?.DisplayName)
            ? this.User.DisplayName
            : (this.User?.Username ?? "Unknown");
    }
}
