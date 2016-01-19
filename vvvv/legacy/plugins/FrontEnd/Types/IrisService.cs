using System;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using WampSharp.V2;
using WampSharp.V2.Realm;
using WampSharp.V2.Core.Contracts;

using Iris.Core.Couch;
using Iris.Core.Types;
using Iris.Core.Logging;
using Iris.FrontEnd.Api;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Newtonsoft.Json.Linq;

namespace Iris.FrontEnd.Types
{
    using IrisHttpResponse = Tuple<HttpStatusCode, string>;

    public abstract class IrisService : IDisposable
    {
        private const string TAG = "IrisService";

        protected        string                Exchange;
        protected        CouchClient           Couch;
        protected        IWampSubject          Subject;
        protected static IConnectionFactory    AmqpFactory;
        protected        EventingBasicConsumer Consumer;

        private IBasicProperties PList;
        private IConnection      Connection;
        private IModel           Channel;

        static IrisService()
        {
            AmqpFactory = new ConnectionFactory {
                HostName = "localhost"
            };
        }

        public IrisService(string exchange)
        {
            Exchange = exchange;
            Initialize();
        }

        public void Dispose()
        {
            Consumer.Received -= HandleAMQPMessage;
            Channel.Dispose();
            Connection.Dispose();
        }

        public void Initialize()
        {
            Couch = new CouchClient("http://localhost:5984/");

            try
            {
                BeforeStart();

                Connection = AmqpFactory.CreateConnection();
                Channel = Connection.CreateModel();

                Channel.ExchangeDeclare(Exchange, ExchangeType.Topic, false, true, null);

                string QueueName = Channel.QueueDeclare();

                Consumer = new EventingBasicConsumer(Channel);
                Consumer.Received += HandleAMQPMessage;

                Channel.BasicConsume(QueueName, true, Consumer);
                Channel.QueueBind(QueueName, Exchange, "#");
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception during startup");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(255); // this error should be deadly no?
            }

            PList = Channel.CreateBasicProperties();
            PList.AppId = Host.HostId.ToString(); // is re-generated each time the app is started
            PList.Type  = Host.Role.ToString();
        }

        //  _______ _______ _______ ______
        // |   _   |   |   |       |   __ \
        // |       |       |   -  _|    __/
        // |___|___|__|_|__|_______|___|   machinery
        //

        public void Call (string topic)
        {
            Channel.BasicPublish(Exchange, topic, PList, new byte[0]);
        }

        public void Call (string topic, string thing)
        {
            Channel.BasicPublish(Exchange, topic, PList, Encoding.UTF8.GetBytes(thing));
        }


        private void HandleAMQPMessage(object it, BasicDeliverEventArgs args)
        {
            if (Host.HostId != args.BasicProperties.AppId)
                OnMessage(args);
        }

        protected abstract void BeforeStart();
        protected abstract void OnMessage(BasicDeliverEventArgs args);

        //  ________ _______ _______ ______
        // |  |  |  |   _   |   |   |   __ \
        // |  |  |  |       |       |    __/
        // |________|___|___|__|_|__|___|   machinery
        //

        public void Register(IWampHostedRealm realm)
        {
            Subject = realm.Services.GetSubject("iris.updates");
            Task registration = realm.Services.RegisterCallee(this);
            registration.Wait();
        }

        private void Send (WampEvent @event)
        {
            try
            {
                if (Subject != null) Subject.OnNext(@event);
            }
            catch (WampException ex)
            {
                LogEntry.Fatal(TAG, "WampException: " + ex.ErrorUri);
            }
            catch (Exception ex)
            {
                LogEntry.Fatal(TAG, "Exception: " + ex.Message);
            }
        }

        public void Publish (string uri, Object a)
        {
            Publish(null, uri, a);
        }

        public void Publish (string sid, string uri, Object a)
        {
            var @event = new WampEvent {
                Options = new PublishOptions (),
                Arguments = new object[] { sid, uri, a }
            };
            Send (@event);
        }
    }
}
