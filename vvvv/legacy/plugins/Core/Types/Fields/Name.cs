using System;

using Newtonsoft.Json;

namespace Iris.Core.Types
{
    class NameConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var path = (Name)value;
            writer.WriteValue(path.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new Name((string)reader.Value);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Name);
        }
    }

    [JsonConverter(typeof(NameConverter))]
    public class Name
    {
        private string Val { get; set; }

        public Name(string val)
        {
            Val = val;
        }

        public static bool operator ==(Name np1, string np2)
        {
            if(object.ReferenceEquals(null, np1) || 
               object.ReferenceEquals(null, np2))
                return false;

            return np1.ToString() == np2;
        }

        public static bool operator !=(Name np1, string np2)
        {
            return !(np1 == np2);
        }

        public static bool operator ==(Name np1, Name np2)
        {
            if(object.ReferenceEquals(null, np1) || 
               object.ReferenceEquals(null, np2))
                return false;

            return np1.ToString() == np2.ToString();
        }

        public static bool operator !=(Name np1, Name np2)
        {
            return !(np1 == np2);
        }

        public override bool Equals(object obj)
        {
            var it = obj as Name;
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
