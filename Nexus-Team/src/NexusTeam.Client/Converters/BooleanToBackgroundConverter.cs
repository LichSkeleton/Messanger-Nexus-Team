namespace NexusTeam.Client.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;

    /// <summary>
    /// Converts a boolean value to a background brush.
    /// </summary>
    public class BooleanToBackgroundConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean to a background brush.
        /// </summary>
        /// <param name="value">The boolean value.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">Optional parameter.</param>
        /// <param name="culture">The culture info.</param>
        /// <returns>A brush based on the boolean value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return new SolidColorBrush(Color.FromArgb(0x40, 0x4A, 0x90, 0xD9));
            }

            return new SolidColorBrush(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF));
        }

        /// <summary>
        /// Converts back (not implemented).
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture info.</param>
        /// <returns>Not implemented.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
