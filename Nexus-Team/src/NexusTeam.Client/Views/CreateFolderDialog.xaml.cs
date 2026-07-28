namespace NexusTeam.Client.Views
{
    using System.Windows;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Interaction logic for CreateFolderDialog.xaml.
    /// </summary>
    public partial class CreateFolderDialog : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateFolderDialog"/> class.
        /// </summary>
        public CreateFolderDialog()
        {
            this.InitializeComponent();
            this.ViewModel = new CreateFolderDialogViewModel();
            this.DataContext = this.ViewModel;
            this.ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CreateFolderDialogViewModel.IsEditMode))
                {
                    this.Title = this.ViewModel.IsEditMode ? "Edit Folder" : "Create New Folder";
                }
            };
        }

        /// <summary>
        /// Gets the view model for this dialog.
        /// </summary>
        public CreateFolderDialogViewModel ViewModel { get; }

        private void OnCreateClick(object sender, RoutedEventArgs e)
        {
            if (this.ViewModel.CanConfirm)
            {
                this.DialogResult = true;
                this.Close();
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
