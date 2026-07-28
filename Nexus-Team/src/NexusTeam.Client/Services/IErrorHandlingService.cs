namespace NexusTeam.Client.Services
{
    using System;

    /// <summary>
    /// Service for handling errors globally with user feedback and logging.
    /// </summary>
    public interface IErrorHandlingService
    {
        /// <summary>
        /// Handles an error by logging it and optionally showing user feedback.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="userMessage">The message to show to the user.</param>
        /// <param name="showDialog">Whether to show a dialog to the user.</param>
        void HandleError(Exception exception, string userMessage, bool showDialog = true);

        /// <summary>
        /// Handles an error with a custom user message derived from the exception.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="showDialog">Whether to show a dialog to the user.</param>
        void HandleError(Exception exception, bool showDialog = true);

        /// <summary>
        /// Shows an information message to the user.
        /// </summary>
        /// <param name="message">The message to show.</param>
        void ShowInfo(string message);

        /// <summary>
        /// Shows a warning message to the user.
        /// </summary>
        /// <param name="message">The message to show.</param>
        void ShowWarning(string message);

        /// <summary>
        /// Shows an error message to the user.
        /// </summary>
        /// <param name="message">The error message to show.</param>
        void ShowError(string message);

        /// <summary>
        /// Occurs when an error notification should be displayed to the user.
        /// </summary>
        event EventHandler<ErrorNotificationEventArgs>? ErrorOccurred;
    }
}
