namespace NexusTeam.Server.Data.Models
{
    using System;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    /// <summary>
    /// MongoDB model for GeneratedImage.
    /// </summary>
    public class GeneratedImage
    {
        /// <summary>
        /// Gets or sets the unique identifier for the generated image.
        /// </summary>
        [BsonId]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID who generated this image.
        /// </summary>
        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the prompt used to generate the image.
        /// </summary>
        [BsonElement("prompt")]
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model used for generation (flux, turbo, gptimage).
        /// </summary>
        [BsonElement("model")]
        public string Model { get; set; } = "flux";

        /// <summary>
        /// Gets or sets the image URL from Pollinations API.
        /// </summary>
        [BsonElement("imageUrl")]
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the local file path where image is stored.
        /// </summary>
        [BsonElement("filePath")]
        public string? FilePath { get; set; }

        /// <summary>
        /// Gets or sets the image width.
        /// </summary>
        [BsonElement("width")]
        public int Width { get; set; } = 1024;

        /// <summary>
        /// Gets or sets the image height.
        /// </summary>
        [BsonElement("height")]
        public int Height { get; set; } = 1024;

        /// <summary>
        /// Gets or sets the timestamp when the image was generated.
        /// </summary>
        [BsonElement("generatedAt")]
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the image is deleted.
        /// </summary>
        [BsonElement("isDeleted")]
        public bool IsDeleted { get; set; }
    }
}
