using System;
using VVVV.PluginInterfaces.V2;
using Iris.Core.Types;

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "HostInfo", Category = "Iris", Help = "Show info for current host", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo

    public class HostInfo : IPluginEvaluate, IDisposable
    {
        [Output("Name")]
        ISpread<string> OutHostName;

        [Output("Id")]
        ISpread<string> OutId;

        [Output("IP")]
        ISpread<string> OutIP;

        [Output("Role")]
        ISpread<string> OutRole;

        public HostInfo()
        {
            Host _ = Host.Renderer;
        }

        public void Dispose()
        {
        }

        public void Evaluate (int _)
        {
            OutHostName[0] = Host.HostName.ToString();
            OutId[0] = Host.HostId.ToString();
            OutIP[0] = Host.IP.ToString();
            OutRole[0] = Host.Role.ToString();
        }
    }
}
