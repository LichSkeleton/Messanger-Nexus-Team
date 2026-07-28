namespace NexusTeam.Client.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using NexusTeam.Client.Services;
    using Serilog;

    /// <summary>
    /// View model for the login view.
    /// </summary>
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthenticationService authService;
        private readonly INavigationService navigationService;
        private readonly ILogger logger;

        [ObservableProperty]
        private string usernameOrEmail = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private bool rememberMe;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private bool isLoggingIn;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginViewModel"/> class.
        /// </summary>
        /// <param name="authService">Authentication service.</param>
        /// <param name="navigationService">Navigation service.</param>
        /// <param name="logger">Logger instance.</param>
        public LoginViewModel(IAuthenticationService authService, INavigationService navigationService, ILogger logger)
        {
            this.authService = authService;
            this.navigationService = navigationService;
            this.logger = logger;
            this.Title = "Login";
        }

        /// <summary>
        /// Called when a property value changes.
        /// </summary>
        /// <param name="e">Property changed event args.</param>
        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(this.UsernameOrEmail) || e.PropertyName == nameof(this.Password))
            {
                this.LoginCommand.NotifyCanExecuteChanged();
                if (!string.IsNullOrEmpty(this.ErrorMessage))
                {
                    this.ErrorMessage = null;
                }
            }
        }

        /// <summary>
        /// Command to perform login.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            if (!this.ValidateInput())
            {
                return;
            }

            this.IsLoggingIn = true;
            this.ErrorMessage = null;

            try
            {
                var response = await this.authService.LoginAsync(
                    this.UsernameOrEmail,
                    this.Password,
                    this.RememberMe);

                this.logger.Information("Login successful");

                // ����������: ��������� � ChatView ����� ��������� ������
                this.navigationService.NavigateTo<ChatViewModel>();
            }
            catch (InvalidOperationException ex)
            {
                // Error message from AuthenticationService is already user-friendly
                var errorMsg = ex.Message;
                this.logger.Warning(ex, "Login failed with message: {Message}", errorMsg);
                this.ErrorMessage = errorMsg;
                this.logger.Debug("ErrorMessage set to: {ErrorMessage}", this.ErrorMessage);
            }
            catch (Exception ex)
            {
                var errorMsg = "An unexpected error occurred. Please check your connection and try again.";
                this.ErrorMessage = errorMsg;
                this.logger.Error(ex, "Unexpected error during login: {ExceptionType}", ex.GetType().Name);
            }
            finally
            {
                this.IsLoggingIn = false;
            }
        }

        /// <summary>
        /// Command to navigate to register view.
        /// </summary>
        [RelayCommand]
        private void NavigateToRegister()
        {
            this.navigationService.NavigateTo<RegisterViewModel>();
        }

        /// <summary>
        /// Command placeholder for Google login.
        /// </summary>
        [RelayCommand]
        private void LoginWithGoogle()
        {
            this.NotifySocialLoginNotSupported("Google");
        }

        /// <summary>
        /// Command placeholder for GitHub login.
        /// </summary>
        [RelayCommand]
        private void LoginWithGithub()
        {
            this.NotifySocialLoginNotSupported("GitHub");
        }

        /// <summary>
        /// Command placeholder for Facebook login.
        /// </summary>
        [RelayCommand]
        private void LoginWithFacebook()
        {
            this.NotifySocialLoginNotSupported("Facebook");
        }

        /// <summary>
        /// Command placeholder for Microsoft login.
        /// </summary>
        [RelayCommand]
        private void LoginWithMicrosoft()
        {
            this.NotifySocialLoginNotSupported("Microsoft");
        }

        private void NotifySocialLoginNotSupported(string providerName)
        {
            var message = $"{providerName} login is not yet supported.";
            this.ErrorMessage = null;
            this.ErrorMessage = message;
            this.logger.Information("{Provider} login requested but is not yet supported.", providerName);
        }

        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(this.UsernameOrEmail) &&
                   !string.IsNullOrWhiteSpace(this.Password) &&
                   !this.IsLoggingIn;
        }

        private bool ValidateInput()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(this.UsernameOrEmail))
            {
                errors.Add("Username or email is required.");
            }

            if (string.IsNullOrWhiteSpace(this.Password))
            {
                errors.Add("Password is required.");
            }

            if (errors.Count > 0)
            {
                this.ErrorMessage = string.Join(" ", errors);
                return false;
            }

            return true;
        }
    }
}
