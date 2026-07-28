namespace NexusTeam.Client.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// Converts UserStatus enum to Brush color for status indicator.
    /// Online is green; Offline/Invisible are gray.
    /// </summary>
    public class UserStatusToBrushConverter : IValueConverter
    {
        /// <summary>
        /// Converts UserStatus to a Brush color.
        /// </summary>
        /// <param name="value">The UserStatus enum value.</param>
        /// <param name="targetType">The target type (Brush).</param>
        /// <param name="parameter">Converter parameter (not used).</param>
        /// <param name="culture">Culture info (not used).</param>
        /// <returns>A Brush representing the status color.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UserStatus status)
            {
                return status switch
                {
                    UserStatus.Online => new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)), // Green #22C55E
                    UserStatus.Offline => new SolidColorBrush(Color.FromArgb(255, 156, 163, 175)), // Light gray #9CA3AF
                    UserStatus.Invisible => new SolidColorBrush(Color.FromArgb(255, 156, 163, 175)), // Light gray (appears offline)
                    UserStatus.Away => new SolidColorBrush(Color.FromArgb(255, 250, 204, 21)), // Bright yellow #FACC15
                    UserStatus.DoNotDisturb => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)), // Red #EF4444
                    _ => new SolidColorBrush(Color.FromArgb(255, 156, 163, 175)),
                };
            }

            return new SolidColorBrush(Color.FromArgb(255, 128, 128, 128));
        }

        /// <summary>
        /// Not implemented for this converter.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
