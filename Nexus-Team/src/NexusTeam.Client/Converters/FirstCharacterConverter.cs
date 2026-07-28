namespace NexusTeam.Client.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    /// <summary>
    /// Converts a string to its first character (uppercase).
    /// Returns empty string if input is null or empty.
    /// </summary>
    public class FirstCharacterConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var stringValue = value.ToString();
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return string.Empty;
            }

            // Get first character and convert to uppercase
            return stringValue.Trim()[0].ToString().ToUpperInvariant();
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
