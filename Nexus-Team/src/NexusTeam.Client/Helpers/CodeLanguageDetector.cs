namespace NexusTeam.Client.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Helper class to detect programming language from file extension.
    /// </summary>
    public static class CodeLanguageDetector
    {
        private static readonly Dictionary<string, string> ExtensionToLanguageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // C#
            { ".cs", "C#" },

            // JavaScript/TypeScript
            { ".js", "JavaScript" },
            { ".ts", "TypeScript" },
            { ".jsx", "JavaScript" },
            { ".tsx", "TypeScript" },

            // Python
            { ".py", "Python" },

            // Java
            { ".java", "Java" },

            // C/C++
            { ".cpp", "C++" },
            { ".cc", "C++" },
            { ".cxx", "C++" },
            { ".c", "C" },
            { ".h", "C/C++" },
            { ".hpp", "C++" },

            // Go
            { ".go", "Go" },

            // Rust
            { ".rs", "Rust" },

            // PHP
            { ".php", "PHP" },

            // Ruby
            { ".rb", "Ruby" },

            // Swift
            { ".swift", "Swift" },

            // Kotlin
            { ".kt", "Kotlin" },
            { ".kts", "Kotlin" },

            // Scala
            { ".scala", "Scala" },

            // Web
            { ".html", "HTML" },
            { ".htm", "HTML" },
            { ".css", "CSS" },
            { ".scss", "SCSS" },
            { ".sass", "Sass" },
            { ".less", "Less" },
            { ".vue", "HTML" },
            { ".svelte", "HTML" },

            // Data formats
            { ".json", "JSON" },
            { ".xml", "XML" },
            { ".yaml", "YAML" },
            { ".yml", "YAML" },

            // SQL
            { ".sql", "SQL" },

            // Shell scripts
            { ".sh", "Shell" },
            { ".bash", "Bash" },
            { ".zsh", "Zsh" },

            // Batch/PowerShell
            { ".bat", "Batch" },
            { ".cmd", "Batch" },
            { ".ps1", "PowerShell" },

            // XAML
            { ".xaml", "XAML" },
            { ".axaml", "XAML" },

            // Other
            { ".md", "Markdown" },
            { ".txt", "Text" },
        };

        private static readonly Dictionary<string, string> ExtensionToAvalonSyntaxMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".cs", "C#" },
            { ".cpp", "C++" },
            { ".cc", "C++" },
            { ".cxx", "C++" },
            { ".c", "C++" },
            { ".h", "C++" },
            { ".hpp", "C++" },
            { ".java", "Java" },
            { ".js", "JavaScript" },
            { ".ts", "JavaScript" },
            { ".jsx", "JavaScript" },
            { ".tsx", "JavaScript" },
            { ".php", "PHP" },
            { ".html", "HTML" },
            { ".htm", "HTML" },
            { ".vue", "HTML" },
            { ".svelte", "HTML" },
            { ".xml", "XML" },
            { ".css", "CSS" },
            { ".scss", "CSS" },
            { ".sass", "CSS" },
            { ".less", "CSS" },
            { ".json", "JavaScript" },
            { ".yaml", "JavaScript" },
            { ".yml", "JavaScript" },
            { ".sql", "SQL" },
            { ".py", "Python" },
            { ".vb", "VB" },
            { ".aspx", "ASP/XHTML" },
            { ".xaml", "XML" },
            { ".axaml", "XML" },
            { ".sh", "JavaScript" },
            { ".bat", "JavaScript" },
            { ".ps1", "PowerShell" },
            { ".md", "MarkDown" },
        };

        /// <summary>
        /// Gets the programming language name from file extension.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The language name, or "Unknown" if not recognized.</returns>
        public static string GetLanguageName(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return ExtensionToLanguageMap.TryGetValue(extension, out var language) ? language : "Unknown";
        }

        /// <summary>
        /// Gets the AvalonEdit syntax highlighting name from file extension.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>The syntax name for AvalonEdit, or null if not supported.</returns>
        public static string? GetAvalonSyntaxName(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return ExtensionToAvalonSyntaxMap.TryGetValue(extension, out var syntax) ? syntax : null;
        }

        /// <summary>
        /// Checks if the file extension is a supported code file.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <returns>True if it's a code file, false otherwise.</returns>
        public static bool IsCodeFile(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return ExtensionToLanguageMap.ContainsKey(extension);
        }
    }
}
