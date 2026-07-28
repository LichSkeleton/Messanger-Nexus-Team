namespace NexusTeam.Client.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// Converts UserStatus enum to display text.
    /// </summary>
    public class UserStatusToTextConverter : IValueConverter
    {
        /// <summary>
        /// Converts UserStatus to display text.
        /// </summary>
        /// <param name="value">The UserStatus enum value.</param>
        /// <param name="targetType">The target type (string).</param>
        /// <param name="parameter">Converter parameter (not used).</param>
        /// <param name="culture">Culture info (not used).</param>
        /// <returns>A string representing the status: "Online", "Offline", "Away", "Do Not Disturb".</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UserStatus status)
            {
                return status switch
                {
                    UserStatus.Online => "Online",
                    UserStatus.Offline => "Offline",
                    UserStatus.Away => "Away",
                    UserStatus.DoNotDisturb => "Do Not Disturb",
                    UserStatus.Invisible => "Invisible",
                    _ => "Offline",
                };
            }

            return "Offline";
        }

        /// <summary>
        /// Not implemented for this converter. This is a one-way converter.
        /// </summary>
        /// <param name="value">The value to convert back (not used).</param>
        /// <param name="targetType">The target type for conversion (not used).</param>
        /// <param name="parameter">Optional converter parameter (not used).</param>
        /// <param name="culture">Culture information for conversion (not used).</param>
        /// <returns>Throws NotImplementedException.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
