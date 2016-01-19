using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Iris.Core.Types
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Behavior
    {
        None,
        XSlider,
        XYSlider,
        Toggle,
        Bang,
        String,
        MultiLine,
        FileName,
        Directory,
        Url,
        IP
    }
}
