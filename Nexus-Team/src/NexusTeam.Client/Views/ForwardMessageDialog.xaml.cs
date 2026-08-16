namespace NexusTeam.Client.Views
{
    using System.Windows;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Dialog for choosing a chat to forward a message into.
    /// </summary>
    public partial class ForwardMessageDialog : Window
    {
        private readonly ForwardMessageDialogViewModel viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForwardMessageDialog"/> class.
        /// </summary>
        public ForwardMessageDialog()
        {
            this.InitializeComponent();
            this.viewModel = new ForwardMessageDialogViewModel();
            this.DataContext = this.viewModel;
        }

        /// <summary>
        /// Gets the dialog view model.
        /// </summary>
        public ForwardMessageDialogViewModel ViewModel => this.viewModel;

        private void OnForwardClick(object sender, RoutedEventArgs e)
        {
            if (this.viewModel.SelectedChat == null)
            {
                this.viewModel.ValidationError = "Select a chat";
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
