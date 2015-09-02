using System;
using System.ComponentModel.Composition;
using System.Collections.Concurrent;

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
    [PluginInfo(Name = "WriteCues", Category = "Iris", Help = "WriteCues", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo

    public class WriteCues : IPluginEvaluate, IDisposable
    {
        [Import()]
        public IPluginHost V1Host;

        [Import()]
        public IHDEHost V2Host;

        [Import()]
        public ILogger Logger;

        [Input("TimeCode", IsSingle = true)]
        ISpread<int> InTimeCode;

        [Input("Debug", IsSingle = true, DefaultValue = 0)]
        ISpread<bool> InDebug;

        [Output("Buffer Length", IsSingle = true)]
        ISpread<int> OutLength;

        [Output("Connected", IsSingle = true, DefaultValue = 0)]
        ISpread<bool> OutConnected;

        private int Frame = 0;
        private int Retries = 5;
        private bool Initialized = false;
        private VVVVGraph VVVV;
        private CueAmqpListener Listener;
        private ConcurrentDictionary<string,Cue> Cues;

        public WriteCues()
        {
            Host _ = Host.Renderer;
        }

        public void Dispose()
        {
            if(Listener != null) Listener.Stop();
        }

        private void Initialize()
        {
            if(Listener != null && Listener.Error != null && Retries != 0)
                Logger.Log(LogType.Debug, Listener.Error);

            if(Listener != null) OutConnected[0] = Listener.Running;

            if(Initialized && Listener.Running) return;
            if(Retries == 0) return;

            if(VVVV == null)
            {
                VVVV = new VVVVGraph {
                    V1Host = V1Host,
                    V2Host = V2Host
                };
                VVVV.Register();
            }

            if(Cues == null)
                Cues = new ConcurrentDictionary<string, Cue>();

            if(Listener == null)
                Listener = new CueAmqpListener(Cues);

            if(Listener.Error == null)
                Listener.Start();

            Initialized = true;
            Retries -= 1;
        }

        public void Evaluate(int _)
        {
            Initialize();
            Process();
            Tick();
        }

        private void Tick()
        {
            if (Initialized) VVVV.NextTick();
        }

        private void Process()
        {
            if (!Initialized || !Listener.Running ||
                Listener.Error != null) return;

            OutLength[0] = Cues.Count;

            if(Cues.IsEmpty) return;

            if(InTimeCode.IsChanged) Frame = InTimeCode[0];

            var e = Cues.GetEnumerator();
            while(e.MoveNext())
            {
                var cue = e.Current.Value;

                if(cue.ExecFrame < 0 || (cue.ExecFrame <= Frame))
                {
                    Cue c; Cues.TryRemove(e.Current.Key, out c);
                    VVVV.CallCue(c);

                    if(InDebug[0]) {
                        Logger.Log(LogType.Debug, "Writing cue to Graph: " + c.Name);
                        LogEntry.Info("WriteCues", "Writing cue to Graph: " + c.Name);
                    }
                }
            }
        }
    }
}
