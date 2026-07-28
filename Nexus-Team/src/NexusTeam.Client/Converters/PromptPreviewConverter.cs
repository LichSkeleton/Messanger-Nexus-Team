namespace NexusTeam.Client.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    /// <summary>
    /// Converts a prompt string to a preview showing first words.
    /// </summary>
    public class PromptPreviewConverter : IValueConverter
    {
        private const int MaxWords = 5;
        private const int MaxLength = 60;

        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var prompt = value as string;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return string.Empty;
            }

            // If prompt is short enough, return as is
            if (prompt.Length <= MaxLength)
            {
                return prompt;
            }

            // Take first N words
            var words = prompt.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var wordCount = Math.Min(words.Length, MaxWords);
            var preview = string.Empty;

            for (int i = 0; i < wordCount; i++)
            {
                if (i > 0)
                {
                    preview += " ";
                }

                preview += words[i];
            }

            // If still too long, truncate
            if (preview.Length > MaxLength)
            {
                preview = preview.Substring(0, MaxLength).TrimEnd();
            }

            return preview + "...";
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
