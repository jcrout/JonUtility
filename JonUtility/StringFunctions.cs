namespace JonUtility
{
    using System;
    using System.Diagnostics;

    public static class StringFunctions
    {
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
    }
}
