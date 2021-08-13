using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Whalerator
{
    public class Banners
    {
        /// <summary>
        /// Determines if a given source string is a file path.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsFileSource(string source) =>
            !string.IsNullOrWhiteSpace(source) &&
            source.Split('\n').Length == 1 &&
            !leadingRegex.IsMatch(source) &&
            !trailingRegex.IsMatch(source);       

        /// <summary>
        /// Loads banner text from a source string. If the source is a file path, text will be read from that file. Otherwise, the string will
        /// simply be returned unmodified.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ReadBanner(string source)
        {
            if (IsFileSource(source))
            {
                if (File.Exists(source))
                {
                    return File.ReadAllText(source);
                }
                else
                {
                    throw new FileNotFoundException($"The file does not exist ({source}). To use literal text, the banner must include at least one newline, or leading/trailing whitespace.", source);
                }
            }
            return source;
        }

        private static Regex leadingRegex = new Regex(@"^\s", RegexOptions.Compiled);
        private static Regex trailingRegex = new Regex(@"\s$", RegexOptions.Compiled);

    }
}
