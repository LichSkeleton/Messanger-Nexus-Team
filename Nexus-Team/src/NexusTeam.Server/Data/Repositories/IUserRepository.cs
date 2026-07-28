namespace NexusTeam.Server.Data.Repositories
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Models;

    /// <summary>
    /// Repository interface for user data operations.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Gets a user by their unique identifier.
        /// </summary>
        /// <param name="id">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user if found, null otherwise.</returns>
        Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a user by their username.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user if found, null otherwise.</returns>
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a user by their email address.
        /// </summary>
        /// <param name="email">The email address.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user if found, null otherwise.</returns>
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all users.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of all users.</returns>
        Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new user.
        /// </summary>
        /// <param name="user">The user to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing user.
        /// </summary>
        /// <param name="user">The user to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a user by their identifier.
        /// </summary>
        /// <param name="id">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    }
}
