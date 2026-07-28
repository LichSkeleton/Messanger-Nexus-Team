namespace NexusTeam.Client.Services
{
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Service for securely storing and retrieving user credentials and tokens.
    /// Uses DPAPI (Data Protection API) for encryption.
    /// </summary>
    public interface ICredentialStorageService
    {
        /// <summary>
        /// Saves user credentials securely.
        /// </summary>
        /// <param name="usernameOrEmail">Username or email.</param>
        /// <param name="password">Password to encrypt.</param>
        void SaveCredentials(string usernameOrEmail, string password);

        /// <summary>
        /// Retrieves saved credentials if they exist.
        /// </summary>
        /// <returns>Tuple containing username/email and password, or null if no credentials are saved.</returns>
        (string UsernameOrEmail, string Password)? GetSavedCredentials();

        /// <summary>
        /// Saves authentication tokens and user info.
        /// </summary>
        /// <param name="accessToken">JWT access token.</param>
        /// <param name="refreshToken">Refresh token.</param>
        /// <param name="user">User information.</param>
        void SaveTokens(string accessToken, string? refreshToken, UserDto user);

        /// <summary>
        /// Retrieves saved tokens if they exist.
        /// </summary>
        /// <returns>Tuple containing tokens and user, or null if no tokens are saved.</returns>
        (string AccessToken, string? RefreshToken, UserDto User)? GetSavedTokens();

        /// <summary>
        /// Clears all stored credentials and tokens.
        /// </summary>
        void ClearAll();

        /// <summary>
        /// Clears only the stored credentials (not tokens).
        /// </summary>
        void ClearCredentials();

        /// <summary>
        /// Checks if credentials are currently saved.
        /// </summary>
        /// <returns>True if credentials are saved.</returns>
        bool HasSavedCredentials();
    }
}
