using System;

using Newtonsoft.Json;

using Iris.Core.Logging;

namespace Iris.Core.Types
{
    public class PinAttributes
    {
        public string Id { get; set; }

        public PinAttributes(bool generate) {
            if(generate) Id = Guid.NewGuid().ToString();
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string ToSpread()
        {
            return "|" + ToJson() + "|";
        }

        public static PinAttributes FromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<PinAttributes>(json);
            }
            catch(Exception ex)
            {
                LogEntry.Fatal("PinAttributes", "Could not deserialize PinAttributes: " + ex.Message);
                return new PinAttributes(true);
            }
        }
    }
}