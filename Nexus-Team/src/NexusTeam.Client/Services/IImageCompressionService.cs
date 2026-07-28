namespace NexusTeam.Client.Services
{
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for compressing and resizing images.
    /// </summary>
    public interface IImageCompressionService
    {
        /// <summary>
        /// Compresses an image file to a JPEG stream with specified quality and max dimensions.
        /// </summary>
        /// <param name="filePath">The path to the image file.</param>
        /// <param name="quality">The JPEG quality level (0-100).</param>
        /// <param name="maxWidth">The maximum width in pixels.</param>
        /// <param name="maxHeight">The maximum height in pixels.</param>
        /// <returns>A stream containing the compressed image data.</returns>
        Task<Stream> CompressImageAsync(string filePath, int quality = 80, int maxWidth = 1280, int maxHeight = 1280);

        /// <summary>
        /// Generates a thumbnail for an image file.
        /// </summary>
        /// <param name="filePath">The path to the image file.</param>
        /// <param name="quality">The JPEG quality level (0-100).</param>
        /// <param name="maxWidth">The maximum width in pixels.</param>
        /// <param name="maxHeight">The maximum height in pixels.</param>
        /// <returns>A stream containing the thumbnail image data.</returns>
        Task<Stream> GenerateThumbnailAsync(string filePath, int quality = 55, int maxWidth = 200, int maxHeight = 200);
    }
}
