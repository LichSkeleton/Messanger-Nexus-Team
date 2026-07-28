namespace NexusTeam.Client.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using NexusTeam.Client.Services;

    /// <summary>
    /// View model for the translation window.
    /// </summary>
    public partial class TranslateWindowViewModel : ViewModelBase
    {
        private readonly ITranslationService translationService;
        private string originalText;
        private string? translatedText;
        private string? selectedLanguageCode;
        private bool isTranslating;
        private string? errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslateWindowViewModel"/> class.
        /// </summary>
        /// <param name="translationService">The translation service.</param>
        /// <param name="textToTranslate">The text to translate.</param>
        public TranslateWindowViewModel(ITranslationService translationService, string textToTranslate)
        {
            this.translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
            this.originalText = textToTranslate ?? throw new ArgumentNullException(nameof(textToTranslate));

            // Initialize languages
            this.AvailableLanguages = new ObservableCollection<LanguageItem>
            {
                new LanguageItem("EN", "English"),
                new LanguageItem("DE", "German"),
                new LanguageItem("FR", "French"),
                new LanguageItem("ES", "Spanish"),
                new LanguageItem("IT", "Italian"),
                new LanguageItem("JA", "Japanese"),
                new LanguageItem("ZH", "Chinese"),
                new LanguageItem("PL", "Polish"),
                new LanguageItem("UK", "Ukrainian"),
                new LanguageItem("PT", "Portuguese"),
                new LanguageItem("TR", "Turkish"),
                new LanguageItem("AR", "Arabic"),
                new LanguageItem("KO", "Korean"),
            };

            // Set default language to English
            this.SelectedLanguageCode = "EN";
        }

        /// <summary>
        /// Gets the original text to translate.
        /// </summary>
        public string OriginalText
        {
            get => this.originalText;
            private set => this.SetProperty(ref this.originalText, value);
        }

        /// <summary>
        /// Gets or sets the translated text.
        /// </summary>
        public string? TranslatedText
        {
            get => this.translatedText;
            set => this.SetProperty(ref this.translatedText, value);
        }

        /// <summary>
        /// Gets or sets the selected language code.
        /// </summary>
        public string? SelectedLanguageCode
        {
            get => this.selectedLanguageCode;
            set => this.SetProperty(ref this.selectedLanguageCode, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether translation is in progress.
        /// </summary>
        public bool IsTranslating
        {
            get => this.isTranslating;
            set => this.SetProperty(ref this.isTranslating, value);
        }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string? ErrorMessage
        {
            get => this.errorMessage;
            set => this.SetProperty(ref this.errorMessage, value);
        }

        /// <summary>
        /// Gets the available languages for translation.
        /// </summary>
        public ObservableCollection<LanguageItem> AvailableLanguages { get; }

        /// <summary>
        /// Gets the command to translate text.
        /// </summary>
        [RelayCommand]
        private async Task TranslateAsync()
        {
            if (string.IsNullOrWhiteSpace(this.SelectedLanguageCode))
            {
                this.ErrorMessage = "Please select a target language";
                return;
            }

            if (string.IsNullOrWhiteSpace(this.OriginalText))
            {
                this.ErrorMessage = "No text to translate";
                return;
            }

            this.ErrorMessage = null;
            this.IsTranslating = true;
            this.TranslatedText = null;

            try
            {
                var result = await this.translationService.TranslateAsync(this.OriginalText, this.SelectedLanguageCode).ConfigureAwait(true);

                if (result == null)
                {
                    this.ErrorMessage = "Translation failed. Please try again.";
                }
                else
                {
                    this.TranslatedText = result;
                }
            }
            catch (Exception ex)
            {
                this.ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                this.IsTranslating = false;
            }
        }

        /// <summary>
        /// Represents a language item for the ComboBox.
        /// </summary>
        public class LanguageItem
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="LanguageItem"/> class.
            /// </summary>
            /// <param name="code">The language code.</param>
            /// <param name="name">The language name.</param>
            public LanguageItem(string code, string name)
            {
                this.Code = code;
                this.Name = name;
            }

            /// <summary>
            /// Gets the language code.
            /// </summary>
            public string Code { get; }

            /// <summary>
            /// Gets the language name.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Returns a string representation of the language item.
            /// </summary>
            /// <returns>The language name.</returns>
            public override string ToString() => this.Name;
        }
    }
}
