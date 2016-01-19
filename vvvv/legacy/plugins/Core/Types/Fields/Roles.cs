using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Iris.Core.Logging;

namespace Iris.Core.Types
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Roles
    {
        Renderer,
        FrontEnd,
        Processor,
        Undefined
    }

    public static class Role
    {
        public static Roles Parse(string input)
        {
            Roles role = Roles.Undefined;
            try
            {
                var type = (Roles)Enum.Parse(typeof(Roles), input);
                if (Enum.IsDefined(typeof(Roles), type)) role = type;
            }
            catch (Exception ex)
            {
                LogEntry.Fatal("Role", "Could not parse Role  " + ex.Message);
            }
            return role;
        }
    }
}
