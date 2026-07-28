namespace NexusTeam.Client.Services
{
    using System.Threading.Tasks;

    /// <summary>
    /// Service for translating text using DeepL API.
    /// </summary>
    public interface ITranslationService
    {
        /// <summary>
        /// Translates text to the target language.
        /// </summary>
        /// <param name="text">The text to translate.</param>
        /// <param name="targetLang">The target language code (e.g., "EN", "RU", "DE").</param>
        /// <returns>The translated text, or null if translation failed.</returns>
        Task<string?> TranslateAsync(string text, string targetLang);
    }
}
