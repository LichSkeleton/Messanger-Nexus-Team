namespace NexusTeam.Client.Services
{
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Service for managing user avatars.
    /// </summary>
    public interface IAvatarService
    {
        /// <summary>
        /// Gets the default avatar image.
        /// </summary>
        /// <returns>The default avatar bitmap.</returns>
        BitmapImage GetDefaultAvatar();

        /// <summary>
        /// Loads an avatar image from a URL or returns default if URL is null/empty.
        /// </summary>
        /// <param name="avatarUrl">The avatar URL.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The avatar bitmap image.</returns>
        Task<BitmapImage> LoadAvatarAsync(string? avatarUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a new avatar image.
        /// </summary>
        /// <param name="filePath">Path to the image file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated user DTO with new avatar URL.</returns>
        Task<NexusTeam.Shared.Dtos.UserDto> UploadAvatarAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
