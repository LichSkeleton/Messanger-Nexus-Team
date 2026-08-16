namespace NexusTeam.Client.Views
{
    using System.Windows;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Dialog for adding members to a group.
    /// </summary>
    public partial class AddMembersDialog : Window
    {
        private readonly AddMembersDialogViewModel viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddMembersDialog"/> class.
        /// </summary>
        public AddMembersDialog()
        {
            this.InitializeComponent();
            this.viewModel = new AddMembersDialogViewModel();
            this.DataContext = this.viewModel;
        }

        /// <summary>
        /// Gets the dialog view model.
        /// </summary>
        public AddMembersDialogViewModel ViewModel => this.viewModel;

        private void OnAddClick(object sender, RoutedEventArgs e)
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
