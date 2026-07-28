namespace NexusTeam.Client.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using NexusTeam.Client.ViewModels;

    public partial class RegisterView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterView"/> class.
        /// </summary>
        public RegisterView()
        {
            this.InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is RegisterViewModel viewModel)
            {
                viewModel.Password = ((PasswordBox)sender).Password;
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is RegisterViewModel viewModel)
            {
                viewModel.ConfirmPassword = ((PasswordBox)sender).Password;
            }
        }
    }
}
