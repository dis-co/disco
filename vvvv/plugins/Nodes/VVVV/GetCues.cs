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
    using CuesDict = ConcurrentDictionary<string,Cue>;

    #region PluginInfo
    [PluginInfo(Name = "GetCues", Category = "Iris", Help = "GetCues", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo

    public class GetCues : IPluginEvaluate, IDisposable
    {
        private bool        Initialized = false;
        private CuesDict    Cues;
        private CueListener Listener;
        private Project     CurrentProject;

        [Import()]
        public IHDEHost V2Host;

        [Import()]
        public ILogger Logger;

        [Input("Project")]
        ISpread<Project> InProject;

        [Input("Update", IsBang = true, IsSingle = true)]
        ISpread<bool> FUpdate;

        [Output("Cues")]
        ISpread<Cue> OutCues;

        public GetCues()
        {
            Cues = new CuesDict();
        }

        public void Dispose()
        {
            if(Listener != null) Listener.Stop();
        }

        private void Initialize()
        {
            if (Initialized) return;
            if(CurrentProject != null && CurrentProject.Database != null)
            {
                Listener = new CueListener(Logger, CurrentProject, Cues);
                Listener.Start();
                Initialized = true;
            }
        }

        private void DeInitialize()
        {
            if(!Initialized) return;
            if(Listener != null) Listener.Stop();
            Cues.Clear();
            Initialized = false;
        }

        private void Process() // handle all input
        {
            try
            {
                OutCues.AssignFrom(Cues.ToList().ConvertAll(kv => kv.Value));
            }
            catch(Exception ex)
            {
                Logger.Log(LogType.Debug, "Oops");
                Logger.Log(LogType.Debug, ex.Message);
                Logger.Log(LogType.Debug, ex.StackTrace);
            }
        }


        public void Evaluate (int _)
        {
            if(InProject.IsChanged && InProject.SliceCount > 0)
            {
                Project project = InProject[0];
                if(project != null && CurrentProject != project)
                {
                    CurrentProject = project;
                    DeInitialize();
                    Initialize();
                }
            }
            else
            {
                CurrentProject = null;
                DeInitialize();
            }
            if (FUpdate[0])
                Process();
        }
    }
}
