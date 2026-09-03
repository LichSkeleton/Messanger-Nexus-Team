namespace NexusTeam.Client.Selectors
{
    using System.Windows;
    using System.Windows.Controls;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Template selector for message list items (messages or date separators).
    /// </summary>
    public class MessageItemTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Gets or sets the template for date separators.
        /// </summary>
        public DataTemplate? DateSeparatorTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template for centered system messages.
        /// </summary>
        public DataTemplate? SystemMessageTemplate { get; set; }

        /// <summary>
        /// Gets or sets the template for messages.
        /// </summary>
        public DataTemplate? MessageTemplate { get; set; }

        /// <inheritdoc/>
        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is DateSeparatorViewModel)
            {
                return this.DateSeparatorTemplate;
            }

            if (item is MessageViewModel message)
            {
                if (message.IsSystem)
                {
                    return this.SystemMessageTemplate;
                }

                return this.MessageTemplate;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
