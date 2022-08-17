using System;
using System.Text;

namespace CmdLineArgsParser.Extensions
{
    /// <summary>
    /// String extensions
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Turn the first character of a string to uppercase
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Capitalize(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            if (str.Length > 1)
                return $"{ char.ToUpper(str[0])}{str.Substring(1) }";
            return $"{ char.ToUpper(str[0]) }";
        }

        /// <summary>
        /// Word wraps the given text to fit within the specified width.
        /// </summary>
        /// <param name="str">Text to be word wrapped</param>
        /// <param name="width">Width, in characters, to which the text
        /// should be word wrapped</param>
        /// <remarks>https://www.codeproject.com/articles/51488/implementing-word-wrap-in-c</remarks>
        /// <returns>The modified text</returns>
        public static string WordWrap(this string str, int width)
        {
            int pos, next;
            StringBuilder sb = new StringBuilder();

            // Lucidity check
            if (width < 1)
                throw new ArgumentException($"{nameof(width)} must be greater than 1", nameof(width));

            // Parse each line of text
            for (pos = 0; pos < str.Length; pos = next) {
                // Find end of line
                int eol = str.IndexOf(Environment.NewLine, pos, StringComparison.InvariantCultureIgnoreCase);
                if (eol == -1)
                    next = eol = str.Length;
                else
                    next = eol + Environment.NewLine.Length;

                // Copy this line of text, breaking into smaller lines as needed
                if (eol > pos) {
                    do {
                        int len = eol - pos;
                        if (len > width)
                            len = BreakLine(str, pos, width);
                        sb.Append(str, pos, len);
                        sb.Append(Environment.NewLine);

                        // Trim whitespace following break
                        pos += len;
                        while (pos < eol && Char.IsWhiteSpace(str[pos]))
                            pos++;
                    } while (eol > pos);
                } else sb.Append(Environment.NewLine); // Empty line
            }

            return sb.ToString();
        }

        /// <summary>
        /// Locates position to break the given line so as to avoid
        /// breaking words.
        /// </summary>
        /// <param name="text">String that contains line of text</param>
        /// <param name="pos">Index where line of text starts</param>
        /// <param name="max">Maximum line length</param>
        /// <returns>The modified line length</returns>
        private static int BreakLine(string text, int pos, int max)
        {
            // Find last whitespace in line
            int i = max;
            while (i >= 0 && !char.IsWhiteSpace(text[pos + i]))
                i--;

            // If no whitespace found, break at maximum length
            if (i < 0)
                return max;

            // Find start of whitespace
            while (i >= 0 && char.IsWhiteSpace(text[pos + i]))
                i--;

            // Return length of text before whitespace
            return i + 1;
        }
    }
}
