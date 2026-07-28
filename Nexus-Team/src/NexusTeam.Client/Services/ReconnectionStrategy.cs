namespace NexusTeam.Client.Services
{
    using System;

    /// <summary>
    /// Provides exponential backoff strategy for reconnection attempts.
    /// </summary>
    public class ReconnectionStrategy
    {
        private readonly int maxRetries;
        private readonly TimeSpan initialDelay;
        private readonly TimeSpan maxDelay;
        private readonly double exponentialBase;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReconnectionStrategy"/> class.
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts.</param>
        /// <param name="initialDelay">Initial delay before first retry.</param>
        /// <param name="maxDelay">Maximum delay between retries.</param>
        /// <param name="exponentialBase">Base for exponential backoff calculation.</param>
        public ReconnectionStrategy(
            int maxRetries = 10,
            TimeSpan? initialDelay = null,
            TimeSpan? maxDelay = null,
            double exponentialBase = 2.0)
        {
            if (maxRetries < 0)
            {
                throw new ArgumentException("Max retries must be non-negative", nameof(maxRetries));
            }

            if (exponentialBase <= 1.0)
            {
                throw new ArgumentException("Exponential base must be greater than 1.0", nameof(exponentialBase));
            }

            this.maxRetries = maxRetries;
            this.initialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
            this.maxDelay = maxDelay ?? TimeSpan.FromSeconds(60);
            this.exponentialBase = exponentialBase;
        }

        /// <summary>
        /// Gets the maximum number of retry attempts.
        /// </summary>
        public int MaxRetries => this.maxRetries;

        /// <summary>
        /// Calculates the delay for a given retry attempt using exponential backoff.
        /// </summary>
        /// <param name="attemptNumber">The retry attempt number (0-based).</param>
        /// <returns>The delay to wait before the retry attempt.</returns>
        public TimeSpan GetDelay(int attemptNumber)
        {
            if (attemptNumber < 0)
            {
                throw new ArgumentException("Attempt number must be non-negative", nameof(attemptNumber));
            }

            if (attemptNumber >= this.maxRetries)
            {
                return TimeSpan.Zero;
            }

            var delaySeconds = this.initialDelay.TotalSeconds * Math.Pow(this.exponentialBase, attemptNumber);
            var delay = TimeSpan.FromSeconds(delaySeconds);

            return delay > this.maxDelay ? this.maxDelay : delay;
        }

        /// <summary>
        /// Determines whether another retry attempt should be made.
        /// </summary>
        /// <param name="attemptNumber">The retry attempt number (0-based).</param>
        /// <returns>True if another attempt should be made; otherwise, false.</returns>
        public bool ShouldRetry(int attemptNumber)
        {
            return attemptNumber < this.maxRetries;
        }
    }
}
