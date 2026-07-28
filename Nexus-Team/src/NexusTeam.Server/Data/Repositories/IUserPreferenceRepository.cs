namespace NexusTeam.Server.Data.Repositories
{
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Models;

    /// <summary>
    /// Repository interface for user preference operations using MongoDB.
    /// </summary>
    public interface IUserPreferenceRepository
    {
        /// <summary>
        /// Gets user preferences by user ID.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user preferences if found, null otherwise.</returns>
        Task<UserPreference?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates new user preferences.
        /// </summary>
        /// <param name="preference">The user preference to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CreateAsync(UserPreference preference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates existing user preferences.
        /// </summary>
        /// <param name="preference">The user preference to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateAsync(UserPreference preference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes user preferences by user ID.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteAsync(string userId, CancellationToken cancellationToken = default);
    }
}
