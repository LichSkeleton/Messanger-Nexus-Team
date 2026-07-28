namespace NexusTeam.Client.Services
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Serilog;

    /// <summary>
    /// Service for translating text using DeepL API.
    /// </summary>
    public class TranslationService : ITranslationService
    {
        private const string ApiKey = "843e2039-2885-40c0-94fc-0d79872e5f42:fx";
        private const string ApiEndpoint = "https://api-free.deepl.com/v2/translate";

        private readonly HttpClient httpClient;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for making requests.</param>
        /// <param name="logger">The logger instance.</param>
        public TranslationService(HttpClient httpClient, ILogger logger)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Translates text to the target language.
        /// </summary>
        /// <param name="text">The text to translate.</param>
        /// <param name="targetLang">The target language code (e.g., "EN", "RU", "DE").</param>
        /// <returns>The translated text, or null if translation failed.</returns>
        public async Task<string?> TranslateAsync(string text, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(targetLang))
            {
                this.logger.Warning("Target language is empty");
                return null;
            }

            try
            {
                // Prepare form data
                var formData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("auth_key", ApiKey),
                    new KeyValuePair<string, string>("text", text),
                    new KeyValuePair<string, string>("target_lang", targetLang),
                };

                var content = new FormUrlEncodedContent(formData);

                // Make POST request
                var response = await this.httpClient.PostAsync(ApiEndpoint, content).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    this.logger.Error("DeepL API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var jsonDoc = JsonDocument.Parse(jsonResponse);

                // Parse response: {"translations": [{"detected_source_language": "EN", "text": "..."}]}
                if (jsonDoc.RootElement.TryGetProperty("translations", out var translations) &&
                    translations.ValueKind == System.Text.Json.JsonValueKind.Array &&
                    translations.GetArrayLength() > 0)
                {
                    var firstTranslation = translations[0];
                    if (firstTranslation.TryGetProperty("text", out var translatedText))
                    {
                        return translatedText.GetString();
                    }
                }

                this.logger.Warning("Unexpected response format from DeepL API: {Response}", jsonResponse);
                return null;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error translating text");
                return null;
            }
        }
    }
}
