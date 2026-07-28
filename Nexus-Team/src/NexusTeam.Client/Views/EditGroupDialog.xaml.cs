namespace NexusTeam.Client.Views
{
    using System.Windows;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Dialog for editing group name and avatar.
    /// </summary>
    public partial class EditGroupDialog : Window
    {
        private readonly EditGroupDialogViewModel viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditGroupDialog"/> class.
        /// </summary>
        public EditGroupDialog()
        {
            this.InitializeComponent();
            this.viewModel = new EditGroupDialogViewModel();
            this.DataContext = this.viewModel;
        }

        /// <summary>
        /// Gets the dialog view model.
        /// </summary>
        public EditGroupDialogViewModel ViewModel => this.viewModel;

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            this.viewModel.ConfirmCommand.Execute(null);
            if (this.viewModel.DialogResult)
            {
                this.DialogResult = true;
                this.Close();
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.viewModel.CancelCommand.Execute(null);
            this.DialogResult = false;
            this.Close();
        }
    }
}
