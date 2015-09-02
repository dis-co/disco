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
using Iris.Core.Logging;
using Iris.Nodes.Types;
using Iris.Nodes.Graph;

namespace VVVV.Nodes
{
    #region PluginInfo
    [PluginInfo(Name = "GetProjects", Category = "Iris", Help = "GetProjects", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo

    public class GetProjects : IPluginEvaluate, IDisposable
    {
        private bool                                 Initialized = false;
        private ConcurrentDictionary<string,Project> Projects;
        private ProjectListener                      Listener;

        [Import()]
        public IHDEHost V2Host;

        [Import()]
        public ILogger Logger;

        [Input("Update", IsBang = true, IsSingle = true)]
        ISpread<bool> FUpdate;

        [Output("Projects")]
        ISpread<Project> OutProjects;

        public GetProjects()
        {
            Host ThisIsA = Host.Renderer;
        }

        public void Dispose()
        {
            if(Listener != null) Listener.Stop();
        }

        private void Initialize()
        {
            if (Initialized) return;

            Projects = new ConcurrentDictionary<string, Project>();
            Listener = new ProjectListener(Projects);

            try
            {
                Listener.Start();
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Debug, "ex: " + ex.Message);
                Logger.Log(LogType.Debug, ex.StackTrace);
            }

            Initialized = true;
        }

        private void Process() // handle all input
        {
            OutProjects.AssignFrom(Projects.ToList().Select(kv => kv.Value));
        }

        public void Evaluate (int _)
        {
            Initialize();
            if (FUpdate[0])
                Process();
        }
    }
}