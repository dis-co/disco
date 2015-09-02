using System;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;

namespace Iris.Core.Types
{
    class IrisIPConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            IrisIP path = (IrisIP)value;
            writer.WriteValue(path.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new IrisIP((string)reader.Value);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IrisIP);
        }
    }

    [JsonConverter(typeof(IrisIPConverter))]
    public class IrisIP
    {
        private string Val { get; set; }

        public  IrisIP()
        {
            IPHostEntry host;
            string Ip = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Ip = ip.ToString();
                }
            }
            Val = Ip;
        }

        public IrisIP(string val)
        {
            Val = val;
        }

        public static bool operator ==(IrisIP np1, string np2)
        {
            if(object.ReferenceEquals(null, np1) || 
               object.ReferenceEquals(null, np2))
                return false;

            return np1.ToString() == np2;
        }

        public static bool operator !=(IrisIP np1, string np2)
        {
            return !(np1 == np2);
        }

        public static bool operator ==(IrisIP np1, IrisIP np2)
        {
            if(object.ReferenceEquals(null, np1) || 
               object.ReferenceEquals(null, np2))
                return false;

            return np1.ToString() == np2.ToString();
        }

        public static bool operator !=(IrisIP np1, IrisIP np2)
        {
            return !(np1 == np2);
        }

        public override bool Equals(object obj)
        {
            var it = obj as IrisIP;
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
