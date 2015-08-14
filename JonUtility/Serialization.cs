namespace JonUtility
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Schema;
    using Formatting = Newtonsoft.Json.Formatting;

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
            DynamicMethod setterMethod = new DynamicMethod(methodName, null, new Type[2] {typeof(S), typeof(T)}, true);
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

            DynamicMethod setterMethod = new DynamicMethod(string.Empty, fieldType, new Type[1] {objType}, true);
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
}
