using System;
using System.Collections.Generic;
using System.Text;

namespace SmartRoadSense.Redux {

    public static class StringExtensions {

        /// <summary>
        /// Converts a word to title case.
        /// </summary>
        /// <remarks>
        /// Does not respect acronyms and full uppercase words.
        /// </remarks>
        public static string ToTitleCase(this string s) {
            if(string.IsNullOrEmpty(s))
                return string.Empty;

            var sb = new StringBuilder(s.Length);

            bool waitingForWord = true;
            foreach(var c in s) {
                if(waitingForWord && Char.IsLetterOrDigit(c)) {
                    //Found first letter of word
                    sb.Append(Char.ToUpperInvariant(c));
                    waitingForWord = false;
                }
                else {
                    sb.Append(Char.ToLowerInvariant(c));

                    if(Char.IsWhiteSpace(c)) {
                        waitingForWord = true;
                    }
                }
            }

            return sb.ToString();
        }

    }

}
