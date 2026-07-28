namespace NexusTeam.Client.Services
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Helpers;
    using Serilog;

    /// <summary>
    /// Implementation of avatar service for managing user avatars.
    /// </summary>
    public class AvatarService : IAvatarService
    {
        private readonly HttpClient httpClient;
        private readonly IImageCompressionService imageCompressionService;
        private readonly ILogger logger;
        private readonly string serverBaseUrl;
        private BitmapImage? defaultAvatar;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvatarService"/> class.
        /// </summary>
        /// <param name="httpClient">HTTP client for API requests.</param>
        /// <param name="imageCompressionService">Image compression service.</param>
        /// <param name="logger">Logger instance.</param>
        public AvatarService(
            HttpClient httpClient,
            IImageCompressionService imageCompressionService,
            ILogger logger)
        {
            this.httpClient = httpClient;
            this.imageCompressionService = imageCompressionService;
            this.logger = logger;

            // Extract base URL from HttpClient BaseAddress
            this.serverBaseUrl = httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost:5251";
        }

        /// <inheritdoc/>
        public BitmapImage GetDefaultAvatar()
        {
            if (this.defaultAvatar != null)
            {
                return this.defaultAvatar;
            }

            try
            {
                // Load default avatar from Views/ava.png
                var defaultAvatarPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Views",
                    "ava.png");

                // If not found in BaseDirectory, try relative to executable
                if (!File.Exists(defaultAvatarPath))
                {
                    var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                    defaultAvatarPath = Path.Combine(assemblyDirectory ?? ".", "Views", "ava.png");
                }

                // If still not found, try pack URI or embedded resource
                if (!File.Exists(defaultAvatarPath))
                {
                    try
                    {
                        // Try pack URI first
                        this.defaultAvatar = new BitmapImage(new Uri("pack://application:,,,/Views/ava.png"));
                        this.defaultAvatar.Freeze();
                        this.logger.Debug("Default avatar loaded from pack URI");
                        return this.defaultAvatar;
                    }
                    catch
                    {
                        // If pack URI fails, try embedded resource
                        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                        var resourceName = "NexusTeam.Client.Views.ava.png";
                        var stream = assembly.GetManifestResourceStream(resourceName);

                        if (stream != null)
                        {
                            this.defaultAvatar = new BitmapImage();
                            this.defaultAvatar.BeginInit();
                            this.defaultAvatar.StreamSource = stream;
                            this.defaultAvatar.CacheOption = BitmapCacheOption.OnLoad;
                            this.defaultAvatar.EndInit();
                            this.defaultAvatar.Freeze();
                            this.logger.Debug("Default avatar loaded from embedded resource");
                            return this.defaultAvatar;
                        }

                        // If both fail, throw to trigger fallback
                        throw;
                    }
                }

                this.defaultAvatar = new BitmapImage();
                this.defaultAvatar.BeginInit();
                this.defaultAvatar.UriSource = new Uri(defaultAvatarPath);
                this.defaultAvatar.CacheOption = BitmapCacheOption.OnLoad;
                this.defaultAvatar.EndInit();
                this.defaultAvatar.Freeze();

                this.logger.Debug("Default avatar loaded from: {Path}", defaultAvatarPath);
                return this.defaultAvatar;
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to load default avatar, creating placeholder");

                // Create a simple placeholder programmatically
                try
                {
                    var renderTarget = new RenderTargetBitmap(200, 200, 96, 96, PixelFormats.Pbgra32);
                    var drawingVisual = new DrawingVisual();

                    using (var drawingContext = drawingVisual.RenderOpen())
                    {
                        // Draw a circle with a solid color
                        var brush = new SolidColorBrush(Color.FromRgb(0x33, 0x35, 0x4E));
                        drawingContext.DrawEllipse(brush, null, new System.Windows.Point(100, 100), 100, 100);

                        // Draw a question mark in the center
                        var text = new FormattedText(
                            "?",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Windows.FlowDirection.LeftToRight,
                            new Typeface("Segoe UI"),
                            80,
                            Brushes.White,
                            96);

                        var textX = (200 - text.Width) / 2;
                        var textY = (200 - text.Height) / 2;
                        drawingContext.DrawText(text, new System.Windows.Point(textX, textY));
                    }

                    renderTarget.Render(drawingVisual);
                    renderTarget.Freeze();

                    // Convert RenderTargetBitmap to BitmapImage
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                    using (var memoryStream = new MemoryStream())
                    {
                        encoder.Save(memoryStream);
                        memoryStream.Position = 0;

                        this.defaultAvatar = new BitmapImage();
                        this.defaultAvatar.BeginInit();
                        this.defaultAvatar.StreamSource = memoryStream;
                        this.defaultAvatar.CacheOption = BitmapCacheOption.OnLoad;
                        this.defaultAvatar.EndInit();
                        this.defaultAvatar.Freeze();
                    }

                    this.logger.Debug("Created programmatic default avatar placeholder");
                    return this.defaultAvatar;
                }
                catch (Exception fallbackEx)
                {
                    this.logger.Error(fallbackEx, "Failed to create programmatic placeholder, returning minimal bitmap");

                    // Return a minimal valid BitmapImage - create a 1x1 pixel image
                    var minimalBitmap = new RenderTargetBitmap(1, 1, 96, 96, PixelFormats.Pbgra32);
                    minimalBitmap.Freeze();

                    // Convert RenderTargetBitmap to BitmapImage
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(minimalBitmap));

                    using (var memoryStream = new MemoryStream())
                    {
                        encoder.Save(memoryStream);
                        memoryStream.Position = 0;

                        this.defaultAvatar = new BitmapImage();
                        this.defaultAvatar.BeginInit();
                        this.defaultAvatar.StreamSource = memoryStream;
                        this.defaultAvatar.CacheOption = BitmapCacheOption.OnLoad;
                        this.defaultAvatar.EndInit();
                        this.defaultAvatar.Freeze();
                    }

                    return this.defaultAvatar;
                }
            }
        }

        /// <inheritdoc/>
        public Task<BitmapImage> LoadAvatarAsync(string? avatarUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                return Task.FromResult(this.GetDefaultAvatar());
            }

            try
            {
                // If it's a local file path, load directly
                if (File.Exists(avatarUrl))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(Path.GetFullPath(avatarUrl));
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return Task.FromResult(bitmap);
                }

                // If it's a URL, load from server
                string fullUrl;
                if (avatarUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    fullUrl = avatarUrl;
                }
                else
                {
                    // Handle relative URLs - ensure proper concatenation
                    var baseUrl = this.serverBaseUrl.TrimEnd('/');
                    var relativeUrl = avatarUrl.TrimStart('/');
                    fullUrl = $"{baseUrl}/{relativeUrl}";
                }

                this.logger.Debug("Loading avatar from URL: {Url}", fullUrl);

                var bitmapImage = new BitmapImage();
                var tcs = new TaskCompletionSource<BitmapImage>();

                bitmapImage.DownloadCompleted += (s, e) =>
                {
                    try
                    {
                        bitmapImage.Freeze();
                        this.logger.Debug("Avatar download completed and frozen from URL: {Url}", fullUrl);
                        tcs.SetResult(bitmapImage);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Warning(ex, "Failed to freeze avatar image after download");
                        tcs.SetException(ex);
                    }
                };

                bitmapImage.DownloadFailed += (s, e) =>
                {
                    this.logger.Warning("Avatar download failed from URL: {Url}, Error: {Error}", fullUrl, e.ErrorException?.Message);
                    tcs.SetException(e.ErrorException ?? new InvalidOperationException("Avatar download failed"));
                };

                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(fullUrl);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.DecodePixelWidth = 200; // Optimize for display
                bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // Bypass cache to ensure fresh image
                bitmapImage.EndInit();

                // If image is already loaded (cached), freeze it immediately
                if (!bitmapImage.IsDownloading)
                {
                    bitmapImage.Freeze();
                    this.logger.Debug("Avatar already loaded (cached) from URL: {Url}", fullUrl);
                    return Task.FromResult(bitmapImage);
                }

                // Wait for download to complete
                return tcs.Task;
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to load avatar from URL: {Url}, using default", avatarUrl);
                return Task.FromResult(this.GetDefaultAvatar());
            }
        }

        /// <inheritdoc/>
        public async Task<NexusTeam.Shared.Dtos.UserDto> UploadAvatarAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("Avatar file not found", filePath);
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                var maxSizeBytes = 5 * 1024 * 1024; // 5MB

                Stream fileStream;
                bool shouldCompress = fileInfo.Length > maxSizeBytes;

                if (shouldCompress)
                {
                    this.logger.Information(
                        "Avatar file size ({Size}) exceeds {MaxSize}, compressing...",
                        FileHelper.FormatFileSize(fileInfo.Length),
                        FileHelper.FormatFileSize(maxSizeBytes));

                    // Compress image - use quality 85 and max dimensions 1024x1024 for avatars
                    fileStream = await this.imageCompressionService.CompressImageAsync(
                        filePath,
                        quality: 85,
                        maxWidth: 1024,
                        maxHeight: 1024);
                }
                else
                {
                    fileStream = File.OpenRead(filePath);
                }

                try
                {
                    using var content = new MultipartFormDataContent();
                    using var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                    content.Add(streamContent, "file", Path.GetFileName(filePath));

                    var response = await this.httpClient.PostAsync("/api/users/avatar/upload", content, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        // Deserialize as UserDto
                        var userDto = await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken);
                        if (userDto == null)
                        {
                            throw new InvalidOperationException("Avatar upload response was null or could not be deserialized to UserDto.");
                        }

                        if (string.IsNullOrEmpty(userDto.AvatarUrl))
                        {
                            throw new InvalidOperationException("Avatar upload response did not contain AvatarUrl.");
                        }

                        this.logger.Information("Avatar uploaded successfully: {Url}", userDto.AvatarUrl);
                        return userDto;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        this.logger.Warning("Avatar upload failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
                        throw new InvalidOperationException($"Failed to upload avatar: {errorContent}");
                    }
                }
                finally
                {
                    if (shouldCompress)
                    {
                        fileStream.Dispose();
                    }
                    else
                    {
                        fileStream.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error uploading avatar: {FilePath}", filePath);
                throw;
            }
        }
    }
}
