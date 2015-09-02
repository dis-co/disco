using System;
using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Iris.Core.Logging;

namespace Iris.Core.Types
{
    public class PinSliceConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var pv = value as PinSlice;

            if (pv == null) return;

            var obj = new JObject();
            obj.Add(new JProperty("Behavior", JToken.FromObject(pv.Behavior)));

            try
            {
                switch(pv.Behavior)
                {
                    case Behavior.Bang:
                        if(pv.Value.GetType() == typeof(Boolean))
                            obj.Add("Value", new JValue(pv.Value));
                        else if(pv.Value.GetType() == typeof(Double) ||
                                pv.Value.GetType() == typeof(Int32)  ||
                                pv.Value.GetType() == typeof(Int64))
                            obj.Add("Value", new JValue((double)pv.Value > 0));
                        else if(pv.Value.GetType() == typeof(string))
                            obj.Add("Value", new JValue(pv.Value.ToString() == "1"));
                        else
                            obj.Add("Value", new JValue(pv.Value));
                        break;
                    case Behavior.Toggle:
                        if(pv.Value.GetType() == typeof(Boolean))
                            obj.Add("Value", new JValue(pv.Value));
                        else if(pv.Value.GetType() == typeof(string))
                            obj.Add("Value", new JValue(pv.Value.ToString() == "1"));
                        else
                            obj.Add("Value", new JValue(pv.Value));
                        break;
                    case Behavior.XSlider:
                        if(pv.Value.GetType() == typeof(Double) ||
                           pv.Value.GetType() == typeof(Int32)  || 
                           pv.Value.GetType() == typeof(Int64))
                        {
                            obj.Add("Value", new JValue(pv.Value));
                        }
                        else
                        {
                            double dbl;

                            var res = Double.TryParse((string)pv.Value,
                                NumberStyles.Float, CultureInfo.InvariantCulture, out dbl);
                            
                            if(res)
                                obj.Add("Value", new JValue(dbl));
                            else
                                obj.Add("Value", new JValue(pv.Value));
                        }
                        break;
                    default:
                        obj.Add("Value", new JValue((string)pv.Value));
                        break;
                }
            }
            catch (Exception ex)
            {
                LogEntry.Fatal("PinSlice", "Excpetion in json parser" + ex.Message);
            }

            obj.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException("Not Implemented Here (syndrome)");
        }
    }

    [JsonConverter(typeof(PinSliceConverter))]
    public class PinSlice
    {
        public Behavior Behavior { get; set; }
        public Object   Value    { get; set; }

        public PinSlice(Behavior behavior, Object value)
        {
            Behavior = behavior;
            Value    = value;
        }

        public string ToSpread()
        {
            switch (Behavior)
            {
                case Behavior.Toggle:
                    return BooleanValue();

                case Behavior.Bang:
                    return BooleanValue();

                case Behavior.XSlider:
                    return DoubleValue();

                default:
                    return "|" + Value + "|";
            }
        }

        public string ToSpread(int idx)
        {
            // separator for XY values is 'x'
            return "|" + Value.ToString().Split('x')[idx] + "|";
        }

        private string BooleanValue()
        {
            bool outp;
            bool.TryParse(Value.ToString(), out outp);
            if (outp) return "|1|";
            return "|0|";
        }

        private string DoubleValue()
        {
            double val = 0;
            if (Value != null)
                val = Convert.ToDouble (Value.ToString ());
            return "|" + val.ToString(CultureInfo.GetCultureInfo("en-US")) + "|";
        }
    }
}
