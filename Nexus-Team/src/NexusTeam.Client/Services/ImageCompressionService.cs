namespace NexusTeam.Client.Services
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Serilog;

    /// <summary>
    /// Implementation of image compression service using WPF Imaging API.
    /// </summary>
    public class ImageCompressionService : IImageCompressionService
    {
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCompressionService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ImageCompressionService(ILogger logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task<Stream> CompressImageAsync(string filePath, int quality = 80, int maxWidth = 1280, int maxHeight = 1280)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var originalFileInfo = new FileInfo(filePath);
                    var originalSize = originalFileInfo.Length;

                    this.logger.Information(
                        "Compressing image: {FilePath}, Original size: {OriginalSize}",
                        filePath,
                        originalSize);

                    // Load the image
                    var originalImage = new BitmapImage();
                    originalImage.BeginInit();
                    originalImage.UriSource = new Uri(filePath);
                    originalImage.CacheOption = BitmapCacheOption.OnLoad;
                    originalImage.EndInit();
                    originalImage.Freeze();

                    // Calculate new dimensions - always resize if image is larger than max dimensions
                    double scale = 1.0;
                    bool needsResize = originalImage.PixelWidth > maxWidth || originalImage.PixelHeight > maxHeight;
                    if (needsResize)
                    {
                        double scaleX = (double)maxWidth / originalImage.PixelWidth;
                        double scaleY = (double)maxHeight / originalImage.PixelHeight;
                        scale = Math.Min(scaleX, scaleY);
                    }

                    // Resize if needed
                    BitmapSource processedImage = originalImage;
                    if (scale < 1.0)
                    {
                        var newWidth = (int)(originalImage.PixelWidth * scale);
                        var newHeight = (int)(originalImage.PixelHeight * scale);

                        this.logger.Debug(
                            "Resizing image from {OldWidth}x{OldHeight} to {NewWidth}x{NewHeight}",
                            originalImage.PixelWidth,
                            originalImage.PixelHeight,
                            newWidth,
                            newHeight);

                        processedImage = new TransformedBitmap(originalImage, new ScaleTransform(scale, scale));
                    }

                    // Try compression with specified quality
                    var encoder = new JpegBitmapEncoder();
                    encoder.QualityLevel = quality;
                    encoder.Frames.Add(BitmapFrame.Create(processedImage));

                    var memoryStream = new MemoryStream();
                    encoder.Save(memoryStream);
                    memoryStream.Position = 0;

                    var compressedSize = memoryStream.Length;
                    int currentQuality = quality;

                    // If compressed image is larger than original, try with lower quality
                    if (compressedSize >= originalSize && quality > 60)
                    {
                        this.logger.Debug(
                            "Compressed size ({CompressedSize}) >= original size ({OriginalSize}), trying lower quality",
                            compressedSize,
                            originalSize);

                        // Try with lower quality (reduce by 10%)
                        currentQuality = Math.Max(60, quality - 10);
                        encoder = new JpegBitmapEncoder();
                        encoder.QualityLevel = currentQuality;
                        encoder.Frames.Add(BitmapFrame.Create(processedImage));

                        memoryStream.Dispose();
                        memoryStream = new MemoryStream();
                        encoder.Save(memoryStream);
                        memoryStream.Position = 0;

                        compressedSize = memoryStream.Length;
                        this.logger.Debug("Retry with quality {Quality}, new size: {NewSize}", currentQuality, compressedSize);
                    }

                    // If still larger than original, try one more time with even lower quality
                    if (compressedSize >= originalSize && currentQuality > 50)
                    {
                        this.logger.Debug(
                            "Compressed size ({CompressedSize}) >= original size ({OriginalSize}), trying even lower quality",
                            compressedSize,
                            originalSize);

                        // Try with even lower quality (reduce by another 10%)
                        currentQuality = Math.Max(50, currentQuality - 10);
                        encoder = new JpegBitmapEncoder();
                        encoder.QualityLevel = currentQuality;
                        encoder.Frames.Add(BitmapFrame.Create(processedImage));

                        memoryStream.Dispose();
                        memoryStream = new MemoryStream();
                        encoder.Save(memoryStream);
                        memoryStream.Position = 0;

                        compressedSize = memoryStream.Length;
                        this.logger.Debug("Retry with quality {Quality}, new size: {NewSize}", currentQuality, compressedSize);
                    }

                    // If still larger than original, use original file
                    if (compressedSize >= originalSize)
                    {
                        this.logger.Information(
                            "Compressed image ({CompressedSize}) is larger than or equal to original ({OriginalSize}), using original file",
                            compressedSize,
                            originalSize);

                        memoryStream.Dispose();
                        throw new InvalidOperationException("Compression did not reduce file size");
                    }

                    // If compressed image is smaller (even by 1 byte), use it
                    // This matches Telegram behavior - any reduction is worth it
                    var reductionPercent = (double)(originalSize - compressedSize) / originalSize * 100;
                    this.logger.Debug(
                        "Compressed image is {ReductionPercent:F2}% smaller than original",
                        reductionPercent);

                    this.logger.Information(
                        "Image compressed successfully. Original size: {OriginalSize}, New size: {NewSize}, Reduction: {Reduction:P}",
                        originalSize,
                        compressedSize,
                        (double)(originalSize - compressedSize) / originalSize);

                    return (Stream)memoryStream;
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, "Failed to compress image: {FilePath}", filePath);
                    throw;
                }
            });
        }

        /// <inheritdoc/>
        public async Task<Stream> GenerateThumbnailAsync(string filePath, int quality = 55, int maxWidth = 200, int maxHeight = 200)
        {
            return await this.CompressImageAsync(filePath, quality, maxWidth, maxHeight);
        }
    }
}
