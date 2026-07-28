namespace NexusTeam.Server.Services.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Service for user authentication and registration.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user with transactional logic.
        /// </summary>
        /// <param name="request">The registration request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The registration response.</returns>
        Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Authenticates a user and issues JWT and refresh tokens.
        /// </summary>
        /// <param name="request">The login request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The login response with tokens.</returns>
        Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    }
}
