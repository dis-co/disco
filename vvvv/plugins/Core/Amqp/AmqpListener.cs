using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Iris.Core.Amqp
{
    public abstract class AmqpListener : IDisposable
    {
        public string Exchange { get; set; }

        public bool   Running = false;
        public string Error   = null;
        
        protected IModel                    Channel;
        protected IConnection               Connection;
        protected IConnectionFactory        AmqpFactory;
        protected EventingBasicConsumer     Consumer;
        protected Dictionary<string,object> Options =
              new Dictionary<string, object>();

        private readonly object Lock = new object ();
        private          Thread WorkerThread;

        public void Dispose()
        {
            Stop();
        }

        public void Start ()
        {
            WorkerThread = new Thread(Runnable);
            WorkerThread.Start();
        }

        public void Stop ()
        {
            lock (Lock) {
                Monitor.Pulse (Lock);
            }
        }

        protected abstract void ProcessMessage(object o, BasicDeliverEventArgs ea);

        private void Runnable ()
        {
            try
            {
                var factory = new ConnectionFactory {
                    HostName = "localhost",
                    RequestedHeartbeat = 60
                };

                using(Connection = factory.CreateConnection())
                {
                    using(Channel = Connection.CreateModel())
                    {
                        Channel.ExchangeDeclare(Exchange, ExchangeType.Topic, false, true, null);

                        string QueueName = Channel.QueueDeclare();

                        Consumer = new EventingBasicConsumer(Channel);
                        Consumer.Received += ProcessMessage;

                        Channel.BasicConsume(QueueName, true, Consumer);
                        Channel.QueueBind(QueueName, Exchange, "#");

                        Running = true;

                        // Enter the lock.
                        lock (Lock) {
                            // Blocks, until the `Lock` is released during Stop()
                            Monitor.Wait (Lock);
                            // No calls should go here, or it will deadlock.
                        }
                    }
                }

                Running = false;

                Consumer.Received -= ProcessMessage;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Running = false;
            }
        }
    }
}
