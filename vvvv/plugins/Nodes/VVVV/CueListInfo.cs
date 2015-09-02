using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

using Iris.Nodes;
using Iris.Core.Types;
using Iris.Core.Events;
using Iris.Nodes.Types;
using Iris.Nodes.Graph;

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "CueListInfo", Category = "Iris", Help = "CueListInfo", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo

    public class CueListInfo : IPluginEvaluate, IDisposable
    {
        [Import()]
        public IHDEHost V2Host;

        [Import()]
        public ILogger Logger;

        [Input("CueLists")]
        ISpread<CueList> InCueList;

        [Output("Id")]
        ISpread<string> OutId;

        [Output("Revision")]
        ISpread<string> OutRev;

        [Output("Name")]
        ISpread<string> OutName;

        [Output("Values")]
        ISpread<List<string>> OutCues;

        public void Dispose()
        {
        }

        public void Evaluate (int _)
        {
            if(InCueList.IsChanged && InCueList.SliceCount > 0)
            {
                for(int i = 0; i < InCueList.SliceCount; i++)
                {
                    CueList cue = InCueList[i];
                    if(cue != null)
                    {
                        OutId[i]   = cue._id.ToString();
                        OutRev[i]  = cue._rev.ToString();
                        OutName[i] = cue.Name;
                        OutCues[i] = cue.Cues;
                    }
                }
            }
            else
            {
                OutId[0]   = null;
                OutRev[0]  = null;
                OutName[0] = null;
                OutCues[0] = null;
            }
        }
    }
}