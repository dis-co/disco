using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#pragma warning disable 0168

namespace VVVV.Nodes
{
    using Payload = Tuple<string, string, IIrisData>;

    #region PluginInfo
    [PluginInfo(Name = "CallCue", Category = "Iris", Help = "CallCue", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo

    public class CallCue : IPluginEvaluate, IDisposable
    {
        [Import()]
        public IHDEHost V2Host;

        [Import()]
        public ILogger Logger;

        [Input("Call")]
        IDiffSpread<bool> InCall;

        [Input("Cue")]
        ISpread<Cue> InCue;

        [Input("Debug", IsSingle=true, IsToggle=true)]
        ISpread<bool> FDebug;

        [Input("ExecFrame", DefaultValue = -1)]
        ISpread<int> InExecFrame;

        [Output("Connected", IsSingle = true, DefaultValue = 0)]
        ISpread<bool> OutConnected;

        private bool Debug = false;
        private bool Run = true;
        private bool Connected = false;
        private int Retries = 5;

        private Thread WorkerThread;
        private CancellationTokenSource Token;
        private BlockingCollection<Payload> Queue;
        
        public CallCue()
        {
            Host _ = Host.Renderer;
            Queue = new BlockingCollection<Payload>();
            WorkerThread = new Thread(Runnable);
            Token = new CancellationTokenSource();
        }

        public void Dispose()
        {
            Token.Cancel();
        }

        public void Evaluate (int sm)
        {
            Init();

            Debug = FDebug[0];
            
            if(Connected && InCall.IsChanged) {
                for(int i = 0; i < sm; i++)
                {
                    if(InCall[i])
                    {
                        var Cue = InCue[i];
                        Cue.ExecFrame = InExecFrame[i];

                        if (Debug)
                        { 
                            Logger.Log(LogType.Debug, "Called cue: " + Cue.Name);
                            Logger.Log(LogType.Debug, "Called cue: " + Cue.ExecFrame.ToString());
                            LogEntry.Info("CallCues", "Called cue: " + Cue.Name);
                        }

                        Queue.Add(new Payload(DataUri.CueBase, DataUri.UpdateAction, Cue));
                    }
                }
            }

            OutConnected[0] = Connected;
        }

        private void Init()
        {
            if(Connected || Retries == 0) return;
            WorkerThread.Start();
        }

        private void Runnable()
        {
            try
            {
                var factory = new ConnectionFactory {
                    HostName = "localhost",
                    RequestedHeartbeat = 60
                };

                using (var Connection = factory.CreateConnection())
                {
                    using (var Channel = Connection.CreateModel())
                    {
                        Retries = 5;
                        Connected = true;

                        while(Run) {
                            try
                            {
                                Payload thing = Queue.Take(Token.Token);
                                var body = Encoding.UTF8.GetBytes(thing.Item3.ToJSON());
                                var plist = Channel.CreateBasicProperties();

                                plist.AppId = Host.HostId.ToString();
                                plist.Type  = Host.Role.ToString();

                                Channel.BasicPublish(thing.Item1, thing.Item2, plist, body);
                            }
                            catch(Exception ex)
                            {
                                Run = false;
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Retries -= 1;
                Connected = false;
                if (Debug)
                    Logger.Log(LogType.Debug, "Could not connect to MQ: " + ex.Message);
            } 
        }
    }
}
