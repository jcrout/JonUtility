namespace JonUtility
{
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.IO;
    using System.Runtime;
    using System.Runtime.InteropServices;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Xml.Serialization;
    using System.Xml;
    using System.Xml.Schema;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Schema;
    using System.Drawing;
    using System.Runtime.CompilerServices;
    using System.Windows.Forms;
    using System.Numerics;
    using System.Runtime.Serialization;
    using System.CodeDom;
    using System.Runtime.Serialization.Json;
    using System.Security;

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
                    Trace.WriteLine(
                        string.Format("{0}.{1})(): Success ({2})", 
                        a.Method.DeclaringType.Name,
                        a.Method.Name,
                        TicksToMS(time2 - time1, 2)));
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

        public static void TraceMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null, [CallerFilePath] string filePath = "")
        {
            Trace.WriteLine(string.Format("Message: \"{0}\" [{1}, {2}, line {3}] line {1} ({2})",
                message,
                filePath.Substring(filePath.LastIndexOf('\\') + 1),
                caller,
                lineNumber));
        }

        public static string TicksToMS(long ticks, int digits = 2, string showUnit = "ms")
        {
            return ((double)ticks / (double)Stopwatch.Frequency * 1000d).ToString("0.".PadRight(digits + 2, '0')) + showUnit;
        }

        public static string TicksToSeconds(long ticks, int digits = 2, string showUnit = "s")
        {
            return ((double)ticks / (double)Stopwatch.Frequency).ToString("0.".PadRight(digits + 2, '0')) + showUnit;
        }

        public static string GetByteSizeString(long bytes, int decimalDigits = 2, bool fullUnitName = false)
        {
            const long KilobyteAmount = 1024L;
            const long MegabyteAmount = KilobyteAmount * KilobyteAmount;
            const long GigabyteAmount = MegabyteAmount * KilobyteAmount;
            const long TerabyteAmount = GigabyteAmount * KilobyteAmount;
            const long PetabyteAmount = TerabyteAmount * KilobyteAmount;

            if (bytes < KilobyteAmount)
                return bytes.ToString() + (!fullUnitName ? " B" : " Bytes");

            string padding = decimalDigits == 0 ? "0" : "0.".PadRight(decimalDigits + 2, '0');

            if (bytes < MegabyteAmount)
                return Math.Round((decimal)bytes / KilobyteAmount, decimalDigits).ToString(padding) + (!fullUnitName ? " KB" : " Kiloytes");
            else if (bytes < GigabyteAmount)
                return Math.Round((decimal)bytes / MegabyteAmount, decimalDigits).ToString(padding) + (!fullUnitName ? " MB" : " Megabytes");
            else if (bytes < TerabyteAmount)
                return Math.Round((decimal)bytes / GigabyteAmount, decimalDigits).ToString(padding) + (!fullUnitName ? " GB" : " Gigabytes");
            else if (bytes < PetabyteAmount)
                return Math.Round((decimal)bytes / TerabyteAmount, decimalDigits).ToString(padding) + (!fullUnitName ? " TB" : " Terabytes");
            else
                return Math.Round((decimal)bytes / PetabyteAmount, decimalDigits).ToString(padding) + (!fullUnitName ? " PB" : " Petabytes");
        }

        /// <summary>
        /// Use this to always return a Color from a Color's Name property, including when the Color's Name is a 4-digit hex string (doesn't work with Color.FromName, which expects an enum name).
        /// </summary>
        /// <param name="name">The name of the color, as a 4-digit hex string or common/enum name.</param>
        /// <returns></returns>
        public static Color GetColorFromName(string name)
        {
            Color returnColor = Color.FromName(name);
            string lol = returnColor.ToString();
            if (returnColor.A == 0 && returnColor.B == 0 && returnColor.G == 0 && returnColor.R == 0) // invisible color, results from no match found in FromName()
            {
                var bytes = Enumerable.Range(0, name.Length).Where(i => i % 2 == 0).Select(i => Convert.ToByte(name.Substring(i, 2), 16)).ToArray();
                returnColor = Color.FromArgb(bytes[0], bytes[1], bytes[2], bytes[3]);
            }
            return returnColor;
        }

        public static T NewControl<T>(Control parent, string text, int left, int top, int width = -1, int height = -1) where T : Control
        {
            T newControl = Activator.CreateInstance<T>();
            newControl.Parent = parent;
            newControl.Text = text;
            if (width != -1)
            {
                newControl.SetBounds(left, top, width, height);
            }
            else
            {
                newControl.Location = new Point(left, top);
            }
            return newControl;
        }

        public static void CenterForm(this Form frm, int width, int height)
        {
            frm.SetBounds((int)((Screen.PrimaryScreen.WorkingArea.Width - width) / 2), (int)((Screen.PrimaryScreen.WorkingArea.Height - height) / 2), width, height);
        }
    }

    public static class Serialization
    {
        [DataContract]
        internal class LegacySurrogate
        {
            [DataMember(Name = "Data")]
            public string SerializedString { get; set; }

            public LegacySurrogate() { }

            public LegacySurrogate(string serializedString) { this.SerializedString = serializedString; }
        }

        internal class DataContractTypeSurrogate : IDataContractSurrogate
        {
            private Type[] _Types;
            public Type[] Types
            {
                get { return _Types; }
                protected set { _Types = value; }
            }

            private DataContractKind _Kind;
            public DataContractKind Kind
            {
                get { return _Kind; }
                protected set { _Kind = value; }
            }

            public DataContractTypeSurrogate(DataContractKind kind, Type[] types)
            {
                this._Kind = kind;
                this._Types = types;
            }

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
                Type T = _Types.FirstOrDefault(t => t == type);
                if (T == null) return type; // not a special case
                return typeof(LegacySurrogate);
            }

            public object GetDeserializedObject(object obj, Type targetType)
            {
                if (obj is LegacySurrogate && targetType != typeof(LegacySurrogate))
                {
                    LegacySurrogate surrogate = (LegacySurrogate)obj;
                    using (var sr = new StringReader(surrogate.SerializedString))
                    {
                        if (_Kind == DataContractKind.Xml)
                        {
                            using (var reader = XmlReader.Create(sr))
                            {
                                XmlSerializer serializer = new XmlSerializer(targetType);
                                object o = serializer.Deserialize(reader);
                                return o;
                            }
                        }
                        else
                        {
                            using (var reader = new JsonTextReader(sr))
                            {
                                JsonSerializer json = new JsonSerializer();
                                object o = json.Deserialize(reader, targetType);
                                return o;
                            }
                        }
                    }
                }
                return obj;
            }

            // Not Implemented
            public void GetKnownCustomDataTypes(System.Collections.ObjectModel.Collection<Type> customDataTypes)
            {
                throw new NotImplementedException();
            }

            public object GetObjectToSerialize(object obj, Type targetType)
            {
                if (targetType == typeof(LegacySurrogate))
                {
                    using (var sw = new StringWriter())
                    {
                        if (_Kind == DataContractKind.Xml)
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

            // Not Implemented
            public Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
            {
                throw new NotImplementedException();
            }

            // Not Implemented
            public CodeTypeDeclaration ProcessImportedType(CodeTypeDeclaration typeDeclaration, CodeCompileUnit compileUnit)
            {
                return typeDeclaration;
            }
        }

        public enum DataContractKind
        {
            Xml = 0,
            Json = 1
        }

        public static byte[] SerializeLegacyObject(object objectToSerialize, DataContractKind dataContractKind, params Type[] legacyTypes)
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
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(objectToSerialize.GetType(), settings);

                using (MemoryStream ms = new MemoryStream())
                {
                    serializer.WriteObject(ms, objectToSerialize);
                    byte[] serializedBytes = ms.ToArray();
                    return serializedBytes;
                }
            }
        }

        public static T DeserializeLegacyObject<T>(byte[] bytesToDeserialize, DataContractKind dataContractKind, params Type[] legacyTypes)
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
            using (JsonTextReader jtr = new JsonTextReader(sr))
            {
                schema = JSchema.Load(jtr);
            }
            using (var tr = new StringReader(jsonstring))
            using (var jr = new JsonTextReader(tr))
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
                    Console.WriteLine(ErrorCount.ToString() + " errors validating JSON.");
                    return null;
                }

            }
            return obj;
        }

        public static bool GenerateJsonSchema(Type T, string path, bool AllowAdditionalProperties = false)
        {
            var jgen = new Newtonsoft.Json.Schema.JsonSchemaGenerator();
            var schema = jgen.Generate(T, false);
            schema.Title = T.Name;
            try
            {
                using (var writer = new StringWriter())
                using (var jsonTextWriter = new JsonTextWriter(writer))
                using (var fileWriter = new StreamWriter(path, false, Encoding.UTF8))
                {
                    schema.WriteTo(jsonTextWriter);
                    JsonSerializerSettings jss = new JsonSerializerSettings();
                    jss.Formatting = Newtonsoft.Json.Formatting.Indented;
                    jss.CheckAdditionalContent = true;
                    jss.MissingMemberHandling = MissingMemberHandling.Error;
                    object parsedJson = JsonConvert.DeserializeObject(writer.ToString());
                    string jsonString = JsonConvert.SerializeObject(parsedJson, jss);
                    if (!AllowAdditionalProperties)
                    {
                        int indx = jsonString.Substring(0, jsonString.Length - 2).LastIndexOf('}') + 1;
                        jsonString = jsonString.Substring(0, indx) + ", " + Environment.NewLine + "  'additionalProperties': false" + Environment.NewLine + "}";
                    }
                    fileWriter.WriteLine(jsonString);
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
            xrs.ValidationFlags = XmlSchemaValidationFlags.ProcessIdentityConstraints | XmlSchemaValidationFlags.ReportValidationWarnings;
            xrs.ValidationEventHandler += (o, ev) =>
            {
                Errors = true;
                Console.WriteLine(ev.Message);
            };

            XmlSerializer xs = new XmlSerializer(typeof(T));
            using (XmlReader xr = XmlReader.Create(new StringReader(xmlstring), xrs))
            {  // Read through document to verify it complies with the schema.                
                try
                {
                    Result = (T)xs.Deserialize(xr);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error deserializing XML: " + ex.Message); return null;
                }
                return (Errors) ? null : Result;
            }


        }

        public static bool GenerateXMLSchema(Type T, string path)
        {
            if (!path.EndsWith(".xsd")) path += T.Name + ".xsd";
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
                if (string.IsNullOrEmpty(path)) { path = ""; }

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
            System.Diagnostics.Contracts.Contract.Requires<ArgumentNullException>(o != null, "Object cannot be null.");
            return GenerateXMLSchema(o.GetType(), path);
        }
    }

    public static class Diagnostics
    {
        private static Random rand = new Random();
        [DllImport("Kernel32.dll")]
        public static extern void QueryPerformanceCounter(ref long ticks);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        public static void BenchmarkMethod(Action a, int Count, bool PrepareDelegate = true, Action<string> WriteLineMethod = null)
        { // (Action<string>)typeof(Console).GetMethod("WriteLine", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null).CreateDelegate(typeof(Action<string>))
            if (WriteLineMethod == null) WriteLineMethod = s => Console.WriteLine(s);
            if (Count < 1) return;
            if (PrepareDelegate) System.Runtime.CompilerServices.RuntimeHelpers.PrepareDelegate(a);

            long time1 = 0, time2 = 0;
            long slowestTime = 0, fastestTime = long.MaxValue;
            long[] times = new long[Count];

            for (int i = 0; i < Count; i++)
            {
                QueryPerformanceCounter(ref time1);
                a();
                QueryPerformanceCounter(ref time2);
                long dif = time2 - time1;
                slowestTime = Math.Max(slowestTime, dif);
                fastestTime = Math.Min(fastestTime, dif);
                times[i] = dif;
            }

            // Get the averages/StD of all runs
            double avg = times.Average();
            double stddev = Math.Sqrt(times.Select(x => Math.Pow((double)(x - avg), 2d)).Average());

            // Get the averages/StD of last 20%
            int first80 = (int)(Count * .8);
            int last20 = Count - first80;
            var timesEnd = times.Skip(first80);
            double avg2 = timesEnd.Average();
            double stddev2 = Math.Sqrt(timesEnd.Select(x => Math.Pow((double)(x - avg), 2d)).Average());

            // Report the results
            WriteLineMethod("Benchmark method " + a.GetMethodInfo().Name);
            WriteLineMethod("   Average time: " + JonUtility.Utility.TicksToMS((long)avg, 4) + ", StD: " + JonUtility.Utility.TicksToMS((long)stddev, 4));
            WriteLineMethod("   Last " + last20.ToString() + "% Average time: " + JonUtility.Utility.TicksToMS((long)avg2, 4) + ", StD: " + JonUtility.Utility.TicksToMS((long)stddev2, 4));
            WriteLineMethod("   Fastest time: " + JonUtility.Utility.TicksToMS(fastestTime, 4));
            WriteLineMethod("   Slowest time: " + JonUtility.Utility.TicksToMS(slowestTime, 4));
        }

        public static string GetRandomString(int minLength, int maxLength = -1)
        {
            if (minLength <= 0) return string.Empty;
            if (maxLength <= minLength) maxLength = minLength + 1;
            int count = rand.Next(minLength, maxLength);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                int charnum = rand.Next(97, 123);
                sb.Append((char)charnum);
            }
            return sb.ToString();
        }

        public static string[] GetProgressiveStrings(int count, int minimumLength, int maximumLength = -1, int startRange = 33, int endRange = 126)
        {
            int charRange = endRange - startRange;

            if (maximumLength < minimumLength) maximumLength = minimumLength + 1;
            int stringLength = rand.Next(minimumLength, maximumLength);
            double maxCount = (int)Math.Pow(charRange, (double)stringLength);
            if (count > maxCount) throw new Exception("Count exceeds the maximum possible amount of " + maxCount.ToString() + ".");
            string[] strings = new string[count];

            char[] chars = new char[stringLength];
            int[] counters = new int[stringLength];
            for (int i = 0; i < stringLength; i++) counters[i] = startRange;

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
                        counters[i2] = startRange;
                    else
                        break;
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
            Trace.WriteLine("Analyzing struct " + T.Name + ": " + Struct.ToString());
            Trace.Indent();

            StructLayoutAttribute sla = T.StructLayoutAttribute;
            bool autoLayout = false;
            if (sla != null)
            {
                autoLayout = sla.Value.HasFlag(LayoutKind.Auto);
                Trace.WriteLine(string.Format("StructLayout = {0}, Pack = {1}, Size = {2}", sla.Value, sla.Pack, sla.Size));
            }
            if (!autoLayout) Trace.WriteLine(string.Format("Size: {0}", Marshal.SizeOf(T)));

            FieldInfo[] fields = T.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                string accessor = field.IsPublic ? "public" : "private";
                Trace.WriteLine(string.Format("{0} {1} {2}", accessor, field.FieldType.Name, field.Name));
                Trace.Indent();
                var attributes = field.CustomAttributes;
                var foa = attributes.FirstOrDefault(attr => attr.AttributeType == typeof(FieldOffsetAttribute));

                if (foa != null)
                    Trace.WriteLine(string.Format("[FieldOffset({0})]", foa.ConstructorArguments[0].Value));
                if (!autoLayout)
                    Trace.WriteLine(string.Format("Offset: {0}", Marshal.OffsetOf(T, field.Name).ToInt64()));

                Trace.Unindent();
            }

            Trace.Unindent();
        }

        public static void PrintValues(object obj, Action<string> WriteMethod = null)
        {
            _PrintValues pv = new _PrintValues(obj, WriteMethod);
            pv.Start();
        }

        private class _PrintValues
        {
            private static Action<string> defaultWriteMethod;
            private Action<string> WriteMethod;
            private List<Tuple<string, object>> CheckedProperties = new List<Tuple<string, object>>(20);
            private object Original;

            static _PrintValues()
            {
                defaultWriteMethod = (Action<string>)typeof(Console).GetMethod("WriteLine", BindingFlags.Public | BindingFlags.Static, null, new Type[1] { typeof(string) }, null).CreateDelegate(typeof(Action<string>), null);
            }

            public _PrintValues(object original, Action<string> writeMethod)
            {
                this.Original = original;
                this.WriteMethod = writeMethod ?? defaultWriteMethod;
            }

            public void Start()
            {
                WriteMethod("Analyzing " + Original.GetType().Name + " " + Original.ToString());
                try
                {
                    PrintValues(obj: Original, indent: "");
                }
                catch (Exception ex)
                {
                    WriteMethod("Error: " + ex.Message);
                }
            }

            private void PrintValues(object obj, string indent)
            {
                Type T = obj.GetType();

                Tuple<string, MemberInfo[]>[] members = new Tuple<string, MemberInfo[]>[3];
                members[0] = new Tuple<string, MemberInfo[]>("Public Properties:", T.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(pi => pi.GetIndexParameters().Length == 0).ToArray<MemberInfo>());
                members[1] = new Tuple<string, MemberInfo[]>("Public Fields:", T.GetFields(BindingFlags.Public | BindingFlags.Instance));
                members[2] = new Tuple<string, MemberInfo[]>("Private Fields:", T.GetFields(BindingFlags.NonPublic | BindingFlags.Instance));

                int count = members.Sum(list => list.Item2.Length) + members.Length;
                string indent1 = indent + "   ";
                string indent2 = indent1 + "   ";

                //StringBuilder sb = new StringBuilder();
                foreach (var minfo in members)
                {
                    if (minfo.Item2.Length == 0) continue;
                    WriteMethod(indent1 + minfo.Item1);
                    int length = minfo.Item2.Max(mi => mi.Name.Length);

                    for (int i = 0; i < minfo.Item2.Length; i++)
                    {
                        MemberInfo member = minfo.Item2[i];
                        Type memType = (member.MemberType == MemberTypes.Property) ? ((PropertyInfo)member).PropertyType : ((FieldInfo)member).FieldType;
                        string memPrefix = member.Name + " {" + memType.Name + "}";

                        object value;
                        try
                        {
                            value = ((dynamic)member).GetValue(obj);
                            ProcessValue(member: member, value: value, indent: indent2, prefix: memPrefix);
                        }
                        catch (Exception ex)
                        {
                            WriteMethod(indent2 + memPrefix + ": threw Exception of type " + ex.GetType().Name);
                        }
                    }
                }
            }

            private void ProcessValue(MemberInfo member, object value, string indent, string prefix)
            {
                if (value == null)
                {
                    WriteMethod(indent + prefix + ": null");
                    return;
                }

                if (value is string)
                {
                    WriteMethod(indent + prefix + ": " + value.ToString());
                    return;
                }

                Type T2 = value.GetType();
                if (T2.IsPrimitive)
                {
                    WriteMethod(indent + prefix + ": " + value.ToString());
                    return;
                }

                Tuple<string, object> lolzo;
                if ((lolzo = CheckedProperties.FirstOrDefault(x => x.Item2 == value)) != null)
                {
                    WriteMethod(indent + prefix + ": see " + lolzo.Item1 + " property.");
                    return;
                }

                if (T2.IsArray)
                {
                    WriteMethod(indent + prefix + ": array");
                    ProcessArray(member: member, array: (dynamic)value, indent: indent + "   ", indexString: "");
                    CheckedProperties.Add(new Tuple<string, object>(member.Name, value));
                    return;
                }

                WriteMethod(indent + prefix + ": " + value.ToString());
                PrintValues(obj: value, indent: indent);
                CheckedProperties.Add(new Tuple<string, object>(member.Name, value));
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
                        double temp = (double)x;
                        for (int i2 = 0; i2 < rank - 1; i2++)
                        {
                            indices[i2] = (int)Math.Floor(temp / sums[i2 + 1]);
                            if (indices[i2] > 0) temp -= indices[i2] * sums[i2 + 1];
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
                        WriteMethod(indent + indexString + val.ToString() + ": null");
                    else
                    {
                        Type T = o.GetType();
                        if (!T.IsArray)
                            ProcessValue(member: member, value: o, indent: indent, prefix: indexString + val.ToString());
                        else
                        {
                            string newIndex = indexString + val.ToString() + ",";
                            ProcessArray(member: member, array: (dynamic)o, indent: indent, indexString: newIndex);
                        }
                    }
                }
            }

        }
    }

    public static class MathFunctions
    {
        public enum BirthdayStyle : int
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
                    BigInteger xFactorial = Factorial(x);
                    BigInteger xminusnFactorial = Factorial(x - n);
                    BigInteger xPowN = BigInteger.Pow(x, (int)n);
                    BigInteger div1 = BigInteger.Divide(xFactorial, xminusnFactorial);
                    BigInteger div2 = BigInteger.Divide(div1 * 10000, xPowN);
                    double final = (double)div2 / 10000d;
                    return (1d - final);
                case BirthdayStyle.Approximation1:
                    double negativeNSquared = -1d * ((double)n * ((double)n - 1d));
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
            if (number < 0) throw new ArgumentException("Number must be positive.");
            if (number == 0 || number == 1) return new BigInteger(1);
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

    public static class ExtensionMethods
    {
        public static SecureString ConvertToSecureString(this string @this)
        {
            unsafe
            {
                fixed (char* c = @this)
                {
                    System.Security.SecureString ss = new System.Security.SecureString(c, @this.Length);
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
                if (ptr != IntPtr.Zero) Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }
    }
}
