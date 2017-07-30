namespace JonUtility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;

    public class LevenshteinComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return MathFunctions.LevenshteinDistance(x, y);
        }

        public static LevenshteinComparer Create()
        {
            return new LevenshteinComparer();
        }
    }

    public static class StringFunctions
    {
        private static readonly string illegalFileCharsPattern = "[" + Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars())) + "]";
        public static string StripIllegalFileChars(string text)
        {
            return Regex.Replace(text, illegalFileCharsPattern, "");
        }

        public static int RegexParseInteger(string input, string pattern)
        {
            var num = Regex.Match(input, pattern);
            if (num != null && num.Groups.Count > 1)
            {
                return Int32.Parse(num.Groups[1].Value);
            }
            else
            {
                return -1;
            }
        }

        public static string GetSubscriptString(int number)
        {
            if (number < 10)
            {
                return char.ConvertFromUtf32(8320 + number);
            }

            string numString = number.ToString();
            string subscriptString = "";
            foreach (int c in numString)
            {
                subscriptString += char.ConvertFromUtf32(8320 + (c - 48));
            }

            return subscriptString;
        }

        public static string TicksToMS(long ticks, int digits = 2, string showUnit = "ms")
        {
            return (ticks / (double)Stopwatch.Frequency * 1000d).ToString("0.".PadRight(digits + 2, '0')) + showUnit;
        }

        public static string TicksToSeconds(long ticks, int digits = 2, string showUnit = "s")
        {
            return (ticks / (double)Stopwatch.Frequency).ToString("0.".PadRight(digits + 2, '0')) + showUnit;
        }

        public static string GetByteSizeString(long bytes, int decimalDigits = 2, bool fullUnitName = false)
        {
            const long KilobyteAmount = 1024L;
            const long MegabyteAmount = KilobyteAmount * KilobyteAmount;
            const long GigabyteAmount = MegabyteAmount * KilobyteAmount;
            const long TerabyteAmount = GigabyteAmount * KilobyteAmount;
            const long PetabyteAmount = TerabyteAmount * KilobyteAmount;

            if (bytes < KilobyteAmount)
            {
                return bytes + (!fullUnitName ? " B" : " Bytes");
            }

            string padding = decimalDigits == 0 ? "0" : "0.".PadRight(decimalDigits + 2, '0');

            if (bytes < MegabyteAmount)
            {
                return Math.Round((decimal)bytes / KilobyteAmount, decimalDigits).ToString(padding) +
                       (!fullUnitName ? " KB" : " Kiloytes");
            }
            if (bytes < GigabyteAmount)
            {
                return Math.Round((decimal)bytes / MegabyteAmount, decimalDigits).ToString(padding) +
                       (!fullUnitName ? " MB" : " Megabytes");
            }
            if (bytes < TerabyteAmount)
            {
                return Math.Round((decimal)bytes / GigabyteAmount, decimalDigits).ToString(padding) +
                       (!fullUnitName ? " GB" : " Gigabytes");
            }
            if (bytes < PetabyteAmount)
            {
                return Math.Round((decimal)bytes / TerabyteAmount, decimalDigits).ToString(padding) +
                       (!fullUnitName ? " TB" : " Terabytes");
            }
            return Math.Round((decimal)bytes / PetabyteAmount, decimalDigits).ToString(padding) +
                   (!fullUnitName ? " PB" : " Petabytes");
        }

        /// <summary>
        ///     Prefixes the text with either 'a' or 'an' (or 'A' or 'An' if capitalization is specificed) and a space,
        ///     depending on the first letter of the text.
        /// </summary>
        /// <param name="text">The text to prefix.</param>
        /// <param name="capitalizeA">true to capitalize 'a' or 'an'; otherwise, 'a' or 'an' are used in lowercase.</param>
        /// <returns>The original text prefixed by either 'a' or 'an' (or 'A' or 'An' if capitalization is specificed) and a space.</returns>
        public static string AorAn(string text, bool capitalizeA = false)
        {
            var prefix = capitalizeA ? 'A' : 'a';

            if (String.IsNullOrWhiteSpace(text))
            {
                return prefix + " ";
            }

            var firstChar = Char.ToUpper(text[0]);
            if (firstChar == 'A' || firstChar == 'E' || firstChar == 'I' || firstChar == 'O' || firstChar == 'U')
            {
                return prefix + "n " + text;
            }
            else
            {
                return prefix + " " + text;
            }
        }
    }
}
