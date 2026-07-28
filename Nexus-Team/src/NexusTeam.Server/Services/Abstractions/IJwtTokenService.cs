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

        /// <summary>
        /// Validates a JWT token and returns the user ID if valid.
        /// </summary>
        /// <param name="token">The JWT token to validate.</param>
        /// <returns>The user ID if valid, null otherwise.</returns>
        Task<string?> ValidateTokenAsync(string token);
    }
}
