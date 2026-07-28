namespace NexusTeam.Client.Services
{
    using System;

    /// <summary>
    /// Event arguments for error notifications.
    /// </summary>
    public class ErrorNotificationEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorNotificationEventArgs"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="severity">The severity of the error.</param>
        public ErrorNotificationEventArgs(string message, ErrorSeverity severity)
        {
            this.Message = message;
            this.Severity = severity;
        }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the severity of the error.
        /// </summary>
        public ErrorSeverity Severity { get; }
    }
}
