using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Iris.Core.Types
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ValType
    {
        None,
        Real,
        Int,
        Bool
    }
}
