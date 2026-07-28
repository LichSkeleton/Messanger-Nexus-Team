namespace NexusTeam.Client.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Converts boolean to HorizontalAlignment (true = Right, false = Left).
    /// </summary>
    public class BooleanToAlignmentConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }

            return HorizontalAlignment.Left;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
