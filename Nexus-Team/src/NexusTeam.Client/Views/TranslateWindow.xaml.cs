namespace NexusTeam.Client.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using NexusTeam.Client.Services;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Interaction logic for TranslateWindow.xaml.
    /// </summary>
    public partial class TranslateWindow : UserControl
    {
        private readonly TranslateWindowViewModel viewModel;
        private Popup? parentPopup;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslateWindow"/> class.
        /// </summary>
        /// <param name="translationService">The translation service.</param>
        /// <param name="textToTranslate">The text to translate.</param>
        public TranslateWindow(ITranslationService translationService, string textToTranslate)
        {
            this.InitializeComponent();
            this.viewModel = new TranslateWindowViewModel(translationService, textToTranslate);
            this.DataContext = this.viewModel;
        }

        /// <summary>
        /// Sets the parent popup for this control.
        /// </summary>
        /// <param name="popup">The parent popup.</param>
        public void SetParentPopup(Popup popup)
        {
            this.parentPopup = popup;
        }
    }
}
