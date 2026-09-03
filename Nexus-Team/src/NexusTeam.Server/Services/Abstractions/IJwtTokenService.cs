namespace NexusTeam.Server.Services.Abstractions
{
    using System.Threading.Tasks;
    using NexusTeam.Shared.Models;

    /// <summary>
    /// Service for generating and validating JWT tokens.
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// Generates a JWT access token for a user.
        /// </summary>
        /// <param name="user">The user to generate a token for.</param>
        /// <returns>The generated JWT token.</returns>
        Task<string> GenerateAccessTokenAsync(User user);

        /// <summary>Generates an access token bound to a browser device.</summary>
        /// <param name="user">Authenticated user.</param>
        /// <param name="deviceId">Stable browser device identifier.</param>
        /// <returns>The signed access token.</returns>
        Task<string> GenerateAccessTokenAsync(User user, string deviceId) => this.GenerateAccessTokenAsync(user);

        /// <summary>
        /// Validates a JWT token and returns the user ID if valid.
        /// </summary>
        /// <param name="token">The JWT token to validate.</param>
        /// <returns>The user ID if valid, null otherwise.</returns>
        Task<string?> ValidateTokenAsync(string token);

        /// <summary>Validates a token and returns its user/device identity.</summary>
        /// <param name="token">Access token.</param>
        /// <returns>The authenticated identity, or null.</returns>
        async Task<AuthenticatedIdentity?> ValidateIdentityAsync(string token)
        {
            var userId = await this.ValidateTokenAsync(token);
            return userId == null ? null : new AuthenticatedIdentity(userId, null);
        }
    }
}
