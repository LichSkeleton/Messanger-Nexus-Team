namespace NexusTeam.Client.Helpers
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Helper class for formatting dates for chat message separators.
    /// </summary>
    public static class DateFormatter
    {
        /// <summary>
        /// Formats a date for display in chat message separators.
        /// </summary>
        /// <param name="date">The date to format.</param>
        /// <returns>Formatted date string.</returns>
        public static string FormatDate(DateTime date)
        {
            var today = DateTime.Now.Date;
            var yesterday = today.AddDays(-1);
            var dateDate = date.Date;

            // Check if the date is today
            if (dateDate == today)
            {
                // Could be localized in the future - for now use English
                return "Today";
            }

            // Check if the date is yesterday
            if (dateDate == yesterday)
            {
                // Could be localized in the future - for now use English
                return "Yesterday";
            }

            // Check if the date is in the current year
            if (dateDate.Year == today.Year)
            {
                // Format: "24 November"
                return dateDate.ToString("d MMMM", CultureInfo.InvariantCulture);
            }

            // Format: "24 November 2024" for past years
            return dateDate.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Checks if two dates are on different days.
        /// </summary>
        /// <param name="date1">First date.</param>
        /// <param name="date2">Second date.</param>
        /// <returns>True if dates are on different days, false otherwise.</returns>
        public static bool AreDifferentDays(DateTime date1, DateTime date2)
        {
            return date1.Date != date2.Date;
        }

        /// <summary>
        /// Gets the date key for grouping messages by day.
        /// </summary>
        /// <param name="dateTime">The date time to get the key for.</param>
        /// <returns>Date key representing the day.</returns>
        public static DateTime GetDateKey(DateTime dateTime)
        {
            return dateTime.Date;
        }
    }
}
