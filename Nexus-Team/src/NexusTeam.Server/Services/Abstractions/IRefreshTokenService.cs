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

        /// <summary>Generates a refresh token bound to a device.</summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated token.</returns>
        Task<string> GenerateRefreshTokenAsync(string userId, string deviceId, CancellationToken cancellationToken = default)
            => this.GenerateRefreshTokenAsync(userId, cancellationToken);

        /// <summary>
        /// Validates a refresh token and returns the associated user ID.
        /// </summary>
        /// <param name="refreshToken">The refresh token to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user ID if valid, null otherwise.</returns>
        Task<string?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>Validates and returns a device-bound refresh identity.</summary>
        /// <param name="refreshToken">Refresh token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The bound identity, or null.</returns>
        async Task<RefreshTokenIdentity?> ValidateRefreshTokenIdentityAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var userId = await this.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
            return userId == null ? null : new RefreshTokenIdentity(userId, string.Empty);
        }

        /// <summary>
        /// Revokes a refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token to revoke.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    }
}
