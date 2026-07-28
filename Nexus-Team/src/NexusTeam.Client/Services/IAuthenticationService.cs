namespace NexusTeam.Client.Services
{
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Service for managing user authentication and JWT tokens.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Gets the current authenticated user.
        /// </summary>
        UserDto? CurrentUser { get; }

        /// <summary>
        /// Gets a value indicating whether the user is authenticated.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Gets the current access token.
        /// </summary>
        string? AccessToken { get; }

        /// <summary>
        /// Authenticates a user with username/email and password.
        /// </summary>
        /// <param name="usernameOrEmail">Username or email.</param>
        /// <param name="password">Password.</param>
        /// <param name="rememberMe">Whether to save credentials for auto-login.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Login response with tokens and user info.</returns>
        Task<LoginResponse> LoginAsync(string usernameOrEmail, string password, bool rememberMe, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="email">Email address.</param>
        /// <param name="password">Password.</param>
        /// <param name="displayName">Display name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Registration response.</returns>
        Task<RegisterResponse> RegisterAsync(string username, string email, string password, string displayName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logs out the current user and clears stored credentials.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task LogoutAsync();

        /// <summary>
        /// Attempts to restore a previous session from stored credentials.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation. True if session was restored successfully.</returns>
        Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes the access token using the refresh token.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation. True if token was refreshed successfully.</returns>
        Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the current user's profile (display name).
        /// </summary>
        /// <param name="displayName">New display name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Updated user info.</returns>
        Task<UserDto> UpdateProfileAsync(string displayName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the current user's avatar URL locally.
        /// </summary>
        /// <param name="avatarUrl">The new avatar URL.</param>
        void UpdateCurrentUserAvatar(string? avatarUrl);
    }
}
