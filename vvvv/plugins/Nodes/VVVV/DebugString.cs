using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

using Iris.Core.Types;
using Iris.Nodes.Types;
using Iris.Core.Logging;

using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.Graph;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

#pragma warning disable 0168

namespace VVVV.Nodes
{
    using Payload = Tuple<string, string, IIrisData>;

    #region PluginInfo
    [PluginInfo(Name = "DebugString", Category = "Iris", Help = "DebugString", Tags = "", AutoEvaluate = true)]
    #endregion PluginInfo

    public class DebugString : IPluginEvaluate, IDisposable
    {

        [Input("Input")]
        IDiffSpread<string> InValue;

        [Input("PatchName", IsSingle = true)]
        IDiffSpread<string> PatchName;

        [Input("PinName", IsSingle = true)]
        IDiffSpread<string> PinName;

        [Input("PinId", IsSingle = true)]
        IDiffSpread<string> PinId;

        [Output("Buffer Size")]
        ISpread<int> BufSize;

        private const string TAG = "DebugString";

        private bool Initialized = false;
        private bool Ready = false;
        private bool Run = true;

        private PinData Pin;
        private Patch Patch;
        private Patches Patches;

        private Thread WorkerThread;
        private CancellationTokenSource Token;
        private BlockingCollection<Payload> Queue;

        private List<string> Exchanges = new List<string> {
            DataUri.PinBase,
            DataUri.PatchBase
        };

        public DebugString() {
            var host = Host.Renderer;
            Token = new CancellationTokenSource();
            Queue = new BlockingCollection<Payload>();
            WorkerThread = new Thread(Runnable);
        }
            
        public void Evaluate(int SpreadMax) 
        {
            if(!Initialized)
            {
                var id = (PinId[0].Length > 0) ? PinId[0] : System.Guid.NewGuid().ToString(); 
                MkPatch(PatchName[0],PinName[0], id, InValue.ToList());
                WorkerThread.Start();
                Initialized = true;
            }

            if(PatchName.IsChanged)
                Patch.Name = new Name(PatchName[0]);

            if(PinName.IsChanged)
            {
                Pin.Name = PinName[0];
                Queue.Add(new Payload(DataUri.PinBase, DataUri.UpdateAction, Pin));
            }

            if(PinId.IsChanged)
            {
                Pin.Id = new IrisId(PinId[0]);
                Queue.Add(new Payload(DataUri.PinBase, DataUri.UpdateAction, Pin));
            }

            if(InValue.IsChanged && Ready)
            {
                Assign(InValue.ToList());
                Queue.TryAdd(new Payload(DataUri.PinBase, DataUri.UpdateAction, MkPin(PinName[0],PinId[0],InValue.ToList())));
            }

            BufSize[0] = Queue.Count;
        }

        public void Dispose()
        {
            Token.Cancel();
            Run = false;
        }

        private void Assign(List<string> Values)
        {
            var slices = new PinSlices();
            Values.ForEach(v => slices.Add(new PinSlice(Behavior.String, v)));
            Pin.Values = slices;
        }

        private PinData MkPin(string PinName, string PinId, List<string> Values)
        {
            var slices = new PinSlices();

            Values.ForEach(v => slices.Add(new PinSlice(Behavior.String, v)));

            return new PinData {
                Id        = new IrisId(PinId),
                HostId    = Host.HostId,
                NodePath  = new NodePath("debug-string"),
                Name      = PinName,
                Tag       = "",
                Address   = new OSCAddress("debug-string"),
                Type      = PinType.String,
                Values    = slices,
                Behavior  = Behavior.String,
            };
        }

        private void MkPatch(string PatchName, string PinName, string PinId, List<string> Values)
        {
            Pin = MkPin(PinName, PinId, Values);

            Patch = new Patch(new IrisId(System.Guid.NewGuid().ToString()),
                              new NodePath("debug-string"),
                              new Name(PatchName),
                              new FilePath(@"debug-string"),
                              new Pins { Pin });
            Patches = new Patches { Patch };
        }

        private void Runnable()
        {
            var factory = new ConnectionFactory() {
                HostName = "localhost",
                RequestedHeartbeat = 60
            }; 

            using (var Connection = factory.CreateConnection()) {
                using (var Channel = Connection.CreateModel()) {

                    Exchanges.ForEach(ex => Channel.ExchangeDeclare(ex, ExchangeType.Topic, false, true, null));

                    // gets a new queues (buffer for msgs) with name assigned by rabbit
                    var name = Channel.QueueDeclare().QueueName;

                    Channel.QueueBind(name, DataUri.PinBase, "#");
                    Channel.QueueBind(name, DataUri.PatchBase, "#");

                    var consumer = new EventingBasicConsumer(Channel);

                    consumer.Registered += (ch, ea) => {
                        Ready = true;
                    };

                    consumer.Received += (ch, ea) => {
                        var role = Role.Parse(ea.BasicProperties.Type);
                        if (ea.Exchange   == DataUri.PatchBase  &&
                            ea.RoutingKey == DataUri.ListAction &&
                            role == Roles.FrontEnd)
                        {
                            var body = Encoding.UTF8.GetBytes(Patches.ToJSON());

                            var plist = Channel.CreateBasicProperties();
                            plist.AppId = Host.HostId.ToString();
                            plist.Type = Host.Role.ToString();

                            Channel.BasicPublish(DataUri.PatchBase, DataUri.ListAction, plist, body);
                        }
                    };

                    Channel.BasicConsume(name, true, consumer);

                    Initialized = true;

                    while(Run)
                    {
                        try
                        {
                            Payload job = Queue.Take(Token.Token);
                            var body = Encoding.UTF8.GetBytes(job.Item3.ToJSON());

                            var plist = Channel.CreateBasicProperties();
                            plist.AppId = Host.HostId.ToString();
                            plist.Type = Host.Role.ToString();

                            Channel.BasicPublish(job.Item1, job.Item2, plist, body);
                        }
                        catch(Exception ex)
                        {
                            Run = false;
                        }
                    }
                }
            }
        }
    }
}
