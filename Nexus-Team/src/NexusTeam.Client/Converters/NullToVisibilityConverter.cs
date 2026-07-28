namespace NexusTeam.Client.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Converts null or empty string values to Visibility values.
    /// Supports an optional parameter to invert the behavior.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value to a visibility.
        /// By default, null or empty values become Collapsed, non-null values become Visible.
        /// If the parameter is "Invert" or "True", the behavior is reversed.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter to invert behavior (use "Invert" or "True").</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>Visibility.Visible or Visibility.Collapsed.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isNullOrEmpty = value == null || (value is string stringValue && string.IsNullOrEmpty(stringValue));

            var shouldShow = ShouldInvert(parameter) ? isNullOrEmpty : !isNullOrEmpty;

            return shouldShow ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static bool ShouldInvert(object parameter)
        {
            if (parameter is bool boolParameter)
            {
                return boolParameter;
            }

            if (parameter is string stringParameter)
            {
                if (bool.TryParse(stringParameter, out var boolResult))
                {
                    return boolResult;
                }

                return string.Equals(stringParameter, "invert", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
