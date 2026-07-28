namespace NexusTeam.Shared.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Exception thrown when input validation fails.
    /// Contains details about validation errors.
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        public ValidationException()
            : base("One or more validation errors occurred.")
        {
            this.Errors = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ValidationException(string message)
            : base(message)
        {
            this.Errors = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
            this.Errors = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class.
        /// </summary>
        /// <param name="errors">Dictionary of field names and their validation errors.</param>
        public ValidationException(IDictionary<string, string[]> errors)
            : base("One or more validation errors occurred.")
        {
            this.Errors = errors;
        }

        /// <summary>
        /// Gets the validation errors grouped by field name.
        /// </summary>
        public IDictionary<string, string[]> Errors { get; }
    }
}
