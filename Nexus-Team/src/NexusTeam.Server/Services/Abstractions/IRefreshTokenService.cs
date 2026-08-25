namespace NexusTeam.Server.Services.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for managing refresh tokens in Redis.
    /// </summary>
    public interface IRefreshTokenService
    {
        /// <summary>
        /// Generates a new refresh token for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated refresh token.</returns>
        Task<string> GenerateRefreshTokenAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a refresh token and returns the associated user ID.
        /// </summary>
        /// <param name="refreshToken">The refresh token to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user ID if valid, null otherwise.</returns>
        Task<string?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes a refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token to revoke.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    }
}
