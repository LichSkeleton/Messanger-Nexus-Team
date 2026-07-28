namespace NexusTeam.Client.Views
{
    using System.Windows;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Interaction logic for CreateChatDialog.xaml.
    /// </summary>
    public partial class CreateChatDialog : Window
    {
        private readonly CreateChatDialogViewModel viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateChatDialog"/> class.
        /// </summary>
        public CreateChatDialog()
        {
            this.InitializeComponent();
            this.viewModel = new CreateChatDialogViewModel();
            this.DataContext = this.viewModel;
        }

        /// <summary>
        /// Gets the view model associated with this dialog.
        /// </summary>
        public CreateChatDialogViewModel ViewModel => this.viewModel;

        private void OnCreateClick(object sender, RoutedEventArgs e)
        {
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
