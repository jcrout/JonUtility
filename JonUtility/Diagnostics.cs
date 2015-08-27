namespace JonUtility
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using SF = StringFunctions;

    /// <summary>
    ///     Provides static methods for benchmarking and providing generated
    ///     input for testing.
    /// </summary>
    public static class Diagnostics
    {
        private static readonly Random rand = new Random();

        [DllImport("Kernel32.dll")]
        public static extern void QueryPerformanceCounter(ref long ticks);

        public static void BenchmarkMethod(Action a, int Count, bool PrepareDelegate = true,
            Action<string> WriteLineMethod = null)
        {
            if (WriteLineMethod == null)
            {
                WriteLineMethod = s => Console.WriteLine(s);
            }

            if (Count < 1)
            {
                return;
            }

            if (PrepareDelegate)
            {
                RuntimeHelpers.PrepareDelegate(a);
            }

            long time1 = 0, time2 = 0;
            long slowestTime = 0, fastestTime = long.MaxValue;
            long[] times = new long[Count];

            for (int i = 0; i < Count; i++)
            {
                Diagnostics.QueryPerformanceCounter(ref time1);
                a();
                Diagnostics.QueryPerformanceCounter(ref time2);
                long dif = time2 - time1;
                slowestTime = Math.Max(slowestTime, dif);
                fastestTime = Math.Min(fastestTime, dif);
                times[i] = dif;
            }

            // Get the averages/StD of all runs
            double avg = times.Average();
            double stddev = Math.Sqrt(times.Select(x => Math.Pow(x - avg, 2d)).Average());

            // Get the averages/StD of last 20%
            int first80 = (int)(Count * .8);
            int last20 = Count - first80;
            var timesEnd = times.Skip(first80);
            double avg2 = timesEnd.Average();
            double stddev2 = Math.Sqrt(timesEnd.Select(x => Math.Pow(x - avg, 2d)).Average());

            // Report the results
            WriteLineMethod("Benchmark method " + a.GetMethodInfo().Name);
            WriteLineMethod(
                "   Average time: " + SF.TicksToMS((long)avg, 4) + ", StD: " + SF.TicksToMS((long)stddev, 4));
            WriteLineMethod(
                "   Last " + last20 + "% Average time: " + SF.TicksToMS((long)avg2, 4) + ", StD: " +
                SF.TicksToMS((long)stddev2, 4));
            WriteLineMethod("   Fastest time: " + SF.TicksToMS(fastestTime, 4));
            WriteLineMethod("   Slowest time: " + SF.TicksToMS(slowestTime, 4));
        }

        public static int GetRandomNumber(int minInclusive, int maxExclusive)
        {
            return Diagnostics.rand.Next(minInclusive, maxExclusive);
        }

        /// <summary>
        ///     Returns a random number between 0 and 1, with the specified
        ///     number of digits between 1 and 9 (defaults to 2)
        /// </summary>
        /// <param name="digits"></param>
        /// <returns>
        ///     The number of digit places in the return value, between 1
        ///     and 9 (defaults to 2).
        /// </returns>
        public static double GetRandomDouble(int digits = 2)
        {
            if (digits < 1)
            {
                digits = 1;
            }
            else if (digits > 9)
            {
                digits = 9;
            }
            double max = Math.Pow(10, digits);
            return Diagnostics.rand.Next(0, (int)max + 1) / max;
        }

        public static string GetRandomString(int minLength, int maxLength = -1)
        {
            if (minLength <= 0)
            {
                return string.Empty;
            }
            if (maxLength <= minLength)
            {
                maxLength = minLength + 1;
            }
            int count = Diagnostics.rand.Next(minLength, maxLength);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                int charnum = Diagnostics.rand.Next(97, 123);
                sb.Append((char)charnum);
            }
            return sb.ToString();
        }

        public static string[] GetProgressiveStrings(int count, int minimumLength, int maximumLength = -1,
            int startRange = 33, int endRange = 126)
        {
            int charRange = endRange - startRange;

            if (maximumLength < minimumLength)
            {
                maximumLength = minimumLength + 1;
            }
            int stringLength = Diagnostics.rand.Next(minimumLength, maximumLength);
            double maxCount = (int)Math.Pow(charRange, stringLength);
            if (count > maxCount)
            {
                throw new Exception("Count exceeds the maximum possible amount of " + maxCount + ".");
            }
            string[] strings = new string[count];

            char[] chars = new char[stringLength];
            int[] counters = new int[stringLength];
            for (int i = 0; i < stringLength; i++)
            {
                counters[i] = startRange;
            }

            int lastIndex = stringLength - 1;
            for (int i = 0; i < count; i++)
            {
                for (int i2 = 0; i2 < stringLength; i2++)
                {
                    chars[i2] = (char)(counters[i2]);
                }
                strings[i] = new string(chars);
                for (int i2 = lastIndex; i2 > -1; i2--)
                {
                    counters[i2]++;
                    if (counters[i2] > endRange)
                    {
                        counters[i2] = startRange;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return strings;
        }

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        public static void PrintValues(object objectToScan, Action<string> writeMethod = null)
        {
            if (objectToScan == null)
            {
                throw new ArgumentNullException(nameof(objectToScan));
            }

            if (writeMethod == null)
            {
                writeMethod = s => Console.WriteLine(s);
            }

            var type = objectToScan.GetType();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            writeMethod(String.Format("Printing values for {0}: {1}", type.Name, objectToScan.ToString()));
            foreach (var prop in properties)
            {
                try
                {
                    object propValue = prop.GetValue(objectToScan);
                    writeMethod(prop.Name + ": " + propValue.ToString());
                }
                catch (Exception ex)
                {
                    writeMethod(string.Format("{0}{1}: threw exception of type {2}", "    ", prop.Name, ex.GetType().FullName));
                }
            }
        }
    }
}
