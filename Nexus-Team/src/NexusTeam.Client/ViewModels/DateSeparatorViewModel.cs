namespace NexusTeam.Client.ViewModels
{
    using System;
    using CommunityToolkit.Mvvm.ComponentModel;
    using NexusTeam.Client.Helpers;

    /// <summary>
    /// View model for a date separator in the chat message list.
    /// </summary>
    public class DateSeparatorViewModel : ObservableObject, IMessageListItem
    {
        private DateTime date;
        private string displayText;

        /// <summary>
        /// Initializes a new instance of the <see cref="DateSeparatorViewModel"/> class.
        /// </summary>
        /// <param name="date">The date to display.</param>
        public DateSeparatorViewModel(DateTime date)
        {
            this.date = date;
            this.displayText = DateFormatter.FormatDate(date);
        }

        /// <summary>
        /// Gets or sets the date represented by this separator.
        /// </summary>
        public DateTime Date
        {
            get => this.date;
            set
            {
                if (this.SetProperty(ref this.date, value))
                {
                    this.DisplayText = DateFormatter.FormatDate(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the formatted display text for the date.
        /// </summary>
        public string DisplayText
        {
            get => this.displayText;
            set => this.SetProperty(ref this.displayText, value);
        }
    }
}
