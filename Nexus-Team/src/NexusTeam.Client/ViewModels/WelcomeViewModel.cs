namespace NexusTeam.Client.ViewModels
{
    using System.Windows.Input;
    using CommunityToolkit.Mvvm.Input;
    using NexusTeam.Client.Services;

    /// <summary>
    /// View model for the welcome screen.
    /// </summary>
    public class WelcomeViewModel : ViewModelBase
    {
        private readonly INavigationService navigationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="WelcomeViewModel"/> class.
        /// </summary>
        /// <param name="navigationService">Navigation service.</param>
        public WelcomeViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService;
            this.NavigateToLoginCommand = new RelayCommand(this.NavigateToLogin);
            this.NavigateToRegisterCommand = new RelayCommand(this.NavigateToRegister);
        }

        /// <summary>
        /// Gets the command to navigate to login view.
        /// </summary>
        public ICommand NavigateToLoginCommand { get; }

        /// <summary>
        /// Gets the command to navigate to register view.
        /// </summary>
        public ICommand NavigateToRegisterCommand { get; }

        private void NavigateToLogin()
        {
            this.navigationService.NavigateTo<LoginViewModel>();
        }

        private void NavigateToRegister()
        {
            this.navigationService.NavigateTo<RegisterViewModel>();
        }
    }
}
