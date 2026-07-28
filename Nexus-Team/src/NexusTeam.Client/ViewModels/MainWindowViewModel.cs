namespace NexusTeam.Client.ViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;

    /// <summary>
    /// View model for the main window.
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ViewModelBase? currentViewModel;

        [ObservableProperty]
        private string title = "Nexus Team";

        [ObservableProperty]
        private ChatViewModel? chatViewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel()
        {
        }

        /// <summary>
        /// Sets the current view model.
        /// </summary>
        /// <param name="viewModel">View model to set as current.</param>
        public void SetCurrentViewModel(ViewModelBase? viewModel)
        {
            this.CurrentViewModel = viewModel;

            // Store ChatViewModel reference for use in other views
            if (viewModel is ChatViewModel chatViewModel)
            {
                this.ChatViewModel = chatViewModel;
            }
        }
    }
}
