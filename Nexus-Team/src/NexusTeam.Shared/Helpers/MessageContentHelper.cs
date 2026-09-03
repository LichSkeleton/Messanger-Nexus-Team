namespace NexusTeam.Shared.Helpers
{
    using System;
    using System.Linq;

    /// <summary>
    /// Helper for collapsing long chat message text with a Read more action.
    /// </summary>
    public static class MessageContentHelper
    {
        /// <summary>
        /// Maximum number of characters shown before a message is collapsed.
        /// </summary>
        public const int CollapsedMaxLength = 480;

        /// <summary>
        /// Maximum number of lines shown before a message is collapsed.
        /// </summary>
        public const int CollapsedMaxLines = 10;

        /// <summary>
        /// Checks whether message content is long enough to need a Read more control.
        /// </summary>
        /// <param name="content">The full message content.</param>
        /// <returns>True if the content should be truncated until expanded.</returns>
        public static bool NeedsTruncation(string? content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            if (content.Length > CollapsedMaxLength)
            {
                return true;
            }

            return CountLines(content) > CollapsedMaxLines;
        }

        /// <summary>
        /// Gets the text that should be shown in the chat bubble.
        /// </summary>
        /// <param name="content">The full message content.</param>
        /// <param name="isExpanded">Whether the user expanded the message.</param>
        /// <returns>The full content, or a truncated preview ending with an ellipsis.</returns>
        public static string GetDisplayContent(string? content, bool isExpanded)
        {
            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            if (isExpanded || !NeedsTruncation(content))
            {
                return content;
            }

            return Truncate(content);
        }

        private static string Truncate(string content)
        {
            var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            var limited = lines.Length > CollapsedMaxLines
                ? string.Join("\n", lines.Take(CollapsedMaxLines))
                : content;

            if (limited.Length <= CollapsedMaxLength)
            {
                return limited.TrimEnd() + "...";
            }

            var cut = limited.Substring(0, CollapsedMaxLength);
            var lastSpace = cut.LastIndexOf(' ');
            if (lastSpace >= CollapsedMaxLength / 2)
            {
                cut = cut.Substring(0, lastSpace);
            }

            return cut.TrimEnd() + "...";
        }

        private static int CountLines(string content)
        {
            var count = 1;
            for (var i = 0; i < content.Length; i++)
            {
                if (content[i] == '\n')
                {
                    count++;
                }
            }

            return count;
        }
    }
}
