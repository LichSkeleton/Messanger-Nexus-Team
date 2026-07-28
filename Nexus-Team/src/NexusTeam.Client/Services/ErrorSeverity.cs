namespace NexusTeam.Client.Services
{
    /// <summary>
    /// Represents the severity level of an error.
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// Informational message.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Warning message.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Error message.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Critical error message.
        /// </summary>
        Critical = 3,
    }
}
