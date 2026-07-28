namespace NexusTeam.Server.Services.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for rate limiting operations.
    /// </summary>
    public interface IRateLimitService
    {
        /// <summary>
        /// Checks if a login attempt is allowed for the given identifier.
        /// </summary>
        /// <param name="identifier">The identifier (e.g., username or IP address).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the attempt is allowed; otherwise, false.</returns>
        Task<bool> IsLoginAllowedAsync(string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a message send is allowed for the given user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the send is allowed; otherwise, false.</returns>
        Task<bool> IsMessageSendAllowedAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the remaining time in seconds until the rate limit window resets for login.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The remaining time in seconds.</returns>
        Task<int> GetLoginRateLimitResetTimeAsync(string identifier, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the remaining time in seconds until the rate limit window resets for message sending.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The remaining time in seconds.</returns>
        Task<int> GetMessageRateLimitResetTimeAsync(string userId, CancellationToken cancellationToken = default);
    }
}
