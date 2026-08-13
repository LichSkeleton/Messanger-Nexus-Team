namespace NexusTeam.Client.Services
{
    /// <summary>
    /// Extracts readable preview content from document files.
    /// </summary>
    public interface IDocumentPreviewService
    {
        /// <summary>
        /// Builds a preview model from a local file.
        /// </summary>
        /// <param name="filePath">The local path of the downloaded temp file.</param>
        /// <param name="fileName">The original file name.</param>
        /// <returns>The extracted preview content.</returns>
        DocumentPreviewResult LoadPreview(string filePath, string fileName);
    }
}
