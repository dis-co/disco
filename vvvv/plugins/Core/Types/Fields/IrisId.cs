using System;
using Newtonsoft.Json;

namespace Iris.Core.Types
{
    class IrisIdConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var path = (IrisId)value;
            writer.WriteValue(path.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new IrisId((string)reader.Value);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IrisId);
        }
    }

    [JsonConverter(typeof(IrisIdConverter))]
    public class IrisId
    {
        private string Val { get; set; }

        public IrisId()
        {
            Val = Guid.NewGuid().ToString();
        }

        public IrisId(string val)
        {
            Val = val;
        }

        public static bool operator ==(IrisId np1, string np2)
        {
            if(object.ReferenceEquals(null, np1) || 
               object.ReferenceEquals(null, np2))
                return false;

            return np1.ToString() == np2;
        }

        public static bool operator !=(IrisId np1, string np2)
        {
            return !(np1 == np2);
        }

        public static bool operator ==(IrisId np1, IrisId np2)
        {
            if(object.ReferenceEquals(null, np1) || 
               object.ReferenceEquals(null, np2))
                return false;

            return np1.ToString() == np2.ToString();
        }

        public static bool operator !=(IrisId np1, IrisId np2)
        {
            return !(np1 == np2);
        }

        public override bool Equals(object obj)
        {
            var it = obj as IrisId;
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
