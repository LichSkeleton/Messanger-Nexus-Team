namespace NexusTeam.Shared.Tests.Helpers
{
    using NexusTeam.Shared.Enums;
    using NexusTeam.Shared.Helpers;
    using Xunit;

    public class FileHelperTests
    {
        [Theory]
        [InlineData("photo.JPG", AttachmentType.Image)]
        [InlineData("movie.mp4", AttachmentType.Video)]
        [InlineData("voice.flac", AttachmentType.Audio)]
        [InlineData("report.pdf", AttachmentType.Document)]
        [InlineData("backup.7z", AttachmentType.Archive)]
        [InlineData("service.cs", AttachmentType.Code)]
        public void GetAttachmentType_WithKnownExtension_ReturnsExpectedType(
            string fileName,
            AttachmentType expected)
        {
            var result = FileHelper.GetAttachmentType(fileName);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("file.unknown")]
        [InlineData("file-without-extension")]
        public void GetAttachmentType_WithUnknownExtension_ReturnsOther(string fileName)
        {
            var result = FileHelper.GetAttachmentType(fileName);

            Assert.Equal(AttachmentType.Other, result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void IsValidFileSize_WithNonPositiveSize_ReturnsFalse(long fileSize)
        {
            var result = FileHelper.IsValidFileSize(fileSize, AttachmentType.Document);

            Assert.False(result);
        }

        [Fact]
        public void IsValidFileSize_WithImageAtMaximumSize_ReturnsTrue()
        {
            var result = FileHelper.IsValidFileSize(
                FileHelper.MaxImageSizeBytes,
                AttachmentType.Image);

            Assert.True(result);
        }

        [Fact]
        public void IsValidFileSize_WithImageAboveMaximumSize_ReturnsFalse()
        {
            var result = FileHelper.IsValidFileSize(
                FileHelper.MaxImageSizeBytes + 1,
                AttachmentType.Image);

            Assert.False(result);
        }

        [Fact]
        public void IsValidFileSize_WithDocumentAtMaximumSize_ReturnsTrue()
        {
            var result = FileHelper.IsValidFileSize(
                FileHelper.MaxFileSizeBytes,
                AttachmentType.Document);

            Assert.True(result);
        }

        [Fact]
        public void IsValidFileSize_WithDocumentAboveMaximumSize_ReturnsFalse()
        {
            var result = FileHelper.IsValidFileSize(
                FileHelper.MaxFileSizeBytes + 1,
                AttachmentType.Document);

            Assert.False(result);
        }

        [Theory]
        [InlineData(0, "0 B")]
        [InlineData(1023, "1023 B")]
        [InlineData(1024, "1 KB")]
        [InlineData(1536, "1.5 KB")]
        [InlineData(1048576, "1 MB")]
        [InlineData(1073741824, "1 GB")]
        public void FormatFileSize_ReturnsHumanReadableValue(long bytes, string expected)
        {
            var result = FileHelper.FormatFileSize(bytes);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("photo.JPEG", "image/jpeg")]
        [InlineData("document.pdf", "application/pdf")]
        [InlineData("data.JSON", "application/json")]
        public void GetContentType_WithKnownExtension_ReturnsExpectedMimeType(
            string fileName,
            string expected)
        {
            var result = FileHelper.GetContentType(fileName);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetContentType_WithUnknownExtension_ReturnsBinaryMimeType()
        {
            var result = FileHelper.GetContentType("archive.custom");

            Assert.Equal("application/octet-stream", result);
        }

        [Theory]
        [InlineData("malware.exe")]
        [InlineData("library.DLL")]
        [InlineData("script.bat")]
        [InlineData("command.cmd")]
        [InlineData("screensaver.scr")]
        [InlineData("script.vbs")]
        [InlineData("script.js")]
        public void IsAllowedFileType_WithBlockedExtension_ReturnsFalse(string fileName)
        {
            var result = FileHelper.IsAllowedFileType(fileName);

            Assert.False(result);
        }

        [Theory]
        [InlineData("photo.png")]
        [InlineData("document.pdf")]
        [InlineData("source.cs")]
        [InlineData("file-without-extension")]
        public void IsAllowedFileType_WithSafeExtension_ReturnsTrue(string fileName)
        {
            var result = FileHelper.IsAllowedFileType(fileName);

            Assert.True(result);
        }
    }
}
