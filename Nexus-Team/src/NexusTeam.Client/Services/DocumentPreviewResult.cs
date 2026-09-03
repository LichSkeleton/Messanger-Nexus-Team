namespace NexusTeam.Client.Services
{
    /// <summary>
    /// Extracted content used to render an in-app document preview.
    /// </summary>
    public sealed class DocumentPreviewResult
    {
        /// <summary>
        /// Gets the original file name.
        /// </summary>
        public string FileName { get; init; } = string.Empty;

        /// <summary>
        /// Gets a short label for the file type (PDF, Word, Text).
        /// </summary>
        public string FileTypeLabel { get; init; } = string.Empty;

        /// <summary>
        /// Gets the extracted preview text.
        /// </summary>
        public string TextContent { get; init; } = string.Empty;

        /// <summary>
        /// Gets an optional notice, such as when only the first pages are shown.
        /// </summary>
        public string? Notice { get; init; }
    }
}
