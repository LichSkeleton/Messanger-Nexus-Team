// <copyright file="RichTextBoxHelper.cs" company="NexusTeam">
// Copyright (c) NexusTeam. All rights reserved.
// </copyright>

namespace NexusTeam.Client.Helpers
{
    using System;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;

    /// <summary>
    /// Helper class for binding text to RichTextBox controls.
    /// </summary>
    public static class RichTextBoxHelper
    {
        /// <summary>
        /// Identifies the DocumentText attached property.
        /// </summary>
        public static readonly DependencyProperty DocumentTextProperty =
            DependencyProperty.RegisterAttached(
                "DocumentText",
                typeof(string),
                typeof(RichTextBoxHelper),
                new FrameworkPropertyMetadata(string.Empty, OnDocumentTextChanged));

        /// <summary>
        /// Gets the document text from a RichTextBox.
        /// </summary>
        /// <param name="obj">The dependency object.</param>
        /// <returns>The document text.</returns>
        public static string GetDocumentText(DependencyObject obj)
        {
            return (string)obj.GetValue(DocumentTextProperty);
        }

        /// <summary>
        /// Sets the document text on a RichTextBox.
        /// </summary>
        /// <param name="obj">The dependency object.</param>
        /// <param name="value">The text value to set.</param>
        public static void SetDocumentText(DependencyObject obj, string value)
        {
            obj.SetValue(DocumentTextProperty, value);
        }

        private static void OnDocumentTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RichTextBox richTextBox)
            {
                var text = e.NewValue as string ?? string.Empty;

                // Setup document properties to remove extra spacing
                richTextBox.Document.Blocks.Clear();
                richTextBox.Document.PagePadding = new Thickness(0);
                richTextBox.Document.LineHeight = double.NaN; // Auto line height

                var paragraph = new Paragraph();

                // CRITICAL: Remove default paragraph spacing
                paragraph.Margin = new Thickness(0);
                paragraph.Padding = new Thickness(0);

                // Regex to find URLs (http, https, www)
                // Matches http/https URLs or strings starting with www.
                var urlRegex = new Regex(@"(https?://[^\s]+|www\.[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                var lastIndex = 0;
                foreach (Match match in urlRegex.Matches(text))
                {
                    // Add text before URL
                    if (match.Index > lastIndex)
                    {
                        var textBefore = text.Substring(lastIndex, match.Index - lastIndex);
                        paragraph.Inlines.Add(new Run(textBefore));
                    }

                    // Prepare URL
                    string url = match.Value;
                    if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        url = "http://" + url;
                    }

                    // Create Hyperlink
                    try
                    {
                        var link = new Hyperlink(new Run(match.Value))
                        {
                            NavigateUri = new Uri(url, UriKind.Absolute),
                            Cursor = System.Windows.Input.Cursors.Hand,

                            // Use a lighter blue for better visibility on dark background
                            Foreground = new SolidColorBrush(Color.FromRgb(0x4F, 0x9C, 0xDF)),
                        };

                        link.RequestNavigate += OnRequestNavigate;
                        paragraph.Inlines.Add(link);
                    }
                    catch (UriFormatException)
                    {
                        // If URI is invalid, just add as text
                        paragraph.Inlines.Add(new Run(match.Value));
                    }

                    lastIndex = match.Index + match.Length;
                }

                // Add remaining text
                if (lastIndex < text.Length)
                {
                    paragraph.Inlines.Add(new Run(text.Substring(lastIndex)));
                }

                // Ensure there is at least one inline
                if (paragraph.Inlines.Count == 0)
                {
                    paragraph.Inlines.Add(new Run(string.Empty));
                }

                // Ensure text wrapping works correctly
                richTextBox.Document.TextAlignment = TextAlignment.Left;

                richTextBox.Document.Blocks.Add(paragraph);
            }
        }

        private static void OnRequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                // Open the URL in the default browser
                var psi = new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true,
                };
                Process.Start(psi);
                e.Handled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening link: {ex.Message}");
            }
        }
    }
}
