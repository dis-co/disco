using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Iris.Core.Types
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PinType
    {
        Value,
        String,
        Color,
        Enum,
        Node
    }
}
