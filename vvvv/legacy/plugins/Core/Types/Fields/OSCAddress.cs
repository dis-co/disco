using System;
using Iris.Core.Logging;
using Newtonsoft.Json;

namespace Iris.Core.Types
{
    class OSCAddressConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var path = (OSCAddress)value;
            writer.WriteValue(path.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new OSCAddress((string)reader.Value);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(OSCAddress);
        }
    }

    [JsonConverter(typeof(OSCAddressConverter))]
    public class OSCAddress
    {
        private string Val { get; set; }

        public OSCAddress(string val)
        {
            Val = val;
        }

        public static bool operator ==(OSCAddress np1, string np2)
        {
            if(object.ReferenceEquals(null, np1) || 
               object.ReferenceEquals(null, np2))
                return false;

            return np1.ToString() == np2;
        }

        public static bool operator !=(OSCAddress np1, string np2)
        {
            return !(np1 == np2);
        }

        public static bool operator ==(OSCAddress np1, OSCAddress np2)
        {
            if(object.ReferenceEquals(null, np1) || 
               object.ReferenceEquals(null, np2))
                return false;

            return np1.ToString() == np2.ToString();
        }

        public static bool operator !=(OSCAddress np1, OSCAddress np2)
        {
            return !(np1 == np2);
        }

        public override bool Equals(object obj)
        {
            var it = obj as OSCAddress;
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
