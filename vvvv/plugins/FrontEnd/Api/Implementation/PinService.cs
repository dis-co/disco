using System;
using System.Text;

using Iris.Core.Types;
using Iris.Core.Logging;
using Iris.FrontEnd.Types;
using Iris.FrontEnd.Api.Interfaces;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using WampSharp.V2;
using WampSharp.V2.Realm;
using WampSharp.V2.Core.Contracts;

namespace Iris.FrontEnd.Api.Impl
{
    public class PinService : IrisService, IPinService
    {
        public PinService() : base(DataUri.PinBase)
        {
        }

        protected override void BeforeStart()
        {
        }

        protected override void OnMessage(BasicDeliverEventArgs ea)
        {
            if (ea.RoutingKey == DataUri.CreateAction)
                Publish(DataUri.PinCreate, Encoding.UTF8.GetString(ea.Body));

            if (ea.RoutingKey == DataUri.DeleteAction)
                Publish(DataUri.PinDelete, Encoding.UTF8.GetString(ea.Body));

            if (ea.RoutingKey == DataUri.UpdateAction)
                Publish(DataUri.PinUpdate, Encoding.UTF8.GetString(ea.Body));
        }

        public void UpdatePin(string sid, PinData pin)
        {
            Call(DataUri.UpdateAction, pin.ToJSON());
            Publish(sid, DataUri.PinUpdate, pin);
        }
    }
}
