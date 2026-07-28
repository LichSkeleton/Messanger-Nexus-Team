namespace NexusTeam.Server.Services.Abstractions
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for managing user avatars.
    /// </summary>
    public interface IAvatarService
    {
        /// <summary>
        /// Saves an avatar image for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="fileName">The original file name.</param>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The relative URL path to the saved avatar.</returns>
        Task<string> SaveAvatarAsync(
            string userId,
            string fileName,
            Stream fileStream,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the avatar stream for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The avatar file stream, or null if not found.</returns>
        Task<Stream?> GetAvatarStreamAsync(
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the default avatar stream.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The default avatar file stream.</returns>
        Task<Stream> GetDefaultAvatarStreamAsync(CancellationToken cancellationToken = default);
    }
}
