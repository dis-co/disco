using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Iris.Core.Types;
using Iris.Core.Events;
using Iris.Core.Logging;
using Iris.Nodes.Graph;

using VVVV.Core.Logging;

using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.Graph;
      
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
 
namespace Iris.Nodes.Types
{
    using WriteJob = Tuple<IPin2, string>;
    using System.Net;
    using System.Net.Sockets;

    class AmqpServer : IDisposable
    {
        private const string TAG = "AmqpServer";

        public  bool    Running = false;
        public  string  Error   = null;
        private Thread  WorkerThread;
        private IModel  Channel;

        public  VVVVGraph Context { get; set; }

        #region Constructor
        public AmqpServer ()
        {
            WorkerThread = new Thread (Runnable);
        }
        #endregion

        #region Destructor
        public void Dispose ()
        {
            UnRegister ();
            Stop ();
        }
        #endregion

        private void Register ()
        {
            Context.PinAdded   += OnPinAdded;
            Context.PinRemoved += OnPinRemoved;
            Context.PinChanged += OnPinChange;

            Context.PatchAdded   += OnPatchAdded;
            Context.PatchRemoved += OnPatchRemoved;
            Context.PatchChanged += OnPatchChanged;
        }

        private void UnRegister ()
        {
            Context.PinAdded   -= OnPinAdded;
            Context.PinRemoved -= OnPinRemoved;
            Context.PinChanged -= OnPinChange;

            Context.PatchAdded   -= OnPatchAdded;
            Context.PatchRemoved -= OnPatchRemoved;
            Context.PatchChanged -= OnPatchChanged;
        }

        public void Start ()
        {
            Register();
            WorkerThread.Start ();
        }

        public void Stop ()
        {
            LogEntry.Debug(TAG, "Stopping Thread.");
            Running = false;
        }

        private void Runnable ()
        {
            try
            {
                LogEntry.Debug(TAG, "About to enter loop.");

                var factory = new ConnectionFactory() {
                    HostName = "localhost",
                    RequestedHeartbeat = 60
                }; 

                using (var connection = factory.CreateConnection())
                {
                    using (Channel = connection.CreateModel())
                    {
                        var exchanges = new List<string> {
                            DataUri.PinBase,
                            DataUri.PatchBase,
                            DataUri.CueBase
                        };

                        exchanges.ForEach(ex => Channel.ExchangeDeclare(ex, ExchangeType.Topic, false, true, null));

                        // gets a new queues (buffer for msgs) with name assigned by rabbit
                        var name = Channel.QueueDeclare().QueueName;

                        Channel.QueueBind(name, DataUri.PinBase,   "#");
                        Channel.QueueBind(name, DataUri.PatchBase, "#");
                        Channel.QueueBind(name, DataUri.CueBase,   "#");

                        var consumer = new QueueingBasicConsumer(Channel);

                        Channel.BasicConsume(name, true, consumer);

                        Running = true;

                        while (Running)
                        {
                            var ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();

                            // Only process message that do not originate from this Host
                            if (Host.HostId != ea.BasicProperties.AppId)
                            {
                                var role = Role.Parse(ea.BasicProperties.Type);
                                var message = Encoding.UTF8.GetString(ea.Body);

                                // process only messages intended for pins right here
                                if(ea.Exchange == DataUri.PinBase && ea.RoutingKey == DataUri.UpdateAction) 
                                {
                                    var pin = PinData.FromJSON(message);
                                    if (pin != null) Context.UpdatePin(pin);
                                    else LogEntry.Error(TAG, "could not parse PIN json: " + message);
                                }

                                if (ea.Exchange == DataUri.PatchBase &&
                                    ea.RoutingKey == DataUri.ListAction &&
                                    role == Roles.FrontEnd)
                                {
                                    Publish(DataUri.PatchBase, DataUri.ListAction, Context.GetPatches());
                                }
                            }
                        }
                    }
                }
                LogEntry.Debug(TAG, "Quitting..");
            }
            catch (Exception ex)
            {
                Running = false;
                Error = ex.Message;

                LogEntry.Fatal(TAG, "Could not start AQMPServer: " + ex.Message);
                LogEntry.Fatal(TAG, ex.StackTrace);
            }
        }

        private void Publish(string exchange, string topic, IIrisData thing)
        {
            if (Channel == null) return;
            try
            {
                var body = Encoding.UTF8.GetBytes(thing.ToJSON());

                var plist = Channel.CreateBasicProperties();
                plist.AppId = Host.HostId.ToString();
                plist.Type = Host.Role.ToString();

                Channel.BasicPublish(exchange, topic, plist, body);
            }
            catch (Exception ex)
            {
                Running = false;
                Error = ex.Message;

                LogEntry.Fatal(TAG, "Could not publish via AMQP: " + ex.Message);
                LogEntry.Fatal(TAG, ex.StackTrace);
            }
        }

        private void OnPinChange (object _, PinEventArgs e)
        {
            if (e.Direction == UpdateDirection.Remote) return;
            LogEntry.Debug(TAG, "PinChange event fired.");
            Publish(DataUri.PinBase, DataUri.UpdateAction, e.Pin);
        }

        private void OnPinAdded (object _, PinEventArgs e)
        {
            if(e.Direction == UpdateDirection.Remote) return;
            LogEntry.Debug(TAG, "PinAdded event fired.");
            Publish(DataUri.PinBase, DataUri.CreateAction, e.Pin);
        }

        private void OnPinRemoved (object _, PinEventArgs e)
        {
            if(e.Direction == UpdateDirection.Remote) return;
            LogEntry.Debug(TAG, "PinRemoved event fired.");
            Publish(DataUri.PinBase, DataUri.DeleteAction, e.Pin);
        }

        private void OnPatchAdded (object _, PatchEventArgs e)
        {
            LogEntry.Debug(TAG, "PatchAdded event fired.");
            Publish(DataUri.PatchBase, DataUri.CreateAction, e.Patch);
        }

        private void OnPatchChanged (object _, PatchEventArgs e)
        {
            LogEntry.Debug(TAG, "PatchChanged event fired.");
            Publish(DataUri.PatchBase, DataUri.UpdateAction, e.Patch);
        }

        private void OnPatchRemoved (object _, PatchEventArgs e)
        {
            LogEntry.Debug(TAG, "PatchRemoved event fired.");
            Publish(DataUri.PatchBase, DataUri.DeleteAction, e.Patch);
        }
    }
}
