namespace NexusTeam.Shared.Dtos
{
    using System;

    /// <summary>
    /// Data transfer object for generated images.
    /// </summary>
    public class GeneratedImageDto
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user ID who generated this image.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the prompt used to generate the image.
        /// </summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model used for generation.
        /// </summary>
        public string Model { get; set; } = "flux";

        /// <summary>
        /// Gets or sets the image URL.
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the download URL for the stored image.
        /// </summary>
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// Gets or sets the image width.
        /// </summary>
        public int Width { get; set; } = 1024;

        /// <summary>
        /// Gets or sets the image height.
        /// </summary>
        public int Height { get; set; } = 1024;

        /// <summary>
        /// Gets or sets the timestamp when the image was generated.
        /// </summary>
        public DateTime GeneratedAt { get; set; }
    }
}
