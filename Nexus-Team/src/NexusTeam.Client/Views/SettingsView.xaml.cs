namespace NexusTeam.Client.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Interaction logic for SettingsView.xaml.
    /// </summary>
    public partial class SettingsView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsView"/> class.
        /// </summary>
        public SettingsView()
        {
            this.InitializeComponent();
        }

        private void AvatarEllipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.DataContext is SettingsViewModel viewModel)
            {
                viewModel.ViewAvatarCommand.Execute(null);
            }
        }

        private void EditAvatarButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // Prevent triggering the avatar click
            if (this.DataContext is SettingsViewModel viewModel)
            {
                viewModel.ChangeAvatarCommand.Execute(null);
            }
        }
    }
}
