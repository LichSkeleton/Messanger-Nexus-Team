namespace NexusTeam.Client.Services
{
    using System;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Interface for navigation service.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Gets the current view model.
        /// </summary>
        ViewModelBase? CurrentViewModel { get; }

        /// <summary>
        /// Event raised when the current view model changes.
        /// </summary>
        event EventHandler<ViewModelBase>? CurrentViewModelChanged;

        /// <summary>
        /// Navigates to a view model of the specified type.
        /// </summary>
        /// <typeparam name="TViewModel">Type of view model to navigate to.</typeparam>
        void NavigateTo<TViewModel>()
            where TViewModel : ViewModelBase;

        /// <summary>
        /// Navigates back to the previous view model.
        /// </summary>
        void NavigateBack();

        /// <summary>
        /// Navigates back to the previous view model (alias for NavigateBack).
        /// </summary>
        void GoBack();

        /// <summary>
        /// Clears navigation history.
        /// </summary>
        void ClearHistory();
    }
}
