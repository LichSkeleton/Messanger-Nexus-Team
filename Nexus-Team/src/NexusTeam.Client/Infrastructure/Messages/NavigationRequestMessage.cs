namespace NexusTeam.Client.Infrastructure.Messages
{
    using System;

    /// <summary>
    /// Message requesting navigation to a specific view model.
    /// </summary>
    public class NavigationRequestMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationRequestMessage"/> class.
        /// </summary>
        /// <param name="viewModelType">The type of view model to navigate to.</param>
        public NavigationRequestMessage(Type viewModelType)
        {
            this.ViewModelType = viewModelType;
        }

        /// <summary>
        /// Gets the type of view model to navigate to.
        /// </summary>
        public Type ViewModelType { get; }
    }
}
