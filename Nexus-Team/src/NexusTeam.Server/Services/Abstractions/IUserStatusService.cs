namespace NexusTeam.Server.Services.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// Service for managing user online/offline status in Redis.
    /// </summary>
    public interface IUserStatusService
    {
        /// <summary>
        /// Gets the current status of a user (including Invisible for the user's own view).
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user status, or Offline if not found.</returns>
        Task<UserStatus> GetStatusAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the status that should be shown to other users.
        /// Invisible is reported as Offline.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The public status.</returns>
        Task<UserStatus> GetPublicStatusAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the status of a user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="status">The new status.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SetStatusAsync(string userId, UserStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets whether the user prefers Invisible mode across reconnects.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if invisible preference is enabled.</returns>
        Task<bool> GetInvisiblePreferenceAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets whether the user prefers Invisible mode across reconnects.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="isInvisible">True to appear offline while connected.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SetInvisiblePreferenceAsync(string userId, bool isInvisible, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes the status of a user (sets to offline).
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveStatusAsync(string userId, CancellationToken cancellationToken = default);
    }
}
