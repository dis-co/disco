using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Linq;
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

// ------------  Call Graph (for Bangs)  -------------------
//
// Evaluate
//    | 
//    Process (update our world)
//    |  |
//    |  CallCue &&  UpdatePin
//    |      | 
//    |      pin.Update (either values, or entire pin)
//    |      |
//    |      MkQueueJob value
//    |
//    Tick  (now flush it to vvvv)
//    |  |
//    |  VVVVGraph.FrameCount <= CurrentFrame 
//    |  |
//    |  ProcessGraphWrites
//    |         |
//    |         IPin2.Spread = "|val|"
//    |         |
//    |         MkQueueJob value (Reset with current frame + 1)
//    |
//    Cleanup
//

namespace VVVV.Nodes
{
    using ChangedPin = Tuple<string, string, string>;

    #region PluginInfo
    [PluginInfo(Name = "GraphApi", Category = "Iris", Help = "GraphApi", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo

    public class GraphApi : IPluginEvaluate, IDisposable
    {
        [Import()]
        public IPluginHost V1Host;

        [Import()]
        public IHDEHost V2Host;

        [Import()]
        public ILogger Logger;

        [Input("TimeCode", IsSingle = true, DefaultValue = 0)]
        ISpread<int> InTimeCode;

        [Input("Sync", IsSingle = true, DefaultValue = 0)]
        ISpread<bool> InSync;

        [Input("Latency", IsSingle = true, DefaultValue = 0)]
        ISpread<int> InLatency;

        [Input("Debug", IsSingle = true, DefaultValue = 0)]
        ISpread<bool> InDebug;

        [Output("Buffer Length", IsSingle = true)]
        ISpread<int> OutLength;

        [Output("Recently Played", IsSingle = true)]
        ISpread<int> OutRecent;

        [Output("Played Cues")]
        ISpread<string> OutCues;

        [Output("Connected", IsSingle = true, DefaultValue = 0)]
        ISpread<bool> OutConnected;

        private int Frame = 0;

        private bool Initialized        = false;
        private bool ContextInitialized = false;
        private bool ServerInitialized  = false;

        private AmqpServer AmqpServer;
        private VVVVGraph  Context;

        private CueAmqpListener Listener;

        private ConcurrentDictionary<string,Cue>   Cues;
        private ConcurrentDictionary<string, int> RecentlyPlayed;

        #region Destructor
        public void Dispose()
        {
            if(Listener != null) AmqpServer.Dispose(); 
            if(Listener != null) Listener.Dispose();
            if(Context  != null) Context.Dispose();
        }
        #endregion

        #region Initialization
        private void Initialize()
        {
            if (Initialized) return;
            InitContext();
            InitServer();
            Initialized = true;
        }

        private void InitContext()
        {
            if(ContextInitialized) return;
            Host _ = Host.Renderer; // initialize this global singleton
            Context = new VVVVGraph {
                V1Host = V1Host,
                V2Host = V2Host,
                Logger = Logger
            };
            Context.Register();
            ContextInitialized = true;
        }

        private void InitServer()
        {
            if(ServerInitialized) return;

            Cues = new ConcurrentDictionary<string, Cue>();
            RecentlyPlayed = new ConcurrentDictionary<string, int>();

            AmqpServer = new AmqpServer { Context = Context };
            AmqpServer.Start();

            Listener = new CueAmqpListener(Cues);
            Listener.Start();

            ServerInitialized = true;
        }
        #endregion

        private void Tick()
        {
            if (ContextInitialized) Context.NextTick();
        }

        private void CleanUp()
        {
            RecentlyPlayed.ToList().ForEach(kv => {
                    if(Frame >= kv.Value)
                    {
                        int val;
                        RecentlyPlayed.TryRemove(kv.Key, out val);

                        if(InDebug[0])
                            Logger.Log(LogType.Debug,
                                       " Removed "               + kv.Key +
                                       " Current Frame: "        + Frame  +
                                       " Removal Target Frame: " + kv.Value);
                    }
                });
        }

        private void Process()
        {
            var running = (AmqpServer.Running && Listener.Running);
            OutConnected[0] = running;

            if(!running) return;

            OutLength[0] = Cues.Count;
            OutRecent[0] = RecentlyPlayed.Count;

            //  _____ _                 ____          _      
            // |_   _(_)_ __ ___   ___ / ___|___   __| | ___ 
            //   | | | | '_ ` _ \ / _ \ |   / _ \ / _` |/ _ \
            //   | | | | | | | | |  __/ |__| (_) | (_| |  __/
            //   |_| |_|_| |_| |_|\___|\____\___/ \__,_|\___|
            // 
            if(InSync[0])
            {
                Frame = InTimeCode[0];
            }
            else
            {
                Frame += 1;
            }

            Context.Debug = InDebug[0];

            // Nothing to do.
            if(Cues.IsEmpty) return;

            var calledCues = new List<string>();

            //  ____                              
            // |  _ \ _ __ ___   ___ ___  ___ ___ 
            // | |_) | '__/ _ \ / __/ _ \/ __/ __|
            // |  __/| | | (_) | (_|  __/\__ \__ \
            // |_|   |_|  \___/ \___\___||___/___/
            // 
            var e = Cues.GetEnumerator();
            while(e.MoveNext())
            {
                var cue = e.Current.Value;

                // if the cue was played in the last couple of frames...
                if(InLatency[0] > 0 && RecentlyPlayed.ContainsKey(cue._id))
                {   // skip && remove it 
                    Cue c;
                    if(Cues.TryRemove(e.Current.Key, out c))
                    {
                        if(InDebug[0])
                            Logger.Log(LogType.Debug, "Skipped duplicate " + cue.Name);
                    }   
                    else
                    {   // should I retry once more to remove it?
                        Cues.TryRemove(e.Current.Key, out c);
                    }
                    continue;
                }

                /// <summary>
                ///   Base case: fire cue immediately if it has no ExecFrame set
                /// </summary>
                if(cue.ExecFrame < 0 || cue.ExecFrame <= Frame)
                {
                    Cue c;

                    if(Cues.TryRemove(e.Current.Key, out c))
                    {
                        if(InDebug[0])
                            Logger.Log(LogType.Debug, "Calling cue: " + cue.Name + " target frame: " + cue.ExecFrame +  " current frame: " + Frame);

                        Context.CallCue(c);

                        calledCues.Add(c.Name);

                        if(InLatency[0] > 0)
                        {
                            if(InDebug[0])
                                Logger.Log(LogType.Debug, "Adding " + cue.Name + " to duplicates buffer on Frame: " + Frame);

                            RecentlyPlayed.TryAdd(cue._id, Frame + InLatency[0]);
                        }
                    } 
                    continue;
                }
            }

            OutCues.AssignFrom(calledCues);
        }

        public void Evaluate (int _)
        {
            Initialize();
            Process();
            Tick();
            CleanUp();
        }
    }
}
