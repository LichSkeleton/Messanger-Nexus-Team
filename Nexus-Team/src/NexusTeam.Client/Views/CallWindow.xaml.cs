namespace NexusTeam.Client.Views
{
    using System.Windows;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Interaction logic for CallWindow.xaml.
    /// </summary>
    public partial class CallWindow : Window
    {
        private readonly CallViewModel viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallWindow"/> class.
        /// </summary>
        /// <param name="viewModel">The call view model.</param>
        public CallWindow(CallViewModel viewModel)
        {
            this.InitializeComponent();
            this.viewModel = viewModel;
            this.DataContext = this.viewModel;

            // Subscribe to call ended event to close window
            this.viewModel.CallStateChanged += this.OnCallStateChanged;
        }

        /// <summary>
        /// Handles window closing.
        /// </summary>
        /// <param name="e">Cancel event args.</param>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (this.viewModel != null)
            {
                this.viewModel.CallStateChanged -= this.OnCallStateChanged;
            }

            base.OnClosing(e);
        }

        private void OnCallStateChanged(object? sender, Services.CallStateChangedEventArgs e)
        {
            // Close window when call ends
            if (e.State == Services.CallState.Idle || e.State == Services.CallState.Ending)
            {
                Serilog.Log.Information("CallWindow: Closing window due to call state: {State}, CallId: {CallId}", e.State, e.CallId);
                this.Dispatcher.Invoke(() =>
                {
                    if (this.IsLoaded)
                    {
                        this.Close();
                    }
                });
            }
        }
    }
}
