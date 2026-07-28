namespace NexusTeam.Client.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    /// <summary>
    /// Converter that converts boolean to string based on TrueValue and FalseValue.
    /// </summary>
    public class BooleanToStringConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the value to return when the input is true.
        /// </summary>
        public string TrueValue { get; set; } = "True";

        /// <summary>
        /// Gets or sets the value to return when the input is false.
        /// </summary>
        public string FalseValue { get; set; } = "False";

        /// <inheritdoc/>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? this.TrueValue : this.FalseValue;
            }

            return this.FalseValue;
        }

        /// <inheritdoc/>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return stringValue == this.TrueValue;
            }

            return false;
        }
    }
}
