namespace NexusTeam.Shared.Tests.Helpers
{
    using System;
    using System.Linq;
    using NexusTeam.Shared.Helpers;
    using Xunit;

    public class MessageContentHelperTests
    {
        [Fact]
        public void NeedsTruncation_WithShortText_ReturnsFalse()
        {
            Assert.False(MessageContentHelper.NeedsTruncation("Hello"));
            Assert.False(MessageContentHelper.NeedsTruncation(string.Empty));
            Assert.False(MessageContentHelper.NeedsTruncation(null));
        }

        [Fact]
        public void NeedsTruncation_WithLongText_ReturnsTrue()
        {
            var content = new string('a', MessageContentHelper.CollapsedMaxLength + 1);

            Assert.True(MessageContentHelper.NeedsTruncation(content));
        }

        [Fact]
        public void NeedsTruncation_WithTooManyLines_ReturnsTrue()
        {
            var lines = Enumerable.Repeat("line", MessageContentHelper.CollapsedMaxLines + 1);
            var content = string.Join("\n", lines);

            Assert.True(MessageContentHelper.NeedsTruncation(content));
        }

        [Fact]
        public void GetDisplayContent_WhenCollapsed_TruncatesAndAddsEllipsis()
        {
            var content = new string('a', MessageContentHelper.CollapsedMaxLength + 50);

            var result = MessageContentHelper.GetDisplayContent(content, isExpanded: false);

            Assert.EndsWith("...", result);
            Assert.True(result.Length < content.Length);
        }

        [Fact]
        public void GetDisplayContent_WhenExpanded_ReturnsFullContent()
        {
            var content = new string('a', MessageContentHelper.CollapsedMaxLength + 50);

            var result = MessageContentHelper.GetDisplayContent(content, isExpanded: true);

            Assert.Equal(content, result);
        }

        [Fact]
        public void GetDisplayContent_WhenCollapsed_BreaksOnWordBoundary()
        {
            var content = string.Join(" ", Enumerable.Repeat("word", 200));

            var result = MessageContentHelper.GetDisplayContent(content, isExpanded: false);
            var withoutEllipsis = result.Substring(0, result.Length - 3);

            Assert.EndsWith("...", result);
            Assert.EndsWith("word", withoutEllipsis);
        }
    }
}
