namespace NexusTeam.Client.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Interaction logic for GeneratorView.xaml.
    /// </summary>
    public partial class GeneratorView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratorView"/> class.
        /// </summary>
        public GeneratorView()
        {
            this.InitializeComponent();
        }

        private void DialogOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.DataContext is GeneratorViewModel viewModel)
            {
                viewModel.CloseSendToDialogCommand.Execute(null);
            }
        }

        private void ChatItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SelectableConversationViewModel chat)
            {
                chat.IsSelected = !chat.IsSelected;
            }
        }
    }
}
