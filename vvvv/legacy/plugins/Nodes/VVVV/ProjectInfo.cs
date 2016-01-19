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
    [PluginInfo(Name = "ProjectInfo", Category = "Iris", Help = "ProjectInfo", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo

    public class ProjectInfo : IPluginEvaluate, IDisposable
    {
        [Import()]
        public IHDEHost V2Host;

        [Import()]
        public ILogger Logger;

        [Input("Projects")]
        ISpread<Project> InProject;

        [Output("Id")]
        ISpread<string> OutId;

        [Output("Name")]
        ISpread<string> OutName;

        [Output("Database")]
        ISpread<string> OutDatabase;

        public void Dispose()
        {
        }

        public void Evaluate (int _)
        {
            if(InProject.IsChanged && InProject.SliceCount > 0)
            {
                for(int i = 0; i < InProject.SliceCount; i++)
                {
                    Project project = InProject[i];
                    if(project != null)
                    {
                        OutId[i]       = project._id.ToString();
                        OutName[i]     = project.Name;
                        OutDatabase[i] = project.Database;
                    }
                }
            }
            else
            {
                OutId[0]       = null;
                OutName[0]     = null;
                OutDatabase[0] = null;
            }
        }
    }
}