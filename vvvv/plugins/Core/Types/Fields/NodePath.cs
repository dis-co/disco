using System;
using Newtonsoft.Json;

namespace Iris.Core.Types
{
    class NodePathConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var path = (NodePath)value;
            writer.WriteValue(path.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new NodePath((string)reader.Value);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(NodePath);
        }
    }

    [JsonConverter(typeof(NodePathConverter))]
    public class NodePath
    {
        private string Val { get; set; }

        public NodePath(string val)
        {
            Val = val;
        }

        public static bool operator ==(NodePath np1, string np2)
        {
            if(object.ReferenceEquals(null, np1) || 
               object.ReferenceEquals(null, np2))
                return false;

            return np1.ToString() == np2;
        }

        public static bool operator !=(NodePath np1, string np2)
        {
            return !(np1 == np2);
        }

        public static bool operator ==(NodePath np1, NodePath np2)
        {
            if(object.ReferenceEquals(null, np1) || 
               object.ReferenceEquals(null, np2))
                return false;

            return np1.ToString() == np2.ToString();
        }

        public static bool operator !=(NodePath np1, NodePath np2)
        {
            return !(np1 == np2);
        }

        public override bool Equals(object obj)
        {
            var it = obj as NodePath;
            return this == it;
        }

        public override Int32 GetHashCode()
        {
            return Val.GetHashCode();
        }

        public override string ToString()
        {
            return Val;
        }
    }
}
