namespace JonUtility
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Security;
    using System.Text;
    using System.Windows.Forms;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Schema;
    using Formatting = Newtonsoft.Json.Formatting;
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
        ///     Returns a random number between 0 and 1, with the specified number of digits between 1 and 9 (defaults to 2)
        /// </summary>
        /// <param name="digits"></param>
        /// <returns>The number of digit places in the return value, between 1 and 9 (defaults to 2).</returns>
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
                            new Type[1] { typeof(string) },
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

    public static class ExtensionMethods
    {
        public static SecureString ConvertToSecureString(this string @this)
        {
            unsafe
            {
                fixed (char* c = @this)
                {
                    SecureString ss = new SecureString(c, @this.Length);
                    ss.MakeReadOnly();
                    return ss;
                }
            }
        }

        public static string ConvertToString(this SecureString @this)
        {
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToGlobalAllocUnicode(@this);
                unsafe
                {
                    char* c = (char*)ptr;
                    return new string(c, 0, @this.Length);
                }
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }
            }
        }

        public static void CenterForm(this Form @this, int width, int height)
        {
            @this.SetBounds(
                (Screen.PrimaryScreen.WorkingArea.Width - width) / 2,
                (Screen.PrimaryScreen.WorkingArea.Height - height) / 2,
                width,
                height);
        }

        public static string InsertReplace(this string @this, int insertionIndex, int deletionCount, string textToInsert)
        {
            @this = @this.Remove(insertionIndex, deletionCount);
            @this = @this.Insert(insertionIndex, textToInsert);
            return @this;
        }

        public static bool IsNaNorInfinity(this Double @this)
        {
            return Double.IsNaN(@this) || Double.IsInfinity(@this);
        }

        public static void TraceError(this TraceSource @this, Exception ex)
        {
            string data;
            if (ex is AggregateException)
            {
                var errors = from error in ((AggregateException)ex).Flatten().InnerExceptions
                             select error.GetType().Name + ": " + error.Message;
                data = string.Join(Environment.NewLine, errors);
            }
            else
            {
                data = ex.Message;
            }
            @this.TraceData(TraceEventType.Error, 0, data);
        }

        public static void SafeRaise(this EventHandler @this, object sender)
        {
            if (@this != null)
            {
                @this(sender, EventArgs.Empty);
            }
        }

        public static void SafeRaise<T>(this EventHandler<T> @this, object sender, T eventArgs) where T : EventArgs
        {
            if (@this != null)
            {
                @this(sender, eventArgs);
            }
        }

        public static void TraceMethodTime(this TraceSource @this, long initialTime,
            [CallerMemberName] string caller = "")
        {
            long time2 = Stopwatch.GetTimestamp();
            @this.TraceInformation(string.Format("{0}: {1}", caller, StringFunctions.TicksToMS(time2 - initialTime, 2)));
        }

        public static object GetDefaultValue(this Type @this)
        {
            if (@this.IsValueType)
            {
                return Activator.CreateInstance(@this);
            }
            return null;
        }
    }

    public static class MathFunctions
    {
        public enum BirthdayStyle
        {
            Exact = 0,
            Approximation1 = 1
        }

        public static double EventsUntilCollisionByProbability(double possibleOutcomes, double probability)
        {
            double logPart = 1d / (1d - probability);
            double logProb = Math.Log(logPart);
            double part = 2d * possibleOutcomes * logProb;
            double sqrt = Math.Sqrt(part);
            return sqrt;
        }

        public static double BirthdayParadox(BigInteger x, long n, BirthdayStyle style = BirthdayStyle.Exact)
        {
            switch (style)
            {
                case BirthdayStyle.Exact:
                    BigInteger xFactorial = MathFunctions.Factorial(x);
                    BigInteger xminusnFactorial = MathFunctions.Factorial(x - n);
                    BigInteger xPowN = BigInteger.Pow(x, (int)n);
                    BigInteger div1 = BigInteger.Divide(xFactorial, xminusnFactorial);
                    BigInteger div2 = BigInteger.Divide(div1 * 10000, xPowN);
                    double final = (double)div2 / 10000d;
                    return (1d - final);
                case BirthdayStyle.Approximation1:
                    double negativeNSquared = -1d * (n * (n - 1d));
                    double xTimes2 = (double)x * 2d;
                    double exponent = negativeNSquared / xTimes2;
                    double eToExponent = Math.Exp(exponent);
                    return 1 - eToExponent;
                default:
                    return -1;
            }
        }

        public static BigInteger Factorial(BigInteger number)
        {
            if (number < 0)
            {
                throw new ArgumentException("Number must be positive.");
            }
            if (number == 0 || number == 1)
            {
                return new BigInteger(1);
            }
            BigInteger bi = new BigInteger(1);
            BigInteger count = number + 1;
            for (BigInteger i = 2; i < count; i++)
            {
                BigInteger temp = i;
                bi = BigInteger.Multiply(bi, temp);
            }
            return bi;
        }
    }

    public static class Serialization
    {
        public enum DataContractKind
        {
            Xml = 0,
            Json = 1
        }

        public static byte[] SerializeLegacyObject(object objectToSerialize, DataContractKind dataContractKind,
            params Type[] legacyTypes)
        {
            if (dataContractKind == DataContractKind.Xml)
            {
                DataContractSerializerSettings settings = new DataContractSerializerSettings();
                settings.DataContractSurrogate = new DataContractTypeSurrogate(dataContractKind, legacyTypes);
                DataContractSerializer serializer = new DataContractSerializer(objectToSerialize.GetType(), settings);

                using (MemoryStream ms = new MemoryStream())
                {
                    serializer.WriteObject(ms, objectToSerialize);
                    byte[] serializedBytes = ms.ToArray();
                    return serializedBytes;
                }
            }
            else
            {
                DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
                settings.DataContractSurrogate = new DataContractTypeSurrogate(dataContractKind, legacyTypes);
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(
                    objectToSerialize.GetType(),
                    settings);

                using (MemoryStream ms = new MemoryStream())
                {
                    serializer.WriteObject(ms, objectToSerialize);
                    byte[] serializedBytes = ms.ToArray();
                    return serializedBytes;
                }
            }
        }

        public static T DeserializeLegacyObject<T>(byte[] bytesToDeserialize, DataContractKind dataContractKind,
            params Type[] legacyTypes)
        {
            if (dataContractKind == DataContractKind.Xml)
            {
                DataContractSerializerSettings settings = new DataContractSerializerSettings();
                settings.DataContractSurrogate = new DataContractTypeSurrogate(dataContractKind, legacyTypes);
                DataContractSerializer serializer = new DataContractSerializer(typeof(T), settings);

                using (MemoryStream ms2 = new MemoryStream(bytesToDeserialize))
                {
                    object o = serializer.ReadObject(ms2);
                    T t = (T)o;
                    return t;
                }
            }
            else
            {
                DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
                settings.DataContractSurrogate = new DataContractTypeSurrogate(dataContractKind, legacyTypes);
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T), settings);

                using (MemoryStream ms2 = new MemoryStream(bytesToDeserialize))
                {
                    object o = serializer.ReadObject(ms2);
                    T t = (T)o;
                    return t;
                }
            }
        }

        public static T DeserializeJsonIfValid<T>(string jsonstring, string schemafile) where T : class
        {
            T obj = null;

            JSchema schema = null;
            using (StreamReader sr = new StreamReader(schemafile, Encoding.UTF8))
            {
                using (JsonTextReader jtr = new JsonTextReader(sr))
                {
                    schema = JSchema.Load(jtr);
                }
            }
            using (var tr = new StringReader(jsonstring))
            {
                using (var jr = new JsonTextReader(tr))
                {
                    using (var validatingReader = new JSchemaValidatingReader(jr))
                    {
                        int ErrorCount = 0;
                        validatingReader.Schema = schema;
                        validatingReader.ValidationEventHandler += (o, ev) =>
                        {
                            ErrorCount++;
                            Console.WriteLine(ev.Message);
                        };
                        JsonSerializer serializer = new JsonSerializer();
                        obj = serializer.Deserialize<T>(validatingReader);

                        //while (validatingReader.Read()) { }
                        if (ErrorCount != 0)
                        {
                            Console.WriteLine(ErrorCount + " errors validating JSON.");
                            return null;
                        }
                    }
                }
            }
            return obj;
        }

        public static bool GenerateJsonSchema(Type T, string path, bool AllowAdditionalProperties = false)
        {
            var jgen = new JsonSchemaGenerator();
            var schema = jgen.Generate(T, false);
            schema.Title = T.Name;
            try
            {
                using (var writer = new StringWriter())
                {
                    using (var jsonTextWriter = new JsonTextWriter(writer))
                    {
                        using (var fileWriter = new StreamWriter(path, false, Encoding.UTF8))
                        {
                            schema.WriteTo(jsonTextWriter);
                            JsonSerializerSettings jss = new JsonSerializerSettings();
                            jss.Formatting = Formatting.Indented;
                            jss.CheckAdditionalContent = true;
                            jss.MissingMemberHandling = MissingMemberHandling.Error;
                            object parsedJson = JsonConvert.DeserializeObject(writer.ToString());
                            string jsonString = JsonConvert.SerializeObject(parsedJson, jss);
                            if (!AllowAdditionalProperties)
                            {
                                int indx = jsonString.Substring(0, jsonString.Length - 2).LastIndexOf('}') + 1;
                                jsonString = jsonString.Substring(0, indx) + ", " + Environment.NewLine +
                                             "  'additionalProperties': false" + Environment.NewLine + "}";
                            }
                            fileWriter.WriteLine(jsonString);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public static T DeserializeXMLIfValid<T>(string xmlstring, string schemafile) where T : class
        {
            XmlReaderSettings xrs = new XmlReaderSettings();
            XmlSchemaSet xschemas = new XmlSchemaSet();
            T Result = null;
            string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            bool Errors = false;
            if (xmlstring.StartsWith(_byteOrderMarkUtf8))
            {
                xmlstring = xmlstring.Remove(0, _byteOrderMarkUtf8.Length);
            }
            xschemas.Add("", schemafile);
            xrs.Schemas = xschemas;
            xrs.ValidationType = ValidationType.Schema;
            xrs.ValidationFlags = XmlSchemaValidationFlags.ProcessIdentityConstraints |
                                  XmlSchemaValidationFlags.ReportValidationWarnings;
            xrs.ValidationEventHandler += (o, ev) =>
            {
                Errors = true;
                Console.WriteLine(ev.Message);
            };

            XmlSerializer xs = new XmlSerializer(typeof(T));
            using (XmlReader xr = XmlReader.Create(new StringReader(xmlstring), xrs))
            {
                // Read through document to verify it complies with the schema.                
                try
                {
                    Result = (T)xs.Deserialize(xr);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error deserializing XML: " + ex.Message);
                    return null;
                }
                return (Errors) ? null : Result;
            }
        }

        public static bool GenerateXMLSchema(Type T, string path)
        {
            if (!path.EndsWith(".xsd"))
            {
                path += T.Name + ".xsd";
            }
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: Could not delete old schema file " + path + ": " + ex.Message);
                    return false;
                }
            }
            try
            {
                var schemas = new XmlSchemas();
                var exporter = new XmlSchemaExporter(schemas);
                var mapping = new XmlReflectionImporter().ImportTypeMapping(T);
                exporter.ExportTypeMapping(mapping);
                var schemaWriter = new StringWriter();
                if (string.IsNullOrEmpty(path))
                {
                    path = "";
                }

                using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
                {
                    foreach (XmlSchema schema in schemas)
                    {
                        schema.Write(sw);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error generating xml schema for type " + T.Name + ": " + ex.Message);
                return false;
            }
        }

        public static bool GenerateXMLSchema(object o, string path)
        {
            Contract.Requires<ArgumentNullException>(o != null, "Object cannot be null.");
            return Serialization.GenerateXMLSchema(o.GetType(), path);
        }

        public static void SerializeUsingReflection(Stream outputStream, object objectToSeralize)
        {
            if (objectToSeralize == null)
            {
                return;
            }

            //objectToSeralize = new LolZo(5) { DerpoInt = 6664 };

            var type = objectToSeralize.GetType();
            var ctor = type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            // error
            if (ctor == null)
            {
            }

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var knownTypes = new List<Type>();

            foreach (var field in fields)
            {
                object value = field.GetValue(objectToSeralize);
                Serialization.PopulateFieldTypes(knownTypes, value);

                var serializer = new DataContractJsonSerializer(field.FieldType, knownTypes);

                if (value == null)
                {
                    var nullBytes = Encoding.UTF8.GetBytes("null");
                    outputStream.Write(nullBytes, (int)outputStream.Position, nullBytes.Length);
                }
                else
                {
                    serializer.WriteObject(outputStream, value);
                }

                //writer.Write((char)20);
            }
            // }
        }

        private static Action<S, T> CreateSetter<S, T>(FieldInfo field)
        {
            string methodName = field.ReflectedType.FullName + ".set_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, null, new Type[2] { typeof(S), typeof(T) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            if (field.IsStatic)
            {
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stfld, field);
            }
            gen.Emit(OpCodes.Ret);
            return (Action<S, T>)setterMethod.CreateDelegate(typeof(Action<S, T>));
        }

        private static object GetFieldValue(FieldInfo field, object objectContainingField)
        {
            var fieldType = field.FieldType;
            var objType = objectContainingField.GetType();

            DynamicMethod setterMethod = new DynamicMethod(string.Empty, fieldType, new Type[1] { objType }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, field);
            gen.Emit(OpCodes.Ret);

            var outputDelegate = setterMethod.CreateDelegate(
                typeof(Func<>).MakeGenericType(fieldType),
                objectContainingField);
            var value = outputDelegate.DynamicInvoke(null);

            return value;
        }

        private static void PopulateFieldTypes(List<Type> knownTypes, object obj)
        {
            var objType = obj.GetType();
            if (!knownTypes.Contains(objType))
            {
                knownTypes.Add(objType);
            }

            foreach (
                var field in objType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object value = field.GetValue(obj);
                if (value != null && !knownTypes.Contains(value.GetType()))
                {
                    Serialization.PopulateFieldTypes(knownTypes, value);
                }
            }
        }

        internal class DataContractTypeSurrogate : IDataContractSurrogate
        {
            public DataContractTypeSurrogate(DataContractKind kind, Type[] types)
            {
                this.Kind = kind;
                this.Types = types;
            }

            public DataContractKind Kind { get; protected set; }

            public Type[] Types { get; protected set; }

            public object GetCustomDataToExport(Type clrType, Type dataContractType)
            {
                throw new NotImplementedException();
            }

            public object GetCustomDataToExport(MemberInfo memberInfo, Type dataContractType)
            {
                throw new NotImplementedException();
            }

            public Type GetDataContractType(Type type)
            {
                Type T = this.Types.FirstOrDefault(t => t == type);
                if (T == null)
                {
                    return type; // not a special case
                }
                return typeof(LegacySurrogate);
            }

            public object GetDeserializedObject(object obj, Type targetType)
            {
                if (obj is LegacySurrogate && targetType != typeof(LegacySurrogate))
                {
                    LegacySurrogate surrogate = (LegacySurrogate)obj;
                    using (var sr = new StringReader(surrogate.SerializedString))
                    {
                        if (this.Kind == DataContractKind.Xml)
                        {
                            using (var reader = XmlReader.Create(sr))
                            {
                                XmlSerializer serializer = new XmlSerializer(targetType);
                                object o = serializer.Deserialize(reader);
                                return o;
                            }
                        }
                        using (var reader = new JsonTextReader(sr))
                        {
                            JsonSerializer json = new JsonSerializer();
                            object o = json.Deserialize(reader, targetType);
                            return o;
                        }
                    }
                }
                return obj;
            }

            public void GetKnownCustomDataTypes(Collection<Type> customDataTypes)
            {
                throw new NotImplementedException();
            }

            public object GetObjectToSerialize(object obj, Type targetType)
            {
                if (targetType == typeof(LegacySurrogate))
                {
                    using (var sw = new StringWriter())
                    {
                        if (this.Kind == DataContractKind.Xml)
                        {
                            using (var writer = XmlWriter.Create(sw))
                            {
                                XmlSerializer serializer = new XmlSerializer(obj.GetType());
                                serializer.Serialize(writer, obj);
                            }
                        }
                        else
                        {
                            using (var writer = new JsonTextWriter(sw))
                            {
                                JsonSerializer serializer = new JsonSerializer();
                                serializer.Serialize(writer, obj);
                            }
                        }
                        return new LegacySurrogate(sw.ToString());
                    }
                }
                return obj;
            }

            public Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
            {
                throw new NotImplementedException();
            }

            public CodeTypeDeclaration ProcessImportedType(CodeTypeDeclaration typeDeclaration,
                CodeCompileUnit compileUnit)
            {
                return typeDeclaration;
            }
        }

        [DataContract]
        internal class LegacySurrogate
        {
            public LegacySurrogate()
            {
            }

            public LegacySurrogate(string serializedString)
            {
                this.SerializedString = serializedString;
            }

            [DataMember(Name = "Data")]
            public string SerializedString { get; set; }
        }

        private class LolZo
        {
            private int otherInt;

            public LolZo(int nope)
            {
                this.Yeshkin = nope;
                this.otherInt = nope * 2 + 1;
            }

            private LolZo()
            {
            }

            public int DerpoInt { get; set; }

            public int Yeshkin { get; }
        }
    }

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

    public static class Utility
    {
        public static bool TraceMethodExceptions(Action a, bool traceSuccessMessage = false)
        {
            try
            {
                long time1 = 0, time2 = 0;
                Diagnostics.QueryPerformanceCounter(ref time1);
                a();
                Diagnostics.QueryPerformanceCounter(ref time2);
                if (traceSuccessMessage)
                {
                    Trace.WriteLine(
                        string.Format(
                            "{0}.{1})(): Success ({2})",
                            a.Method.DeclaringType.Name,
                            a.Method.Name,
                            SF.TicksToMS(time2 - time1, 2)));
                }
                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Trace.WriteLine("Error in " + a.Method.DeclaringType.Name + "." + a.Method.Name + "(): " + ex.Message);
                Trace.WriteLine("    Stack Trace: " + ex.StackTrace);
#elif TRACE
                Trace.WriteLine("Error in " + a.Method.DeclaringType.Name + "." + a.Method.Name + "(): " + ex.Message);
#endif
                throw;
            }
        }

        public static void TraceMessage(string message, [CallerLineNumber] int lineNumber = 0,
            [CallerMemberName] string caller = null, [CallerFilePath] string filePath = "")
        {
            Trace.WriteLine(
                string.Format(
                    "Message: \"{0}\" [{1}, {2}, line {3}] line {1} ({2})",
                    message,
                    filePath.Substring(filePath.LastIndexOf('\\') + 1),
                    caller,
                    lineNumber));
        }

        /// <summary>
        ///     Use this to always return a Color from a Color's Name property, including when the Color's Name is a 4-digit hex
        ///     string (doesn't work with Color.FromName, which expects an enum name).
        /// </summary>
        /// <param name="name">The name of the color, as a 4-digit hex string or common/enum name.</param>
        /// <returns></returns>
        public static Color GetColorFromName(string name)
        {
            Color returnColor = Color.FromName(name);
            string lol = returnColor.ToString();
            if (returnColor.A == 0 && returnColor.B == 0 && returnColor.G == 0 && returnColor.R == 0)
            // invisible color, results from no match found in FromName()
            {
                var bytes =
                    Enumerable.Range(0, name.Length)
                              .Where(i => i % 2 == 0)
                              .Select(i => Convert.ToByte(name.Substring(i, 2), 16))
                              .ToArray();
                returnColor = Color.FromArgb(bytes[0], bytes[1], bytes[2], bytes[3]);
            }
            return returnColor;
        }

        public static T NewControl<T>(Control parent, string text, int left, int top, int width = -1, int height = -1)
            where T : Control, new()
        {
            T newControl = Activator.CreateInstance<T>();
            newControl.Parent = parent;
            newControl.Text = text;
            if (width > -1 && height > -1)
            {
                newControl.SetBounds(left, top, width, height);
            }
            else
            {
                newControl.Location = new Point(left, top);
            }
            return newControl;
        }
    }
}

namespace JonUtility
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    public static class WpfExtensionMethods
    {
        private static readonly MethodInfo removeLogicalChildMethod;
        private static readonly MethodInfo removeVisualChildMethod;

        static WpfExtensionMethods()
        {
            WpfExtensionMethods.removeVisualChildMethod = typeof(FrameworkElement).GetMethod(
                "RemoveVisualChild",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(Visual) },
                null);

            WpfExtensionMethods.removeLogicalChildMethod = typeof(FrameworkElement).GetMethod(
                "RemoveLogicalChild",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(object) },
                null);
        }

        public static bool ForceRemoveChild(this FrameworkElement @this, object child)
        {
            if (@this == null || child == null)
            {
                return false;
            }

            if (WpfExtensionMethods.removeLogicalChildMethod == null ||
                WpfExtensionMethods.removeVisualChildMethod == null)
            {
                return false;
            }

            try
            {
                if (WpfExtensionMethods.removeLogicalChildMethod != null)
                {
                    WpfExtensionMethods.removeLogicalChildMethod.Invoke(@this, new[] { child });
                }

                if (WpfExtensionMethods.removeVisualChildMethod != null)
                {
                    WpfExtensionMethods.removeVisualChildMethod.Invoke(@this, new object[] { (Visual)child });
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Color GetOffsetColor(this Color @this, int offset, bool applyToAlpha = false)
        {
            byte alpha = applyToAlpha ? WpfExtensionMethods.GetByte(@this.A, offset) : @this.A;
            byte red = WpfExtensionMethods.GetByte(@this.R, offset);
            byte green = WpfExtensionMethods.GetByte(@this.G, offset);
            byte blue = WpfExtensionMethods.GetByte(@this.B, offset);

            return Color.FromArgb(alpha, red, green, blue);
        }

        public static RenderTargetBitmap ConvertToBitmap(UIElement uiElement, double resolution)
        {
            var scale = resolution / 96d;

            uiElement.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            var sz = uiElement.DesiredSize;
            var rect = new Rect(sz);
            uiElement.Arrange(rect);

            var bmp = new RenderTargetBitmap(
                (int)(scale * (rect.Width)),
                (int)(scale * (rect.Height)),
                scale * 96,
                scale * 96,
                PixelFormats.Default);
            bmp.Render(uiElement);

            return bmp;
        }

        public static void ConvertToJpeg(UIElement uiElement, string path, double resolution)
        {
            var jpegString = WpfExtensionMethods.CreateJpeg(WpfExtensionMethods.ConvertToBitmap(uiElement, resolution));

            if (path != null)
            {
                try
                {
                    using (var fileStream = File.Create(path))
                    {
                        using (var streamWriter = new StreamWriter(fileStream, Encoding.Default))
                        {
                            streamWriter.Write(jpegString);
                            streamWriter.Close();
                        }

                        fileStream.Close();
                    }
                }
                catch
                {
                }
            }
        }

        public static string CreateJpeg(RenderTargetBitmap bitmap)
        {
            var jpeg = new JpegBitmapEncoder();
            jpeg.Frames.Add(BitmapFrame.Create(bitmap));
            string result;

            using (var memoryStream = new MemoryStream())
            {
                jpeg.Save(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                using (var streamReader = new StreamReader(memoryStream, Encoding.Default))
                {
                    result = streamReader.ReadToEnd();
                    streamReader.Close();
                }

                memoryStream.Close();
            }

            return result;
        }

        private static byte GetByte(byte initialValue, int offset)
        {
            int value = initialValue + offset;
            if (value < 0)
            {
                return 0;
            }
            if (value > 255)
            {
                return 255;
            }
            return (byte)value;
        }
    }
}

namespace JonUtility
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;

    public struct PointInt : IComparable, IComparable<PointInt>, IEquatable<PointInt>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PointInt" /> struct.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        public PointInt(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        ///     Gets the X value.
        /// </summary>
        /// <value>The x value.</value>
        public int X { get; }

        /// <summary>
        ///     Gets the Y value.
        /// </summary>
        /// <value>The y value.</value>
        public int Y { get; }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return this.X + ", " + this.Y;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is PointInt))
            {
                return false;
            }

            return this.Equals((PointInt)obj);
        }

        public bool Equals(PointInt other)
        {
            return (this.X == other.X) && (this.Y == other.Y);
        }

        public int CompareTo(PointInt other)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Implements the == operator between <see cref="PointInt"/> instances.
        /// </summary>
        /// <param name="left">The left-hand instance.</param>
        /// <param name="right">The right-hand instance.</param>
        /// <returns>True if the instaces are equal; false otherwise.</returns>
        public static bool operator ==(PointInt left, PointInt right)
        {

            return (left.X == right.X) && (left.Y == right.Y);
        }

        public static bool operator !=(PointInt left, PointInt right)
        {
            return (left.X != right.X) || (left.Y != right.Y);
        }
    }

    /// <summary>
    ///     Provides a generic container for two readonly same-type struct values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Point<T> : IEquatable<Point<T>> where T : struct, IEquatable<T>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Point{T}" /> struct.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        public Point(T x, T y)
        {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        ///     Gets the X value.
        /// </summary>
        /// <value>The x value.</value>
        public T X { get; }

        /// <summary>
        ///     Gets the Y value.
        /// </summary>
        /// <value>The y value.</value>
        public T Y { get; }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return this.X + ", " + this.Y;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Point<T>))
            {
                return false;
            }

            return this == (Point<T>)obj;
        }

        public static bool operator ==(Point<T> left, Point<T> right)
        {
            return left.X.Equals(right.X) && left.Y.Equals(right.Y);
        }

        public static bool operator !=(Point<T> left, Point<T> right)
        {
            return !left.X.Equals(right.X) || !left.Y.Equals(right.Y);
        }

        public static explicit operator JonUtility.Point<byte>(JonUtility.Point<T> point)
        {
            return ConvertPoint<byte>(point);
        }

        public static explicit operator JonUtility.Point<short>(JonUtility.Point<T> point)
        {
            return ConvertPoint<short>(point);
        }

        public static explicit operator JonUtility.Point<int>(JonUtility.Point<T> point)
        {
            return ConvertPoint<int>(point);
        }

        public static explicit operator JonUtility.Point<long>(JonUtility.Point<T> point)
        {
            return ConvertPoint<long>(point);
        }

        public static explicit operator JonUtility.Point<float>(JonUtility.Point<T> point)
        {
            return ConvertPoint<float>(point);
        }

        public static explicit operator JonUtility.Point<double>(JonUtility.Point<T> point)
        {
            return ConvertPoint<double>(point);
        }

        public static explicit operator System.Windows.Point(JonUtility.Point<T> point)
        {
            if (typeof(T).IsPrimitive)
            {
                return new System.Windows.Point(
                    (double)Convert.ChangeType(point.X, typeof(double)),
                    (double)Convert.ChangeType(point.Y, typeof(double)));
            }

            return default(System.Windows.Point);
        }

        public static explicit operator System.Drawing.Point(JonUtility.Point<T> point)
        {
            if (typeof(T).IsPrimitive)
            {
                return new System.Drawing.Point(
                    (int)Convert.ChangeType(point.X, typeof(int)),
                    (int)Convert.ChangeType(point.Y, typeof(int)));
            }

            return default(System.Drawing.Point);
        }

        public static explicit operator System.Drawing.PointF(JonUtility.Point<T> point)
        {
            if (typeof(T).IsPrimitive)
            {
                return new System.Drawing.PointF(
                    (float)Convert.ChangeType(point.X, typeof(float)),
                    (float)Convert.ChangeType(point.Y, typeof(float)));
            }

            return default(System.Drawing.PointF);
        }

        /// <summary>
        ///     Converts the point parameter into a new <see cref="Point{T}"/> instance. This method only converts between primitive numeric types; if the target type is not a primitve numeric type, then the default value is returned instead.
        /// </summary>
        /// <typeparam name="T2">The type of <see cref="Point{T}"/> to return.</typeparam>
        /// <param name="point">The point to convert.</param>
        /// <returns>A new <see cref="Point{T2}"/> instance.</returns>
        /// <exception cref="System.InvalidCastException">This conversion is not supported. Only <see cref="Point{T}"/> instances containing primitive numeric types can be converted./></exception>
        /// <exception cref="System.FormatException">One or both of the point's values are not in a format recognized by the target type.</exception>
        /// <exception cref="System.OverflowException">One or both of the point's values represents a number that is out of the range of the target type.</exception>
        public static Point<T2> ConvertPoint<T2>(Point<T> point) where T2 : struct, IEquatable<T2>
        {
            if (typeof(T).IsPrimitive && typeof(T2).IsPrimitive)
            {
                return new Point<T2>(
                    (T2)Convert.ChangeType(point.X, typeof(T2)),
                    (T2)Convert.ChangeType(point.Y, typeof(T2)));
            }

            return default(Point<T2>);
        }

        /// <summary>
        ///     Indicates whether the current <see cref="Point{T}"/> is equal to another <see cref="Point{T}"/> of the same generic type.
        /// </summary>
        /// <param name="other">An <see cref="Point{T}"/> to compare with this <see cref="Point{T}"/>.</param>
        /// <returns><c>true</c> if the current <see cref="Point{T}"/> instance is equal to the other parameter; otherwise, <c>false</c>.</returns>
        public bool Equals(Point<T> other)
        {
            return this == other;
        }
    }

    public sealed class WinTimer : IDisposable
    {
        private const UInt32 EVENT_TYPE = 1; // + 0x100;  // TIME_KILL_SYNCHRONOUS causes a hang ?!
        private readonly uint interval;
        private readonly SendOrPostCallback procMethod;
        private readonly SynchronizationContext syncContext;
        private readonly TimerEventHandler timerDelegate;
        private bool disposed;
        private bool stopRequested;
        private uint timerID;

        /// <summary>
        ///     Initializes a new instance of the <see cref="WinTimer" /> class.
        /// </summary>
        /// <param name="action">The action delegate to execute on each proc.</param>
        /// <param name="interval">The timer interval between procs.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action" /> is <see langword="null" />.</exception>
        public WinTimer(Action action, int interval)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            this.syncContext = SynchronizationContext.Current;
            this.interval = (uint)interval;
            this.procMethod = o => action();
            this.timerDelegate = this.TimerCallback;
        }

        ~WinTimer()
        {
            this.OnDispose(false);
        }

        private delegate void TimerEventHandler(int id, int msg, IntPtr user, int dw1, int dw2);

        public void Dispose()
        {
            this.OnDispose(true);
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            this.stopRequested = false;
            WinTimer.timeBeginPeriod(1);
            this.timerID = WinTimer.timeSetEvent(
                this.interval,
                0,
                this.timerDelegate,
                UIntPtr.Zero,
                WinTimer.EVENT_TYPE);
        }

        public void Stop()
        {
            if (this.stopRequested)
            {
                return;
            }

            this.stopRequested = true;

            if (this.timerID != 0)
            {
                WinTimer.timeKillEvent(this.timerID);
                WinTimer.timeEndPeriod(1);
                Thread.Sleep(0);
            }
        }

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        private static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        private static extern uint timeEndPeriod(uint uPeriod);

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        private static extern uint timeGetTime();

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        private static extern uint timeKillEvent(uint uTimerID);

        [DllImport("Winmm.dll", CharSet = CharSet.Auto)]
        private static extern uint timeSetEvent(uint uDelay, uint uResolution, TimerEventHandler lpTimeProc,
            UIntPtr dwUser, uint fuEvent);

        private void OnDispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.Stop();
        }

        private void TimerCallback(int id, int msg, IntPtr user, int dw1, int dw2)
        {
            if (this.stopRequested)
            {
                return;
            }

            if (this.timerID != 0)
            {
                this.syncContext.Post(this.procMethod, null);
            }
        }
    }
}
