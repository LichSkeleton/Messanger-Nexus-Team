namespace NexusTeam.Server.Services.Abstractions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for Redis cache operations.
    /// </summary>
    public interface IRedisCacheService
    {
        /// <summary>
        /// Gets a value from cache.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Cached value or null.</returns>
        Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a value in cache.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="value">Value to cache.</param>
        /// <param name="expiry">Expiration time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SetAsync(string key, string value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a value from cache.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the key was removed, false otherwise.</returns>
        Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a key exists in cache.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if key exists, false otherwise.</returns>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Increments a numeric value in cache.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="value">Value to increment by.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The new value after incrementing.</returns>
        Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default);

        /// <summary>
        /// Decrements a numeric value in cache.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="value">Value to decrement by.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The new value after decrementing.</returns>
        Task<long> DecrementAsync(string key, long value = 1, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all keys matching a pattern.
        /// </summary>
        /// <param name="pattern">Pattern to match (supports * wildcard).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of matching keys.</returns>
        Task<List<string>> GetKeysAsync(string pattern, CancellationToken cancellationToken = default);
    }
}
