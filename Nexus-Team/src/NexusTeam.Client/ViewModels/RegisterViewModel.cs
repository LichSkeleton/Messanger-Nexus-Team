namespace NexusTeam.Client.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using NexusTeam.Client.Services;
    using Serilog;

    /// <summary>
    /// View model for the registration view.
    /// </summary>
    public partial class RegisterViewModel : ViewModelBase
    {
        private readonly IAuthenticationService authService;
        private readonly INavigationService navigationService;
        private readonly ILogger logger;

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private string displayName = string.Empty;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private string? successMessage;

        [ObservableProperty]
        private bool isRegistering;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterViewModel"/> class.
        /// </summary>
        /// <param name="authService">Authentication service.</param>
        /// <param name="navigationService">Navigation service.</param>
        /// <param name="logger">Logger instance.</param>
        public RegisterViewModel(IAuthenticationService authService, INavigationService navigationService, ILogger logger)
        {
            this.authService = authService;
            this.navigationService = navigationService;
            this.logger = logger;
            this.Title = "Register";
        }

        /// <summary>
        /// Called when a property value changes.
        /// </summary>
        /// <param name="e">Property changed event args.</param>
        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(this.Username) ||
                e.PropertyName == nameof(this.Email) ||
                e.PropertyName == nameof(this.Password) ||
                e.PropertyName == nameof(this.ConfirmPassword) ||
                e.PropertyName == nameof(this.DisplayName))
            {
                this.RegisterCommand.NotifyCanExecuteChanged();
                if (!string.IsNullOrEmpty(this.ErrorMessage))
                {
                    this.ErrorMessage = null;
                }

                if (!string.IsNullOrEmpty(this.SuccessMessage))
                {
                    this.SuccessMessage = null;
                }
            }
        }

        /// <summary>
        /// Command to perform registration.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [RelayCommand(CanExecute = nameof(CanRegister))]
        private async Task RegisterAsync()
        {
            if (!this.ValidateInput())
            {
                return;
            }

            this.IsRegistering = true;
            this.ErrorMessage = null;
            this.SuccessMessage = null;

            try
            {
                var response = await this.authService.RegisterAsync(
                    this.Username,
                    this.Email,
                    this.Password,
                    this.DisplayName);

                if (response.Success)
                {
                    this.SuccessMessage = "Registration successful! You can now log in.";
                    this.logger.Information("Registration successful for user: {Username}", this.Username);

                    await Task.Delay(1500);
                    this.navigationService.NavigateTo<LoginViewModel>();
                }
                else
                {
                    var errorMessage = response.ErrorMessage ?? "Registration failed. Please check your input and try again.";
                    this.ErrorMessage = this.FormatErrorMessage(errorMessage);
                    this.logger.Warning("Registration failed: {Error}", errorMessage);
                }
            }
            catch (Exception ex)
            {
                this.ErrorMessage = "An unexpected error occurred. Please try again.";
                this.logger.Error(ex, "Unexpected error during registration");
            }
            finally
            {
                this.IsRegistering = false;
            }
        }

        /// <summary>
        /// Command to navigate to login view.
        /// </summary>
        [RelayCommand]
        private void NavigateToLogin()
        {
            this.navigationService.NavigateTo<LoginViewModel>();
        }

        private bool CanRegister()
        {
            return !string.IsNullOrWhiteSpace(this.Username) &&
                   !string.IsNullOrWhiteSpace(this.Email) &&
                   !string.IsNullOrWhiteSpace(this.Password) &&
                   !string.IsNullOrWhiteSpace(this.ConfirmPassword) &&
                   !string.IsNullOrWhiteSpace(this.DisplayName) &&
                   !this.IsRegistering;
        }

        private bool ValidateInput()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(this.Username))
            {
                errors.Add("Username is required.");
            }
            else if (this.Username.Length < 3)
            {
                errors.Add("Username must be at least 3 characters.");
            }

            if (string.IsNullOrWhiteSpace(this.Email))
            {
                errors.Add("Email is required.");
            }
            else if (!this.IsValidEmail(this.Email))
            {
                errors.Add("Invalid email format.");
            }

            if (string.IsNullOrWhiteSpace(this.Password))
            {
                errors.Add("Password is required.");
            }
            else
            {
                var passwordErrors = this.ValidatePassword(this.Password);
                errors.AddRange(passwordErrors);
            }

            if (string.IsNullOrWhiteSpace(this.ConfirmPassword))
            {
                errors.Add("Confirm password is required.");
            }
            else if (this.Password != this.ConfirmPassword)
            {
                errors.Add("Passwords do not match.");
            }

            if (string.IsNullOrWhiteSpace(this.DisplayName))
            {
                errors.Add("Display name is required.");
            }

            if (errors.Count > 0)
            {
                this.ErrorMessage = string.Join(" ", errors);
                return false;
            }

            return true;
        }

        private string FormatErrorMessage(string errorMessage)
        {
            // Format common error messages to be more user-friendly
            if (errorMessage.Contains("Username already exists", StringComparison.OrdinalIgnoreCase))
            {
                return "This username is already taken. Please choose a different one.";
            }

            if (errorMessage.Contains("Email already exists", StringComparison.OrdinalIgnoreCase))
            {
                return "This email is already registered. Please use a different email or try logging in.";
            }

            if (errorMessage.Contains("Username or email already exists", StringComparison.OrdinalIgnoreCase))
            {
                return "This username or email is already registered. Please use different credentials.";
            }

            return errorMessage;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private List<string> ValidatePassword(string password)
        {
            var errors = new List<string>();

            if (password.Length < 8)
            {
                errors.Add("Password must be at least 8 characters long.");
            }

            if (password.Length > 100)
            {
                errors.Add("Password must not exceed 100 characters.");
            }

            if (!Regex.IsMatch(password, "[A-Z]"))
            {
                errors.Add("Password must contain at least one uppercase letter.");
            }

            if (!Regex.IsMatch(password, "[a-z]"))
            {
                errors.Add("Password must contain at least one lowercase letter.");
            }

            if (!Regex.IsMatch(password, "[0-9]"))
            {
                errors.Add("Password must contain at least one digit.");
            }

            return errors;
        }
    }
}
