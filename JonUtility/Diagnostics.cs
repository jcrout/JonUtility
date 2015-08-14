namespace JonUtility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using SF = StringFunctions;

    public static class Diagnostics
    {
        private static readonly Random rand = new Random();

        [DllImport("Kernel32.dll")]
        public static extern void QueryPerformanceCounter(ref long ticks);

        public static void BenchmarkMethod(Action a, int Count, bool PrepareDelegate = true,
            Action<string> WriteLineMethod = null)
        {
            // (Action<string>)typeof(Console).GetMethod("WriteLine", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null).CreateDelegate(typeof(Action<string>))
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

        public static byte[] RawSerialize(object obj)
        {
            int rawsize = Marshal.SizeOf(obj);
            IntPtr buffer = Marshal.AllocHGlobal(rawsize);
            Marshal.StructureToPtr(obj, buffer, false);
            byte[] rawdata = new byte[rawsize];
            Marshal.Copy(buffer, rawdata, 0, rawsize);
            Marshal.FreeHGlobal(buffer);
            return rawdata;
        }

        public static void StructAnalyzer(ValueType Struct)
        {
            Type T = Struct.GetType();
            Trace.WriteLine("Analyzing struct " + T.Name + ": " + Struct);
            Trace.Indent();

            StructLayoutAttribute sla = T.StructLayoutAttribute;
            bool autoLayout = false;
            if (sla != null)
            {
                autoLayout = sla.Value.HasFlag(LayoutKind.Auto);
                Trace.WriteLine(
                    string.Format("StructLayout = {0}, Pack = {1}, Size = {2}", sla.Value, sla.Pack, sla.Size));
            }
            if (!autoLayout)
            {
                Trace.WriteLine(string.Format("Size: {0}", Marshal.SizeOf(T)));
            }

            FieldInfo[] fields = T.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                string accessor = field.IsPublic ? "public" : "private";
                Trace.WriteLine(string.Format("{0} {1} {2}", accessor, field.FieldType.Name, field.Name));
                Trace.Indent();
                var attributes = field.CustomAttributes;
                var foa = attributes.FirstOrDefault(attr => attr.AttributeType == typeof(FieldOffsetAttribute));

                if (foa != null)
                {
                    Trace.WriteLine(string.Format("[FieldOffset({0})]", foa.ConstructorArguments[0].Value));
                }
                if (!autoLayout)
                {
                    Trace.WriteLine(string.Format("Offset: {0}", Marshal.OffsetOf(T, field.Name).ToInt64()));
                }

                Trace.Unindent();
            }

            Trace.Unindent();
        }

        public static void PrintValues(object obj, Action<string> WriteMethod = null)
        {
            _PrintValues pv = new _PrintValues(obj, WriteMethod);
            pv.Start();
        }

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        private class _PrintValues
        {
            private static readonly Action<string> defaultWriteMethod;
            private readonly List<Tuple<string, object>> CheckedProperties = new List<Tuple<string, object>>(20);
            private readonly object Original;
            private readonly Action<string> WriteMethod;

            static _PrintValues()
            {
                _PrintValues.defaultWriteMethod =
                    (Action<string>)
                        typeof(Console).GetMethod(
                            "WriteLine",
                            BindingFlags.Public | BindingFlags.Static,
                            null,
                            new Type[1] {typeof(string)},
                            null).CreateDelegate(typeof(Action<string>), null);
            }

            public _PrintValues(object original, Action<string> writeMethod)
            {
                this.Original = original;
                this.WriteMethod = writeMethod ?? _PrintValues.defaultWriteMethod;
            }

            public void Start()
            {
                this.WriteMethod("Analyzing " + this.Original.GetType().Name + " " + this.Original);
                try
                {
                    this.PrintValues(this.Original, "");
                }
                catch (Exception ex)
                {
                    this.WriteMethod("Error: " + ex.Message);
                }
            }

            private void PrintValues(object obj, string indent)
            {
                Type T = obj.GetType();

                Tuple<string, MemberInfo[]>[] members = new Tuple<string, MemberInfo[]>[3];
                members[0] = new Tuple<string, MemberInfo[]>(
                    "Public Properties:",
                    T.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(pi => pi.GetIndexParameters().Length == 0)
                     .ToArray<MemberInfo>());
                members[1] = new Tuple<string, MemberInfo[]>(
                    "Public Fields:",
                    T.GetFields(BindingFlags.Public | BindingFlags.Instance));
                members[2] = new Tuple<string, MemberInfo[]>(
                    "Private Fields:",
                    T.GetFields(BindingFlags.NonPublic | BindingFlags.Instance));

                int count = members.Sum(list => list.Item2.Length) + members.Length;
                string indent1 = indent + "   ";
                string indent2 = indent1 + "   ";

                //StringBuilder sb = new StringBuilder();
                foreach (var minfo in members)
                {
                    if (minfo.Item2.Length == 0)
                    {
                        continue;
                    }
                    this.WriteMethod(indent1 + minfo.Item1);
                    int length = minfo.Item2.Max(mi => mi.Name.Length);

                    for (int i = 0; i < minfo.Item2.Length; i++)
                    {
                        MemberInfo member = minfo.Item2[i];
                        Type memType = (member.MemberType == MemberTypes.Property)
                            ? ((PropertyInfo)member).PropertyType
                            : ((FieldInfo)member).FieldType;
                        string memPrefix = member.Name + " {" + memType.Name + "}";

                        object value;
                        try
                        {
                            value = ((dynamic)member).GetValue(obj);
                            this.ProcessValue(member, value, indent2, memPrefix);
                        }
                        catch (Exception ex)
                        {
                            this.WriteMethod(indent2 + memPrefix + ": threw Exception of type " + ex.GetType().Name);
                        }
                    }
                }
            }

            private void ProcessArray(MemberInfo member, dynamic array, string indent, string indexString = "")
            {
                Func<int, int[]> GetIndices = null;
                int length = array.Length;
                int rank = array.Rank;
                if (rank > 1)
                {
                    int[] sums = null;
                    int sum = 1;
                    sums = new int[rank];
                    for (int i = rank - 1, i2 = 0; i > 0; i--, i2++)
                    {
                        sum *= (array.GetUpperBound(i) + 1);
                        sums[i] = sum;
                    }
                    GetIndices = (x =>
                    {
                        int[] indices = new int[rank];
                        double temp = x;
                        for (int i2 = 0; i2 < rank - 1; i2++)
                        {
                            indices[i2] = (int)Math.Floor(temp / sums[i2 + 1]);
                            if (indices[i2] > 0)
                            {
                                temp -= indices[i2] * sums[i2 + 1];
                            }
                        }
                        indices[rank - 1] = x % sums[rank - 1];
                        return indices;
                    });
                }

                object o = null;
                string val;
                for (int i = 0; i < length; i++)
                {
                    if (rank == 1)
                    {
                        o = array[i];
                        val = i.ToString();
                    }
                    else
                    {
                        int[] indices = GetIndices(i);
                        val = string.Join(",", indices.Take(rank - 1)) + "," + indices[rank - 1];
                        o = array.GetValue(indices);
                    }

                    if (o == null)
                    {
                        this.WriteMethod(indent + indexString + val + ": null");
                    }
                    else
                    {
                        Type T = o.GetType();
                        if (!T.IsArray)
                        {
                            this.ProcessValue(member, o, indent, indexString + val);
                        }
                        else
                        {
                            string newIndex = indexString + val + ",";
                            ProcessArray(member: member, array: (dynamic)o, indent: indent, indexString: newIndex);
                        }
                    }
                }
            }

            private void ProcessValue(MemberInfo member, object value, string indent, string prefix)
            {
                if (value == null)
                {
                    this.WriteMethod(indent + prefix + ": null");
                    return;
                }

                if (value is string)
                {
                    this.WriteMethod(indent + prefix + ": " + value);
                    return;
                }

                Type T2 = value.GetType();
                if (T2.IsPrimitive)
                {
                    this.WriteMethod(indent + prefix + ": " + value);
                    return;
                }

                Tuple<string, object> lolzo;
                if ((lolzo = this.CheckedProperties.FirstOrDefault(x => x.Item2 == value)) != null)
                {
                    this.WriteMethod(indent + prefix + ": see " + lolzo.Item1 + " property.");
                    return;
                }

                if (T2.IsArray)
                {
                    this.WriteMethod(indent + prefix + ": array");
                    ProcessArray(member: member, array: (dynamic)value, indent: indent + "   ", indexString: "");
                    this.CheckedProperties.Add(new Tuple<string, object>(member.Name, value));
                    return;
                }

                this.WriteMethod(indent + prefix + ": " + value);
                this.PrintValues(value, indent);
                this.CheckedProperties.Add(new Tuple<string, object>(member.Name, value));
            }
        }
    }
}
