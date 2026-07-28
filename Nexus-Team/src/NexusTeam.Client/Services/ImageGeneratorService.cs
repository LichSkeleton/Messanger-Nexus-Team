namespace NexusTeam.Client.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;
    using Microsoft.Win32;
    using NexusTeam.Shared.Dtos;
    using Serilog;

    /// <summary>
    /// Service for AI image generation using Pollinations API.
    /// </summary>
    public class ImageGeneratorService : IImageGeneratorService
    {
        private const string PollinationsBaseUrl = "https://image.pollinations.ai/prompt/";
        private readonly HttpClient pollinationsClient;
        private readonly HttpClient apiClient;
        private readonly ILogger logger;
        private readonly IErrorHandlingService errorHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageGeneratorService"/> class.
        /// </summary>
        /// <param name="pollinationsClient">HTTP client for Pollinations API.</param>
        /// <param name="apiClient">HTTP client for backend API.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="errorHandler">Error handling service.</param>
        public ImageGeneratorService(
            HttpClient pollinationsClient,
            HttpClient apiClient,
            ILogger logger,
            IErrorHandlingService errorHandler)
        {
            this.pollinationsClient = pollinationsClient;
            this.apiClient = apiClient;
            this.logger = logger;
            this.errorHandler = errorHandler;
        }

        /// <inheritdoc/>
        public async Task<(BitmapImage Image, byte[] ImageData, string ImageUrl)> GenerateImageAsync(
            string prompt,
            string model = "flux",
            int width = 1024,
            int height = 1024,
            CancellationToken cancellationToken = default)
        {
            // Simple GET request as per Pollinations API docs:
            // GET https://image.pollinations.ai/prompt/{prompt}
            var encodedPrompt = Uri.EscapeDataString(prompt);
            var seed = Random.Shared.Next(1, 999999);
            var url = $"{PollinationsBaseUrl}{encodedPrompt}?model={model}&width={width}&height={height}&seed={seed}&nologo=true";

            this.logger.Information("Generating image: {Url}", url);

            // Retry logic for rate limiting and server errors
            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var response = await this.pollinationsClient.GetAsync(url, cancellationToken);
                    var statusCode = (int)response.StatusCode;

                    // Handle rate limit (429)
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        if (attempt < maxRetries)
                        {
                            this.logger.Warning("Rate limited (429). Waiting 15 seconds before retry {Attempt}/{MaxRetries}...", attempt, maxRetries);
                            this.errorHandler.ShowWarning("Rate limit reached. Waiting 15 seconds...");
                            await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
                            continue;
                        }

                        throw new Exception("Rate limit exceeded. Please wait 15 seconds between image generations.");
                    }

                    // Handle server errors (5xx) - retry
                    if (statusCode >= 500)
                    {
                        if (attempt < maxRetries)
                        {
                            var delay = attempt * 5; // 5s, 10s
                            this.logger.Warning("Server error ({StatusCode}). Retrying in {Delay}s... ({Attempt}/{MaxRetries})", statusCode, delay, attempt, maxRetries);
                            this.errorHandler.ShowWarning($"Server error. Retrying in {delay} seconds...");
                            await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
                            continue;
                        }

                        throw new Exception($"Server error ({statusCode}). The Pollinations service may be temporarily unavailable.");
                    }

                    response.EnsureSuccessStatusCode();

                    var imageData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                    // Create BitmapImage from bytes
                    var bitmap = new BitmapImage();
                    using (var stream = new MemoryStream(imageData))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze();
                    }

                    this.logger.Information("Image generated successfully, size: {Size} bytes", imageData.Length);

                    return (bitmap, imageData, url);
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("429") || ex.Message.Contains("500") || ex.Message.Contains("502") || ex.Message.Contains("503"))
                {
                    if (attempt < maxRetries)
                    {
                        var delay = ex.Message.Contains("429") ? 15 : attempt * 5;
                        this.logger.Warning("Error: {Message}. Retrying in {Delay}s...", ex.Message, delay);
                        this.errorHandler.ShowWarning($"Retrying in {delay} seconds...");
                        await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);
                        continue;
                    }

                    throw;
                }
            }

            throw new Exception("Failed to generate image after retries");
        }

        /// <inheritdoc/>
        public async Task<GeneratedImageDto> SaveGeneratedImageAsync(
            string prompt,
            string model,
            string imageUrl,
            byte[] imageData,
            int width,
            int height,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // First create the record
                var createRequest = new
                {
                    Prompt = prompt,
                    Model = model,
                    ImageUrl = imageUrl,
                    Width = width,
                    Height = height,
                };

                var response = await this.apiClient.PostAsJsonAsync(
                    "/api/generated-images",
                    createRequest,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var createdImage = await response.Content.ReadFromJsonAsync<GeneratedImageDto>(cancellationToken: cancellationToken);
                if (createdImage == null)
                {
                    throw new InvalidOperationException("Failed to create image record");
                }

                // Then save the image data
                var saveDataRequest = new
                {
                    ImageDataBase64 = Convert.ToBase64String(imageData),
                };

                var saveResponse = await this.apiClient.PostAsJsonAsync(
                    $"/api/generated-images/{createdImage.Id}/data",
                    saveDataRequest,
                    cancellationToken);

                if (saveResponse.IsSuccessStatusCode)
                {
                    var result = await saveResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                    if (result.TryGetProperty("downloadUrl", out var downloadUrl))
                    {
                        createdImage.DownloadUrl = downloadUrl.GetString();
                    }
                }

                this.logger.Information("Generated image saved to server: {Id}", createdImage.Id);
                return createdImage;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to save generated image");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<GeneratedImageDto>> GetGeneratedImagesAsync(int limit = 50, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this.apiClient.GetAsync($"/api/generated-images?limit={limit}", cancellationToken);
                response.EnsureSuccessStatusCode();

                var images = await response.Content.ReadFromJsonAsync<List<GeneratedImageDto>>(cancellationToken: cancellationToken);
                return images ?? new List<GeneratedImageDto>();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to get generated images");
                return new List<GeneratedImageDto>();
            }
        }

        /// <inheritdoc/>
        public async Task<GeneratedImageDto?> GetGeneratedImageAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this.apiClient.GetAsync($"/api/generated-images/{id}", cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<GeneratedImageDto>(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to get generated image: {Id}", id);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteGeneratedImageAsync(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this.apiClient.DeleteAsync($"/api/generated-images/{id}", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to delete generated image: {Id}", id);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetRecentPromptsAsync(int limit = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this.apiClient.GetAsync($"/api/generated-images/prompts?limit={limit}", cancellationToken);
                response.EnsureSuccessStatusCode();

                var prompts = await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: cancellationToken);
                return (prompts ?? new List<string>())
                    .Select(p => (p ?? string.Empty).Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to get recent prompts");
                return new List<string>();
            }
        }

        /// <inheritdoc/>
        public async Task<BitmapImage> DownloadStoredImageAsync(string downloadUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this.apiClient.GetAsync(downloadUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var imageData = await response.Content.ReadAsByteArrayAsync(cancellationToken);

                var bitmap = new BitmapImage();
                using (var stream = new MemoryStream(imageData))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to download stored image: {Url}", downloadUrl);
                throw;
            }
        }

        /// <inheritdoc/>
        public Task<string?> SaveImageToFileAsync(byte[] imageData, string suggestedFileName)
        {
            return Task.Run(() =>
            {
                var dialog = new SaveFileDialog
                {
                    FileName = suggestedFileName,
                    Title = "Save Generated Image",
                    Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|All files (*.*)|*.*",
                    DefaultExt = ".png",
                };

                var result = dialog.ShowDialog();
                if (result != true)
                {
                    return null;
                }

                File.WriteAllBytes(dialog.FileName, imageData);
                return dialog.FileName;
            });
        }
    }
}
