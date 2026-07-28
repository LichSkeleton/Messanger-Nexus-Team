namespace NexusTeam.Client.Services
{
    using System;
    using Serilog;

    /// <summary>
    /// Implementation of global error handling service.
    /// </summary>
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHandlingService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public ErrorHandlingService(ILogger logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc/>
        public event EventHandler<ErrorNotificationEventArgs>? ErrorOccurred;

        /// <inheritdoc/>
        public void HandleError(Exception exception, string userMessage, bool showDialog = true)
        {
            this.logger.Error(exception, "Error occurred: {Message}", userMessage);

            if (showDialog)
            {
                this.OnErrorOccurred(new ErrorNotificationEventArgs(userMessage, ErrorSeverity.Error));
            }
        }

        /// <inheritdoc/>
        public void HandleError(Exception exception, bool showDialog = true)
        {
            var userMessage = this.GetUserFriendlyMessage(exception);
            this.HandleError(exception, userMessage, showDialog);
        }

        /// <inheritdoc/>
        public void ShowInfo(string message)
        {
            this.logger.Information(message);
            this.OnErrorOccurred(new ErrorNotificationEventArgs(message, ErrorSeverity.Info));
        }

        /// <inheritdoc/>
        public void ShowWarning(string message)
        {
            this.logger.Warning(message);
            this.OnErrorOccurred(new ErrorNotificationEventArgs(message, ErrorSeverity.Warning));
        }

        /// <inheritdoc/>
        public void ShowError(string message)
        {
            this.logger.Error(message);
            this.OnErrorOccurred(new ErrorNotificationEventArgs(message, ErrorSeverity.Error));
        }

        private void OnErrorOccurred(ErrorNotificationEventArgs args)
        {
            this.ErrorOccurred?.Invoke(this, args);
        }

        private string GetUserFriendlyMessage(Exception exception)
        {
            return exception switch
            {
                InvalidOperationException => "An operation could not be completed. Please try again.",
                UnauthorizedAccessException => "You do not have permission to perform this action.",
                TimeoutException => "The operation timed out. Please check your connection and try again.",
                System.Net.Http.HttpRequestException => "Network error occurred. Please check your connection.",
                System.Net.WebSockets.WebSocketException => "Connection error occurred. Attempting to reconnect...",
                _ => "An unexpected error occurred. Please try again.",
            };
        }
    }
}
