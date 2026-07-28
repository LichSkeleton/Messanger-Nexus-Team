namespace NexusTeam.Client.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Interaction logic for LoginView.xaml.
    /// </summary>
    public partial class LoginView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoginView"/> class.
        /// </summary>
        public LoginView()
        {
            this.InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}
