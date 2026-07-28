namespace NexusTeam.Shared.Enums
{
    /// <summary>
    /// Types of file attachments supported in messages.
    /// </summary>
    public enum AttachmentType
    {
        /// <summary>
        /// Image file (jpg, png, gif, bmp, etc.)
        /// </summary>
        Image = 0,

        /// <summary>
        /// Video file (mp4, avi, mkv, etc.)
        /// </summary>
        Video = 1,

        /// <summary>
        /// Audio file (mp3, wav, ogg, etc.)
        /// </summary>
        Audio = 2,

        /// <summary>
        /// Document file (pdf, doc, docx, txt, etc.)
        /// </summary>
        Document = 3,

        /// <summary>
        /// Archive file (zip, rar, 7z, tar, etc.)
        /// </summary>
        Archive = 4,

        /// <summary>
        /// Code file (cs, js, py, java, etc.)
        /// </summary>
        Code = 5,

        /// <summary>
        /// Other/unknown file type.
        /// </summary>
        Other = 99,
    }
}
