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
    [PluginInfo(Name = "CueInfo", Category = "Iris", Help = "CueInfo", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo

    public class CueInfo : IPluginEvaluate, IDisposable
    {
        [Import()]
        public IHDEHost V2Host;

        [Import()]
        public ILogger Logger;

        [Input("Cues")]
        ISpread<Cue> InCue;

        [Input("Update", IsBang = true, IsSingle = true)]
        ISpread<bool> FUpdate;

        [Output("Id")]
        ISpread<string> OutId;

        [Output("Revision")]
        ISpread<string> OutRev;

        [Output("Name")]
        ISpread<string> OutName;

        [Output("Tags")]
        ISpread<ISpread<string>> OutTags;

        [Output("Hosts")]
        ISpread<ISpread<string>> OutHosts;

        [Output("Values")]
        ISpread<List<CueValue>> OutValues;

        public void Dispose()
        {
        }

        public void Evaluate (int max)
        {
            if(FUpdate[0])
            {
            if((InCue.IsChanged && InCue.SliceCount > 0))
            {
                OutId.SliceCount     = max;
                OutRev.SliceCount    = max;
                OutName.SliceCount   = max;
                OutTags.SliceCount   = max;
                OutHosts.SliceCount  = max;
                OutValues.SliceCount = max;

                for(int i = 0; i < InCue.SliceCount; i++)
                {
                    Cue cue = InCue[i];
                    if(cue != null)
                    {
                        OutId[i]     = cue._id.ToString();
                        OutRev[i]    = cue._rev.ToString();
                        OutName[i]   = cue.Name;
                        OutValues[i] = cue.Values;

                        if(cue.Tags != null)  OutTags[i].AssignFrom<string>(cue.Tags);
                        if(cue.Hosts != null) OutHosts[i].AssignFrom<string>(cue.Hosts);
                    }
                }
            }
            else
            {
                OutId[0]     = null;
                OutRev[0]    = null;
                OutName[0]   = null;
                OutValues[0] = null;
            }
               }
        }
    }
}
